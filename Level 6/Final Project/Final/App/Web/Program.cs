using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
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

app.MapPost("clips", async (ClipInfo data, AppDbContext db, IHubContext<ClipHub, IClipHub> hub, FileService fileService, NotificationsClient client) =>
{
    var clip = new Clip
    {
        Id = Guid.NewGuid(),
        FileId = await fileService.StoreFileAsync(data.Data, ".mp4"),
        Name = Guid.NewGuid().ToString(),
        DateRecorded = data.DateRecorded,
        CreatedAt = DateTimeOffset.UtcNow,
        Detections = data.Detections.Select(d => new Detection
        {
            Timestamp = TimeSpan.FromMilliseconds(d.Timestamp),
            BoundingBox = new Vector4(d.BoundingBox[0], d.BoundingBox[1], d.BoundingBox[2], d.BoundingBox[3]),
        }).ToArray()
    };

    db.Add(clip);
    var task1 = db.SaveChangesAsync();
    var task2 = hub.Clients.All.NewClipAdded(clip.Id);
    var task3 = client.NotifyAsync(new Notification("New motion detected", $"New motion was detected at {clip.DateRecorded}."));
    await Task.WhenAll(task1, task2, task3);

}).WithTags("clips");

app.MapDelete("clips", (Guid id, AppDbContext db, CancellationToken cancellation) =>
{
    return db.Clips.Where(x => x.Id == id).ExecuteDeleteAsync(cancellation);
}).WithTags("clips");

app.MapGet("clips", (AppDbContext db, CancellationToken cancellation) =>
{
    return db.Clips.Select(x => new
    {
        x.Id,
        x.DateRecorded,
        x.Name,
        Thumbnail = null as byte[],
    }).ToListAsync(cancellation);
}).WithTags("clips");

app.MapGet("clips/{id:guid}", async (Guid id, AppDbContext db, FileService fileService, HttpContext httpContext, CancellationToken cancellation) =>
{
    var clip = await db.Clips.Select(x => new
    {
        x.Id,
        x.Name,
        x.DateRecorded,
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
            BoundingBox = new[] { x.BoundingBox.X, x.BoundingBox.Y, x.BoundingBox.Z, x.BoundingBox.W },
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


app.MapPut("admin/push", async ([FromServices] NotificationsClient client) =>
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

    public NotificationsClient(HttpClient client)
    {
        _client = client;
    }

    public Task NotifyAsync(Notification notification)
        => _client.PostAsJsonAsync("/notify", notification);

}
