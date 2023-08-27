using System.Numerics;

public sealed class Detection
{
    public Guid Id { get; init; }
    public Guid ClipId { get; init; }
    public TimeSpan Timestamp { get; init; }
    public Vector4 BoundingBox { get; init; }
}
