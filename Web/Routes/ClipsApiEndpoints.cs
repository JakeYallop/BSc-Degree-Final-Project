using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Web.Entities;
using Web.Models;

namespace Web.Routes;

public static class ClipsApiEndpoints
{
    public static RouteGroupBuilder MapClipsApiEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("", async (
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
        });

        group.MapDelete("", (Guid id, AppDbContext db, CancellationToken cancellation) =>
        {
            return db.Clips.Where(x => x.Id == id).ExecuteDeleteAsync(cancellation);
        });

        group.MapGet("", (HttpContext httpContext, AppDbContext db, FileService fileService, CancellationToken cancellation) =>
        {
            return db.Clips.Select(x => new
            {
                x.Id,
                DateRecorded = x.DateRecorded.ToLocalTime(),
                x.Name,
                Thumbnail = ThumbnailUrlHelper.GetUrl(httpContext.BaseUrl(), x.Id, x.ImageUsedForClassification),
                Classification = x.Classifications.Select(x => new { x.Confidence, x.Label }).OrderByDescending(x => x.Confidence).FirstOrDefault(),
            }).ToListAsync(cancellation);
        });

        group.MapGet("{id:guid}", async(Guid id, AppDbContext db, FileService fileService, HttpContext httpContext, CancellationToken cancellation) =>
        {
            var clip = await db.Clips
                .Include(x => x.Detections)
                .Include(x => x.Classifications)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellation);

            if (clip is null)
            {
                return Results.NotFound();
            }

            var path = $"{httpContext.BaseUrl()}/clips/{id}/video/{clip.FileId}.mp4";
            return Results.Json(ClipData.MapFrom(clip, httpContext.BaseUrl()), JsonOptions.Default);

        });

        group.MapGet("{id:guid}/video/{fileId:guid}.mp4", async (Guid id, Guid fileId, AppDbContext db, FileService fileService, CancellationToken cancellation) =>
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
        });


        group.MapGet("{id:guid}/thumb/{fileId:guid}.jpg", async (Guid id, Guid fileId, AppDbContext db, FileService fileService, CancellationToken cancellation) =>
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
        });

        group.MapPut("{id:guid}/name", async (Guid id, [FromBody] NameInfo data, AppDbContext db, HttpContext httpContext, IHubContext<ClipHub, IClipHub> hub, CancellationToken cancellationToken) =>
        {
            var clip = await db.Clips.FirstAsync(x => x.Id == id, cancellationToken);
            clip.Name = data.Name;
            clip.Update();
            await db.SaveChangesAsync(cancellationToken);

            await hub.Clients.All.ClipUpdated(id);

        });

        return group;
    }
}
