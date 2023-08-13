using Microsoft.AspNetCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;
using Web;

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();
builder.Services.AddScoped<ClipHub>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy =>
{
    policy.AllowAnyHeader()
        .AllowAnyOrigin()
        .AllowAnyMethod();
});
app.UseHttpsRedirection();
app.UseOpenApi();
app.UseSwaggerUi3();

app.MapPost("clips", async (ClipData data, AppDbContext db, IHubContext<ClipHub, IClipHub> hub, FileService fileService) =>
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

app.MapDelete("clips", (Guid id, AppDbContext db) =>
{
    return db.Clips.Where(x => x.Id == id).ExecuteDeleteAsync();
}).WithTags("clips");

app.MapGet("clips", (AppDbContext db) =>
{
    return db.Clips.Select(x => new
    {
        x.Id,
        x.DateRecorded,
        x.Name,
        Thumbnail = null as byte[],
    }).ToListAsync();
}).WithTags("clips");

app.MapGet("clips/{id:guid}", async (Guid id, AppDbContext db, FileService fileService, HttpContext httpContext) =>
{
    var clip = await db.Clips.Select(x => new
    {
        x.Id,
        x.Name,
        x.FileId,
        Detections = x.Detections.Select(d => new
        {
            d.Timestamp,
            d.BoundingBox,
        }).ToArray()
    }).FirstOrDefaultAsync(x => x.Id == id);

    if (clip is null)
    {
        return Results.NotFound();
    }

    var path = $"{httpContext.Request.Host}/clips/{id}/video/{clip.FileId}";
    return Results.Json(new
    {
        clip.Id,
        clip.Name,
        clip.Detections,
        Url = path

    }, JsonOptions.Default);

}).WithTags("clips");

app.MapGet("clips/{id:guid}/video/{fileId:guid}", async (Guid id, Guid fileId, AppDbContext db, FileService fileService) =>
{
    var exists = db.Clips.AnyAsync(x => x.Id == id && x.FileId == fileId);
    var stream = fileService.GetFileAsync(fileId);
    await exists;
    return stream;
}).WithTags("clips");

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

public sealed class ClipData
{
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public DateTimeOffset DateRecorded { get; init; }
    public DetectionInfoData[] Detections { get; init; } = Array.Empty<DetectionInfoData>();
}

public sealed class DetectionInfoData
{
    public int Timestamp { get; init; }
    public float[] BoundingBox { get; init; } = Array.Empty<float>();
}

public interface IClipHub
{
    [HubMethodName("newClipAdded")]
    Task NewClipAdded(Guid clipId);
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
    DateTimeOffset? ModifiedAt { get; init; }
}

public sealed class Clip : ICreatable, IUpdateable
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public Guid FileId { get; init; }
    public DateTimeOffset DateRecorded { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<Detection> Detections { get; init; } = Array.Empty<Detection>();
    public DateTimeOffset? ModifiedAt { get; init; }
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

    public async Task<Guid> StoreFileAsync(byte[] data, string extension)
    {
        var id = Guid.NewGuid();
        var path = $"store/{id}";
        await File.WriteAllBytesAsync(path, data);
        return id;
    }

    public Task<Stream> GetFileAsync(Guid fileId)
    {
        var path = $"store/{fileId}";
        var stream = File.OpenRead(path);
        return Task.FromResult<Stream>(stream);
    }
}
