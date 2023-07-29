using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(configure =>
{
    if (builder.Environment.IsDevelopment())
    {
        configure.EnableDetailedErrors = true;
    }
});
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(":memory");
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument();

builder.Services.AddScoped<ClipHub>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseOpenApi();
app.UseSwaggerUi3();

app.MapPost("clips", async (ClipData data, AppDbContext db, ClipHub hub) =>
{
    var clip = new Clip
    {
        Id = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow,
        Name = Guid.NewGuid().ToString(),
        Detections = data.Detections.Select(d => new Detection
        {
            Timestamp = d.Timestamp,
            BoundingBox = d.BoundingBox,
        }).ToArray()
    };

    db.Add(clip);
    await db.SaveChangesAsync();
    await hub.ClipAddedAsync(clip.Id);
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
        x.CreatedAt,
        x.Name,
    }).ToListAsync();
}).WithTags("clips");

app.MapGet("clips/{id:guid}", (Guid id, AppDbContext db) =>
{
    return db.Clips.Select(x => new
    {
        x.Id,
        x.CreatedAt,
        x.Name,
        Detections = x.Detections.Select(d => new
        {
            d.Timestamp,
            d.BoundingBox,
        }).ToArray()
    }).FirstOrDefaultAsync(x => x.Id == id);
}).WithTags("clips");

app.MapHub<ClipHub>("/clipHub");
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
    public required DateTimeOffset Date { get; init; }
    public DetectionInfoData[] Detections { get; init; } = Array.Empty<DetectionInfoData>();
}

public sealed class DetectionInfoData
{
    public TimeSpan Timestamp { get; init; }
    public Vector2[] BoundingBox { get; init; } = Array.Empty<Vector2>();
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

public interface IEntity : ICreatable
{
    Guid Id { get; init; }
}

public interface IUpdateable
{
    DateTimeOffset ModifiedAt { get; init; }
}

public sealed class Clip : IEntity
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string Name { get; init; }
    public Detection[] Detections { get; init; } = Array.Empty<Detection>();

}

public sealed class Detection
{
    public Guid ClipId { get; init; }
    public TimeSpan Timestamp { get; init; }
    public Vector2[] BoundingBox { get; init; } = Array.Empty<Vector2>();
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
            .HasConversion(JsonValueConverter<Vector2>.Instance);
    }
}

public sealed class JsonValueConverter<T> : ValueConverter<T, string>
{
    public static readonly JsonValueConverter<T> Instance = new();
    public JsonValueConverter() : base(x => JsonSerializer.Serialize(x, JsonOptions.Default), x => JsonSerializer.Deserialize<T>(x, JsonOptions.Default)!)
    {
    }
}