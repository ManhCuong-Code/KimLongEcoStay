using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace KimLongEcoStay.Web.Services
{
    /// <summary>
    /// SignalR Hub phục vụ đồng bộ dữ liệu thời gian thực 2 chiều (Website ↔ SQL Server)
    /// Endpoint: /hubs/ecostay-sync
    /// </summary>
    public class EcoStaySyncHub : Hub
    {
        private static readonly ConcurrentDictionary<string, DateTime> _connectedClients = new();

        public static int ConnectedClientsCount => _connectedClients.Count;

        public override async Task OnConnectedAsync()
        {
            _connectedClients[Context.ConnectionId] = DateTime.UtcNow;
            await Clients.Caller.SendAsync("SyncConnected", new
            {
                connectionId = Context.ConnectionId,
                connectedClients = _connectedClients.Count,
                serverTime = DateTime.UtcNow
            });
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _connectedClients.TryRemove(Context.ConnectionId, out _);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task PingSync()
        {
            await Clients.Caller.SendAsync("PongSync", new
            {
                connectionId = Context.ConnectionId,
                connectedClients = _connectedClients.Count,
                serverTime = DateTime.UtcNow
            });
        }
    }
}

