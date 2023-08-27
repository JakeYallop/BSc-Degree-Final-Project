public sealed class Clip : ICreatable, IUpdateable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid FileId { get; set; }
    public DateTime DateRecorded { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public ICollection<Detection> Detections { get; set; } = Array.Empty<Detection>();
    public DateTimeOffset? ModifiedAt { get; set; }

    public void Update()
    {
        ModifiedAt = DateTime.UtcNow;
    }

}
