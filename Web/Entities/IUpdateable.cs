namespace Web.Entities;
public interface IUpdateable
{
    DateTimeOffset? ModifiedAt { get; set; }
}
