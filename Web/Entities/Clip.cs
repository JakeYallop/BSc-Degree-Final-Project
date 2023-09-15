namespace Web.Entities;
public sealed class Clip : ICreatable, IUpdateable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid FileId { get; set; }
    public DateTime DateRecorded { get; set; }
    public Guid? ImageUsedForClassification { get; set; }
    public ICollection<Detection> Detections { get; set; } = new List<Detection>();
    public ICollection<Classification> Classifications { get; set; } = new List<Classification>();
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ModifiedAt { get; set; }

    public void Update()
    {
        ModifiedAt = DateTime.UtcNow;
    }

}
