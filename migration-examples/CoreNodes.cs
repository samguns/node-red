using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodeRed.Runtime.Nodes;

namespace NodeRed.Runtime.Nodes.Core
{
    /// <summary>
    /// Inject Node - injects messages into flows
    /// C# implementation of core inject node
    /// </summary>
    public class InjectNode : NodeBase
    {
        private readonly ILogger<InjectNode> _logger;
        private System.Threading.Timer _timer;
        private readonly Dictionary<string, object> _config;

        public InjectNode(
            NodeDefinition def,
            NodeRegistry registry,
            FlowManager flowManager,
            ILogger<InjectNode> logger) 
            : base(def, registry, flowManager)
        {
            _logger = logger;
            _config = ParseConfig(def.Config);
            
            // Setup input handler
            OnInput += async (sender, args) =>
            {
                await HandleInputAsync(args.Message);
            };
        }

        public override async Task OnReceiveAsync(object msg)
        {
            // Inject node can be triggered by input or timer
            await SendPayloadAsync();
        }

        /// <summary>
        /// Start the inject node (sets up timer if needed)
        /// </summary>
        public async Task StartAsync()
        {
            var repeat = _config.ContainsKey("repeat") ? 
                TimeSpan.FromSeconds(Convert.ToDouble(_config["repeat"])) : 
                (TimeSpan?)null;

            if (repeat.HasValue && repeat.Value.TotalSeconds > 0)
            {
                _timer = new System.Threading.Timer(
                    async _ => await SendPayloadAsync(),
                    null,
                    TimeSpan.Zero,
                    repeat.Value
                );
                _logger.LogInformation("Inject node {NodeId} started with repeat interval {Interval}", Id, repeat);
            }
            else
            {
                // One-time inject
                await SendPayloadAsync();
            }

            await Task.CompletedTask;
        }

        public override void Close()
        {
            _timer?.Dispose();
            base.Close();
        }

        private async Task SendPayloadAsync()
        {
            var payload = _config.ContainsKey("payload") ? _config["payload"] : "";
            var topic = _config.ContainsKey("topic") ? _config["topic"] : null;

            var msg = new
            {
                payload = payload,
                topic = topic,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            Send(msg);
            await Task.CompletedTask;
        }

        private async Task HandleInputAsync(object msg)
        {
            // When inject node receives input, send its configured payload
            await SendPayloadAsync();
        }

        private Dictionary<string, object> ParseConfig(JObject config)
        {
            var result = new Dictionary<string, object>();
            
            if (config != null)
            {
                foreach (var prop in config.Properties())
                {
                    result[prop.Name] = prop.Value?.ToObject<object>();
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Debug Node - outputs messages to debug panel
    /// C# implementation of core debug node
    /// </summary>
    public class DebugNode : NodeBase
    {
        private readonly ILogger<DebugNode> _logger;
        private readonly IEditorApiService _editorApi;

        public DebugNode(
            NodeDefinition def,
            NodeRegistry registry,
            FlowManager flowManager,
            ILogger<DebugNode> logger,
            IEditorApiService editorApi) 
            : base(def, registry, flowManager)
        {
            _logger = logger;
            _editorApi = editorApi;
            
            OnInput += async (sender, args) =>
            {
                await HandleInputAsync(args.Message);
            };
        }

        public override async Task OnReceiveAsync(object msg)
        {
            await HandleInputAsync(msg);
        }

        private async Task HandleInputAsync(object msg)
        {
            // Output to debug panel
            var output = new
            {
                id = Id,
                name = Name,
                msg = msg,
                timestamp = DateTimeOffset.UtcNow
            };

            _logger.LogDebug("Debug node {NodeId}: {Message}", Id, msg);
            
            // Send to editor via SignalR
            if (_editorApi != null)
            {
                await _editorApi.NotifyRuntimeEvent("debug", output);
            }
        }
    }
}
