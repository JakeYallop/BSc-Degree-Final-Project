using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Routes;

public static class AdminApiEndpoints
{
    public static RouteGroupBuilder MapAdminApiEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("clearall", async (AppDbContext db, ILoggerFactory factory, CancellationToken cancellation) =>
        {
            var logger = factory.CreateLogger("AdminApi");
            await db.Database.EnsureDeletedAsync(cancellation);
            await db.Database.EnsureCreatedAsync(cancellation);
            try
            {
                Directory.Delete("store", true);
                Directory.CreateDirectory("store");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error clearing down clips directory");
            }
        }).WithDescription("Clear and delete all resources")
        .WithSummary("Only available when running the API in Debug mode")
        .WithOpenApi();


        group.MapPut("push", ([FromServices] NotificationsClient client) =>
        {
            return client.NotifyAsync(new Notification("Test Notification", "Test notification from the API"));
        }).WithDescription("Clear and delete all resources")
        .WithSummary("Only available when running the API in Debug mode");

        return group;
    }
}
