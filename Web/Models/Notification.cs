namespace Web.Models;

public class Notification
{
    public Notification(string title, string body)
    {
        Title = title;
        Body = body;
    }

    public string Title { get; init; }
    public string Body { get; init; }
}
