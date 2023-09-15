namespace Web.Models;
public sealed class ClipInfo
{
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public DateTime DateRecorded { get; init; }
    public DetectionInfoData[] Detections { get; init; } = Array.Empty<DetectionInfoData>();
}
