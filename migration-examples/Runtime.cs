using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodeRed.Runtime.Flows;
using NodeRed.Runtime.Nodes;
using NodeRed.Runtime.Registry;
using NodeRed.Runtime.Storage;

namespace NodeRed.Runtime
{
    /// <summary>
    /// Main Runtime class - equivalent to @node-red/runtime
    /// </summary>
    public class NodeRedRuntime
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NodeRedRuntime> _logger;
        private readonly NodeRegistry _nodeRegistry;
        private readonly FlowManager _flowManager;
        private readonly FlowStorage _storage;
        private bool _started = false;

        public NodeRegistry Nodes => _nodeRegistry;
        public FlowManager Flows => _flowManager;
        public FlowStorage Storage => _storage;

        public NodeRedRuntime(
            IServiceProvider serviceProvider,
            ILogger<NodeRedRuntime> logger,
            NodeRegistry nodeRegistry,
            FlowManager flowManager,
            FlowStorage storage)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _nodeRegistry = nodeRegistry;
            _flowManager = flowManager;
            _storage = storage;
        }

        /// <summary>
        /// Initialize the runtime
        /// Equivalent to runtime.init()
        /// </summary>
        public async Task InitializeAsync(RuntimeSettings settings)
        {
            if (_started)
            {
                throw new InvalidOperationException("Runtime already initialized");
            }

            _logger.LogInformation("Initializing Node-RED Runtime");

            // Initialize storage
            await _storage.InitializeAsync();

            // Load settings
            // await _settings.LoadAsync();

            // Initialize node registry
            await _nodeRegistry.InitializeAsync();

            // Load flows
            await _flowManager.LoadFlowsAsync();

            _logger.LogInformation("Node-RED Runtime initialized");
        }

        /// <summary>
        /// Start the runtime
        /// Equivalent to runtime.start()
        /// </summary>
        public async Task StartAsync()
        {
            if (_started)
            {
                _logger.LogWarning("Runtime already started");
                return;
            }

            _logger.LogInformation("Starting Node-RED Runtime");

            // Load node modules
            await _nodeRegistry.LoadModulesAsync();

            // Load flows from storage
            var flows = await _storage.LoadFlowsAsync();
            if (flows != null)
            {
                await _flowManager.SetFlowsAsync(flows);
            }

            // Start flows
            await _flowManager.StartFlowsAsync();

            _started = true;
            _logger.LogInformation("Node-RED Runtime started");
        }

        /// <summary>
        /// Stop the runtime
        /// Equivalent to runtime.stop()
        /// </summary>
        public async Task StopAsync()
        {
            if (!_started)
            {
                return;
            }

            _logger.LogInformation("Stopping Node-RED Runtime");

            // Stop all flows
            await _flowManager.StopFlowsAsync();

            // Close all nodes
            _nodeRegistry.CloseAllNodes();

            _started = false;
            _logger.LogInformation("Node-RED Runtime stopped");
        }

        /// <summary>
        /// Check if runtime is started
        /// </summary>
        public bool IsStarted => _started;
    }

    /// <summary>
    /// Runtime settings
    /// </summary>
    public class RuntimeSettings
    {
        public string UserDir { get; set; } = ".node-red";
        public string SettingsFile { get; set; }
        public string FlowsFile { get; set; } = "flows.json";
        public bool ReadOnly { get; set; } = false;
        public string HttpAdminRoot { get; set; } = "/";
        public string HttpNodeRoot { get; set; } = "/";
        public int? Port { get; set; }
        public string Host { get; set; } = "0.0.0.0";
        public bool DisableEditor { get; set; } = false;
        public bool AutoInstallModules { get; set; } = false;
        public int RuntimeMetricInterval { get; set; } = 15000;
    }
}
