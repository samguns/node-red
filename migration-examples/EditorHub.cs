using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace NodeRed.EditorApi
{
    /// <summary>
    /// SignalR Hub for real-time editor communication
    /// Equivalent to WebSocket comms in Node-RED
    /// </summary>
    public class EditorHub : Hub
    {
        private readonly ILogger<EditorHub> _logger;

        public EditorHub(ILogger<EditorHub> logger)
        {
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Editor client connected: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Editor client disconnected: {ConnectionId}", Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Handle runtime events from editor
        /// </summary>
        public async Task RuntimeEvent(string eventId, object payload)
        {
            _logger.LogDebug("Received runtime event: {EventId}", eventId);
            
            // Broadcast to all connected clients
            await Clients.All.SendAsync("runtime-event", eventId, payload);
        }

        /// <summary>
        /// Handle node status updates
        /// </summary>
        public async Task NodeStatus(string nodeId, object status)
        {
            _logger.LogDebug("Node status update: {NodeId}", nodeId);
            
            // Broadcast to all connected clients
            await Clients.All.SendAsync("node-status", nodeId, status);
        }
    }

    /// <summary>
    /// Editor API Service interface
    /// </summary>
    public interface IEditorApiService
    {
        Task NotifyRuntimeEvent(string eventId, object payload);
        Task NotifyNodeStatus(string nodeId, object status);
    }

    /// <summary>
    /// Editor API Service implementation
    /// </summary>
    public class EditorApiService : IEditorApiService
    {
        private readonly IHubContext<EditorHub> _hubContext;
        private readonly ILogger<EditorApiService> _logger;

        public EditorApiService(
            IHubContext<EditorHub> hubContext,
            ILogger<EditorApiService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyRuntimeEvent(string eventId, object payload)
        {
            await _hubContext.Clients.All.SendAsync("runtime-event", eventId, payload);
        }

        public async Task NotifyNodeStatus(string nodeId, object status)
        {
            await _hubContext.Clients.All.SendAsync("node-status", nodeId, status);
        }
    }
}
