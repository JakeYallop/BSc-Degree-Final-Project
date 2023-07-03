using System.Numerics;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");


app.MapPost("/clip", (byte[] data) =>
{
});

app.Run();


public class ClipData
{
    public required DateTimeOffset Date { get; init; }
    public required byte[] Data { get; init; }
    public required Vector2[] Boundary { get; init; }
}