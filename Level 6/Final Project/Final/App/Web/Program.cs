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
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "Camera Motion Detection API";
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
app.UseOpenApi(configure =>
{
});
app.UseSwaggerUi3(configure =>
{
    configure.DocumentTitle = "Camera Motion Detection API";
});

app.MapPost("clips", async (
    ClipInfo data,
    AppDbContext db,
    IHubContext<ClipHub, IClipHub> hub,
    FileService fileService,
    NotificationsClient client,
    ImageAnalysisChannel channel) =>
{
    var clip = new Clip
    {
        Id = Guid.NewGuid(),
        FileId = await fileService.StoreFileAsync(data.Data),
        Name = Guid.NewGuid().ToString(),
        DateRecorded = data.DateRecorded,
        CreatedAt = DateTimeOffset.UtcNow,
        Detections = data.Detections.Select(d => new Detection
        {
            Timestamp = TimeSpan.FromMilliseconds(d.Timestamp),
            BoundingBox = new(d.BoundingBox[0], d.BoundingBox[1], d.BoundingBox[2], d.BoundingBox[3]),
        }).ToArray()
    };

    db.Add(clip);
    await db.SaveChangesAsync();

    var analysisTask = channel.WriteAsync(clip.Id);
    var notifyClientTask = hub.Clients.All.NewClipAdded(clip.Id);
    var pushNotificationTask = client.NotifyAsync(new Notification("New motion detected", $"New motion was detected at {clip.DateRecorded.ToLocalTime()}."));
    await Task.WhenAll(notifyClientTask, pushNotificationTask);
}).WithTags("clips");

app.MapDelete("clips", (Guid id, AppDbContext db, CancellationToken cancellation) =>
{
    return db.Clips.Where(x => x.Id == id).ExecuteDeleteAsync(cancellation);
}).WithTags("clips");

app.MapGet("clips", (AppDbContext db, FileService fileService, CancellationToken cancellation) =>
{
    return db.Clips.Select(x => new
    {
        x.Id,
        DateRecorded = x.DateRecorded.ToLocalTime(),
        x.Name,
        ThumbnailId = x.ImageUsedForClassification
    }).ToListAsync(cancellation);
}).WithTags("clips");

app.MapGet("clips/{id:guid}", async (Guid id, AppDbContext db, FileService fileService, HttpContext httpContext, CancellationToken cancellation) =>
{
    var clip = await db.Clips.Select(x => new
    {
        x.Id,
        x.Name,
        DateRecorded = x.DateRecorded.ToLocalTime(),
        x.FileId,
        Detections = x.Detections.Select(d => new
        {
            d.Timestamp,
            d.BoundingBox,
        }).ToArray()
    }).FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellation);

    if (clip is null)
    {
        return Results.NotFound();
    }

    var path = $"{httpContext.BaseUrl()}/clips/{id}/video/{clip.FileId}.mp4";
    return Results.Json(new ClipData()
    {
        DateRecorded = clip.DateRecorded,
        Id = clip.Id,
        Name = clip.Name,
        Url = path,
        Detections = clip.Detections.Select(x => new DetectionData
        {
            BoundingBox = new[] { x.BoundingBox.X, x.BoundingBox.Y, x.BoundingBox.Width, x.BoundingBox.Height },
            Timestamp = x.Timestamp.Milliseconds,
        }).ToArray(),
    }
   , JsonOptions.Default);

}).WithTags("clips");

app.MapGet("clips/{id:guid}/video/{fileId:guid}.mp4", async (Guid id, Guid fileId, AppDbContext db, FileService fileService, CancellationToken cancellation) =>
{
    var exists = db.Clips.AnyAsync(x => x.Id == id && x.FileId == fileId, cancellation);
    var streamTask = fileService.GetFileAsync(fileId, cancellation);
    await Task.WhenAll(streamTask, exists);
    cancellation.ThrowIfCancellationRequested();

    if (!exists.Result)
    {
        return Results.NotFound();
    }

    var s = new MemoryStream();
    streamTask.Result.CopyTo(s);

    return Results.File(s.ToArray(), "video/mp4");
}).WithTags("clips");


app.MapGet("clips/{id:guid}/thumb/{fileId:guid}.jpg", async (Guid id, Guid fileId, AppDbContext db, FileService fileService, CancellationToken cancellation) =>
{
    var exists = db.Clips.AnyAsync(x => x.Id == id && x.ImageUsedForClassification == fileId, cancellation);
    var streamTask = fileService.GetFileAsync(fileId, cancellation);
    await Task.WhenAll(streamTask, exists);
    cancellation.ThrowIfCancellationRequested();

    if (!exists.Result)
    {
        return Results.NotFound();
    }

    var s = new MemoryStream();
    streamTask.Result.CopyTo(s);

    return Results.File(s.ToArray(), "image/jpeg");
}).WithTags("clips");

app.MapPut("clips/{id:guid}/name", async (Guid id, [FromBody] NameInfo data, AppDbContext db, HttpContext httpContext, IHubContext<ClipHub, IClipHub> hub, CancellationToken cancellationToken) =>
{
    var clip = await db.Clips.FirstAsync(x => x.Id == id, cancellationToken);
    clip.Name = data.Name;
    clip.Update();
    await db.SaveChangesAsync(cancellationToken);

    await hub.Clients.All.ClipUpdated(id);

}).WithTags("clips");

#if DEBUG
app.MapPut("admin/clearall", async (AppDbContext db, ILoggerFactory factory, CancellationToken cancellation) =>
{
    var logger = factory.CreateLogger("AdminApi");
    await db.Database.EnsureDeletedAsync(cancellation);
    await db.Database.EnsureCreatedAsync(cancellation);
    try
    {
        Directory.Delete("store", true);
        Directory.CreateDirectory("store");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error clearing down clips directory");
    }
}).WithTags("admin")
.WithDescription("Clear and delete all resources")
.WithSummary("Only available when running the API in Debug mode");


app.MapPut("admin/push", ([FromServices] NotificationsClient client) =>
{
    return client.NotifyAsync(new Notification("Test Notification", "Test notification from the API"));
}).WithTags("admin")
.WithDescription("Clear and delete all resources")
.WithSummary("Only available when running the API in Debug mode");;
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
    public async Task ClipAddedAsync(Guid id)
    {
        await Clients.All.NewClipAdded(id);
    }
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
