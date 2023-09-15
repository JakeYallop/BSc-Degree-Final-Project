using Web.Entities;

namespace Web.Models;
public sealed class ClipData
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public DateTime DateRecorded { get; init; }
    public string Url { get; init; } = null!;
    public IEnumerable<DetectionData> Detections { get; init; } = Array.Empty<DetectionData>();
    public string? Thumbnail { get; init; }
    public IEnumerable<ClassificationData>? Classifications { get; init; }

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
                BoundingBox = new[] { x.BoundingBox.X, x.BoundingBox.Y, x.BoundingBox.Width, x.BoundingBox.Height },
            }),
            Thumbnail = ThumbnailUrlHelper.GetUrl(baseUrl, clip.Id, clip.ImageUsedForClassification),
            Classifications = clip.Classifications.Select(x => new ClassificationData()
            {
                Confidence = x.Confidence,
                Label = x.Label,
            }),
        };
    }
}
