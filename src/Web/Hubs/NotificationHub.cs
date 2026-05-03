using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Web.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
}
