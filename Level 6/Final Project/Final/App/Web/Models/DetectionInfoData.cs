namespace Web.Models;
public sealed class DetectionInfoData
{
    public int Timestamp { get; init; }
    public float[] BoundingBox { get; init; } = Array.Empty<float>();
}
