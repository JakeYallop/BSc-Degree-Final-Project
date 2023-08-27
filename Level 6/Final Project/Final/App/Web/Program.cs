using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;

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
builder.Services.AddHttpClient();

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

app.MapPost("clips", async (ClipInfo data, AppDbContext db, IHubContext<ClipHub, IClipHub> hub, FileService fileService) =>
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
    await db.SaveChangesAsync();
    await hub.Clients.All.NewClipAdded(clip.Id);
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


app.MapPut("admin/push", async ([FromServices] HttpClient client) =>
{
    await client.PostAsJsonAsync("http://localhost:3000/notify", new { });
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


public static class HttpContextExtensions
{
    public static string BaseUrl(this HttpContext context) => $"{context.Request.Scheme}://{context.Request.Host}";
}

public sealed class NameInfo
{
    public string Name { get; init; } = null!;
}

public static class JsonOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };
}

public sealed class ClipInfo
{
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public DateTimeOffset DateRecorded { get; init; }
    public DetectionInfoData[] Detections { get; init; } = Array.Empty<DetectionInfoData>();
}

public sealed class ClipData
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public DateTimeOffset DateRecorded { get; init; }
    public string Url { get; init; } = null!;
    public IEnumerable<DetectionData> Detections { get; init; } = Array.Empty<DetectionData>();

    public static ClipData MapFrom(Clip clip, string baseUrl)
    {
        return new ClipData()
        {
            Id = clip.Id,
            Name = clip.Name,
            DateRecorded = clip.DateRecorded,
            Url = $"{baseUrl}/clips/{clip.Id}/video/{clip.FileId}.mp4",
            Detections = clip.Detections.Select(x => new DetectionData()
            {
                Timestamp = x.Timestamp.Milliseconds,
                BoundingBox = new[] { x.BoundingBox.X, x.BoundingBox.Y, x.BoundingBox.Z, x.BoundingBox.W },
            }),
        };
    }
}

public sealed class DetectionData
{
    public int Timestamp { get; init; }
    public float[] BoundingBox { get; init; } = Array.Empty<float>();
}

public sealed class DetectionInfoData
{
    public int Timestamp { get; init; }
    public float[] BoundingBox { get; init; } = Array.Empty<float>();
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

public interface ICreatable
{
    DateTimeOffset CreatedAt { get; init; }
}

public interface IUpdateable
{
    DateTimeOffset? ModifiedAt { get; set; }
}

public sealed class Clip : ICreatable, IUpdateable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid FileId { get; set; }
    public DateTimeOffset DateRecorded { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<Detection> Detections { get; set; } = Array.Empty<Detection>();
    public DateTimeOffset? ModifiedAt { get; set; }

    public void Update()
    {
        ModifiedAt = DateTime.UtcNow;
    }

}

public sealed class Detection
{
    public Guid Id { get; init; }
    public Guid ClipId { get; init; }
    public TimeSpan Timestamp { get; init; }
    public Vector4 BoundingBox { get; init; }
}
public sealed class AppDbContext : DbContext
{
    public DbSet<Clip> Clips { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Clip>()
            .HasMany(c => c.Detections)
            .WithOne();

        modelBuilder.Entity<Detection>()
            .Property(x => x.BoundingBox)
            .HasConversion(JsonValueConverter<Vector4>.Instance);
    }
}

public sealed class JsonValueConverter<T> : ValueConverter<T, string>
{
    public static readonly JsonValueConverter<T> Instance = new();
    public JsonValueConverter() : base(x => JsonSerializer.Serialize(x, JsonOptions.Default), x => JsonSerializer.Deserialize<T>(x, JsonOptions.Default)!)
    {
    }
}

public sealed class FileService
{
    public FileService()
    {
    }

    public async Task<Guid> StoreFileAsync(byte[] data, string extension, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var path = $"store/{id}";
        await File.WriteAllBytesAsync(path, data, cancellationToken);
        return id;
    }

    public Task<Stream> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = $"store/{fileId}";
        var stream = File.OpenRead(path);
        if (cancellationToken.IsCancellationRequested)
        {
            stream.Dispose();
            throw new OperationCanceledException();
        }
        return Task.FromResult<Stream>(stream);
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
