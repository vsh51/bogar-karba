using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Web.Options;
using Web.Services;

namespace Web.Hubs;

[Authorize]
public sealed class NotificationHub(
    ConnectionTracker tracker,
    IOptions<NotificationOptions> options) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var count = tracker.Increment();

        if (count > options.Value.MaxConnections)
        {
            await Clients.Caller.SendAsync("TooManyConnections");
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        tracker.Decrement();
        return base.OnDisconnectedAsync(exception);
    }
}
