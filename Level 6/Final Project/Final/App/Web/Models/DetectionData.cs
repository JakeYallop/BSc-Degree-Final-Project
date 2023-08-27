public sealed class DetectionData
{
    public int Timestamp { get; init; }
    public float[] BoundingBox { get; init; } = Array.Empty<float>();
}
