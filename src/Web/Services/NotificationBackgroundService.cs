using Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Web.Hubs;
using Web.Options;

namespace Web.Services;

public sealed class NotificationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IHubContext<NotificationHub> hubContext,
    IOptions<NotificationOptions> options,
    ILogger<NotificationBackgroundService> logger) : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DeliverPendingAsync(stoppingToken);
            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task DeliverPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        var unsent = await notifications.GetUnsentAsync(cancellationToken);

        if (unsent.Count == 0)
        {
            return;
        }

        var sentIds = new List<Guid>(unsent.Count);

        foreach (var notification in unsent)
        {
            await hubContext.Clients
                .User(notification.UserId)
                .SendAsync("ReceiveNotification", notification.Message, cancellationToken);

            sentIds.Add(notification.Id);

            logger.LogInformation(
                "Notification {NotificationId} delivered to user {UserId}",
                notification.Id,
                notification.UserId);
        }

        await notifications.MarkSentAsync(sentIds, cancellationToken);
    }
}
