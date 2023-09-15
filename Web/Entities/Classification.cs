namespace Web.Entities;

public class Classification : ICreatable
{
    public Guid Id { get; init; }
    public Guid ClipId { get; init; }
    public double Confidence { get; init; }
    public string Label { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
}
