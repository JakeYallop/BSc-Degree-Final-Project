namespace Web.Models;
public sealed class DetectionData
{
    public double Timestamp { get; init; }
    public float[] BoundingBox { get; init; } = Array.Empty<float>();
}
