namespace Web;
public static class HttpContextExtensions
{
    public static string BaseUrl(this HttpContext context) => $"{context.Request.Scheme}://{context.Request.Host}";
}
