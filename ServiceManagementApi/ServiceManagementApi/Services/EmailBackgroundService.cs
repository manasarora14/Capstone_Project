using Microsoft.AspNetCore.SignalR;
using ServiceManagementApi.Hubs;

namespace ServiceManagementApi.Services
{

    public class EmailBackgroundService : BackgroundService
    {
        private readonly ILogger<EmailBackgroundService> _logger;
        private readonly INotificationQueue _queue;
        private readonly IHubContext<NotificationHub> _hubContext;

        public EmailBackgroundService(
            ILogger<EmailBackgroundService> logger,
            INotificationQueue queue,
            IHubContext<NotificationHub> hubContext)
        {
            _logger = logger;
            _queue = queue;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var notification = await _queue.DequeueAsync(stoppingToken);

                _logger.LogInformation("Pushing notification to User {User}: {Message}",
                    notification.UserId, notification.Message);

                await _hubContext.Clients.User(notification.UserId)
                    .SendAsync("ReceiveNotification", notification.Message, stoppingToken);

            }
        }
    }
}
