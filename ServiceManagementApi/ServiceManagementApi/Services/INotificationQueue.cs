namespace ServiceManagementApi.Services
{
    using ServiceManagementApi.Models;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public interface INotificationQueue
    {
        void Enqueue(string userId, string message); 
        Task<NotificationModel> DequeueAsync(CancellationToken cancellationToken);
    }

    public class NotificationQueue : INotificationQueue
    {
        private readonly Channel<NotificationModel> _queue = Channel.CreateUnbounded<NotificationModel>();

        public void Enqueue(string userId, string message) =>
            _queue.Writer.TryWrite(new NotificationModel { UserId = userId, Message = message });

        public async Task<NotificationModel> DequeueAsync(CancellationToken cancellationToken) =>
            await _queue.Reader.ReadAsync(cancellationToken);
    }
}