using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace ServiceManagementApi.Hubs
{
    
    [Authorize]
    public class NotificationHub : Hub
    {
       
    }
}