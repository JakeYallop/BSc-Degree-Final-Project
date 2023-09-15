using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using System.Threading.Channels;
using Web;
using Web.Classification;
using Web.Entities;
using Web.Models;
using Web.Routes;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddSignalR(configure =>
{
    if (builder.Environment.IsDevelopment())
    {
        configure.EnableDetailedErrors = true;
    }
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(configuration.GetConnectionString("Default"));
});
builder.Services.AddSingleton<FileService>();
builder.Services.AddHostedService<FileServiceStartupService>();
builder.Services.AddSingleton<VideoService>();
builder.Services.AddSingleton<ImageClassificationService>();
builder.Services.AddSingleton<IImageClassifierSettings, EfficientNetLite4Settings>(_ => EfficientNetLite4Settings.Create(modelPath: "Classification/efficientnet-lite4-11.onnx", classesPath: "Classification/efficientnet_labels_processed.txt"));
builder.Services.AddHostedService<ScopedBackgroundService<ImageAnalysisBackgroundService>>();
builder.Services.AddScoped<ImageAnalysisBackgroundService>();
builder.Services.AddSingleton<ImageAnalysisChannel>();

builder.Services.AddScoped<ClipHub>();
builder.Services.AddHttpClient<NotificationsClient>(client =>
{
    client.BaseAddress = new Uri(configuration["NotificationsBaseUrl"] ?? throw new InvalidOperationException("Missing configuration value for NotificationsBaseUrl"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo()
    {
        Title = "Camera Motion Detection API",
    });
});

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy =>
{
    policy.AllowAnyHeader()
        .AllowAnyOrigin()
        .AllowAnyMethod();
});
app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "Camera Motion Detection API";
    options.ConfigObject.DocExpansion = Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None;
});

app.MapGroup("/clips")
    .MapClipsApiEndpoints()
    .WithTags("Clips")
    .WithOpenApi();

#if DEBUG

app.MapGroup("/admin")
    .MapAdminApiEndpoints()
    .WithTags("Admin")
    .WithDescription("A collection of debug/administration APIs. Only available when running the API in Debug mode")
    .WithSummary("Test Description")
    .WithOpenApi();

#endif

app.MapHub<ClipHub>("/clipHub");

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using var scope = scopeFactory.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
db.Database.EnsureCreated();
app.Run();

public static class JsonOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };
}

public interface IClipHub
{
    Task NewClipAdded(Guid clipId);

    Task ClipUpdated(Guid clipId);
    Task AdditionalTasksCompleted(Guid clipId);
}

public sealed class ClipHub : Hub<IClipHub>
{
    //public async Task ClipAddedAsync(Guid id)
    //{
    //    await Clients.All.NewClipAdded(id);
    //}
}

public sealed class FileServiceStartupService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists("store"))
        {
            Directory.CreateDirectory("store");
        }
        return Task.CompletedTask;
    }
}

public sealed class NotificationsClient
{
    private readonly HttpClient _client;
    private readonly ILogger<NotificationsClient> _logger;

    public NotificationsClient(HttpClient client, ILogger<NotificationsClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task NotifyAsync(Notification notification)
    {
        try
        {
            await _client.PostAsJsonAsync("/notify", notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification.");
            return;
        }
    }
}

public sealed class ImageAnalysisChannel
{
    private static readonly Channel<Guid> Channel = System.Threading.Channels.Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    public ValueTask<Guid> ReadAsync(CancellationToken cancellationToken = default)
    {
        return Channel.Reader.ReadAsync(cancellationToken);
    }

    public ValueTask WriteAsync(Guid clipId, CancellationToken cancellationToken = default)
    {
        return Channel.Writer.WriteAsync(clipId, cancellationToken);
    }
}

public sealed class ImageAnalysisBackgroundService : IScopedBackgroundService<ImageAnalysisBackgroundService>
{
    private readonly ImageAnalysisChannel _channel;
    private readonly AppDbContext _db;
    private readonly FileService _fileService;
    private readonly VideoService _videoService;
    private readonly ImageClassificationService _classificationService;
    private readonly IHubContext<ClipHub, IClipHub> _hub;
    private readonly ILogger<ImageAnalysisBackgroundService> _logger;

    public ImageAnalysisBackgroundService(
        ImageAnalysisChannel channel,
        AppDbContext db,
        FileService fileService,
        VideoService videoService,
        ImageClassificationService classificationService,
        IHubContext<ClipHub, IClipHub> hub,
        ILogger<ImageAnalysisBackgroundService> logger)
    {
        _channel = channel;
        _db = db;
        _fileService = fileService;
        _videoService = videoService;
        _classificationService = classificationService;
        _hub = hub;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var clipId = await _channel.ReadAsync(stoppingToken);
                var clip = await _db.Clips
                    .Where(x => x.Id == clipId)
                    .Include(x => x.Detections)
                    .FirstAsync(stoppingToken);

                var file = await _fileService.GetFileAsync(clip.FileId, stoppingToken);
                var frameToClassify = await _videoService.GetBestCroppedFrameAsync(file, clip.Detections.Select(x => (x.BoundingBox, x.Timestamp)).ToArray());
                var fileId = await _fileService.StoreFileAsync(frameToClassify, stoppingToken);

                frameToClassify = await _fileService.GetFileAsync(fileId, stoppingToken);
                var ms = new MemoryStream(capacity: (int)frameToClassify.Length);
                await frameToClassify.CopyToAsync(ms, stoppingToken);
                var classes = await _classificationService.ClassifyAsync(ms.ToArray());
                Console.WriteLine($"Classifier predicted:{Environment.NewLine}{string.Join(Environment.NewLine, classes.Select(x => $"{x.Class} {x.Confidence * 100:F5}%"))}");

                clip.Classifications = classes.Select(c => new Classification
                {
                    Label = c.Class,
                    Confidence = c.Confidence,
                    CreatedAt = DateTimeOffset.UtcNow
                }).ToArray();
                clip.ImageUsedForClassification = fileId;
                await _db.SaveChangesAsync(stoppingToken);
                await _hub.Clients.All.AdditionalTasksCompleted(clipId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying clip.");
            }
        }
    }
}

public sealed class ScopedBackgroundService<T> : BackgroundService where T : IScopedBackgroundService<T>
{
    private readonly IServiceScopeFactory _factory;

    public ScopedBackgroundService(IServiceScopeFactory factory)
    {
        _factory = factory;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _factory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            await service.ExecuteAsync(stoppingToken);
        }
    }
}

public interface IScopedBackgroundService<T> where T : IScopedBackgroundService<T>
{
    public Task ExecuteAsync(CancellationToken cancellationToken);
}

public static class ThumbnailUrlHelper
{
    public static string? GetUrl(string baseUrl, Guid clipId, Guid? thumbnailId)
        => thumbnailId is null ? null : $"{baseUrl}/clips/{clipId}/thumb/{thumbnailId}.jpg";
}
