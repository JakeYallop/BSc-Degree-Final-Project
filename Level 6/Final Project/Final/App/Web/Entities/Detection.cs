using System.Numerics;

namespace Web.Entities;

public sealed class Detection
{
    public Guid Id { get; init; }
    public Guid ClipId { get; init; }
    public TimeSpan Timestamp { get; init; }
    public BoundingBox BoundingBox { get; init; } = null!;
}

public class BoundingBox
{
    public BoundingBox(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public double GetArea() => Width * Height;
    public BoundingBox Grow(int amount, int maxWidth, int maxHeight)
    {
        var xMin = Math.Max(0, X - amount);
        var yMin = Math.Max(0, Y - amount);
        var xMax = Math.Min(maxWidth, Width + (amount * 2));
        var yMax = Math.Min(maxHeight, Height + (amount * 2));
        return new(xMin, yMin, xMax, yMax);
    }
}
