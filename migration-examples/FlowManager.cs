using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NodeRed.Runtime.Nodes;
using NodeRed.Runtime.Registry;

namespace NodeRed.Runtime.Flows
{
    /// <summary>
    /// Flow Manager - manages flow loading and execution
    /// Equivalent to flows module in @node-red/runtime
    /// </summary>
    public class FlowManager
    {
        private readonly NodeRegistry _nodeRegistry;
        private readonly ILogger<FlowManager> _logger;
        private readonly List<Flow> _activeFlows = new List<Flow>();
        private bool _flowsStarted = false;

        public FlowManager(NodeRegistry nodeRegistry, ILogger<FlowManager> logger)
        {
            _nodeRegistry = nodeRegistry;
            _logger = logger;
        }

        /// <summary>
        /// Get all flows
        /// </summary>
        public async Task<JArray> GetFlowsAsync()
        {
            var flows = new JArray();
            foreach (var flow in _activeFlows)
            {
                flows.Add(flow.ToJson());
            }
            return await Task.FromResult(flows);
        }

        /// <summary>
        /// Get a specific flow by ID
        /// </summary>
        public async Task<JObject> GetFlowAsync(string flowId)
        {
            var flow = _activeFlows.FirstOrDefault(f => f.Id == flowId);
            return await Task.FromResult(flow?.ToJson());
        }

        /// <summary>
        /// Set flows (deploy)
        /// </summary>
        public async Task SetFlowsAsync(JArray flows)
        {
            // Stop existing flows
            await StopFlowsAsync();

            // Clear active flows
            _activeFlows.Clear();

            // Parse and create flows
            foreach (var flowJson in flows)
            {
                if (flowJson is JObject flowObj)
                {
                    var flow = ParseFlow(flowObj);
                    _activeFlows.Add(flow);
                }
            }

            _logger.LogInformation("Loaded {Count} flows", _activeFlows.Count);
        }

        /// <summary>
        /// Load flows from storage
        /// </summary>
        public async Task LoadFlowsAsync()
        {
            // This will be called by storage system
            await Task.CompletedTask;
        }

        /// <summary>
        /// Start all flows
        /// </summary>
        public async Task StartFlowsAsync()
        {
            if (_flowsStarted)
            {
                _logger.LogWarning("Flows already started");
                return;
            }

            _logger.LogInformation("Starting flows");

            foreach (var flow in _activeFlows)
            {
                try
                {
                    await flow.StartAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting flow {FlowId}", flow.Id);
                }
            }

            _flowsStarted = true;
            _logger.LogInformation("Flows started");
        }

        /// <summary>
        /// Stop all flows
        /// </summary>
        public async Task StopFlowsAsync()
        {
            if (!_flowsStarted)
            {
                return;
            }

            _logger.LogInformation("Stopping flows");

            foreach (var flow in _activeFlows)
            {
                try
                {
                    await flow.StopAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping flow {FlowId}", flow.Id);
                }
            }

            _flowsStarted = false;
            _logger.LogInformation("Flows stopped");
        }

        /// <summary>
        /// Validate flows
        /// </summary>
        public async Task<FlowValidationResult> ValidateFlowsAsync(JArray flows)
        {
            try
            {
                // Basic validation
                foreach (var flowJson in flows)
                {
                    if (flowJson is JObject flowObj)
                    {
                        // Validate flow structure
                        if (!flowObj.ContainsKey("id"))
                        {
                            return new FlowValidationResult
                            {
                                IsValid = false,
                                Error = "Flow missing id"
                            };
                        }
                    }
                }

                return await Task.FromResult(new FlowValidationResult { IsValid = true });
            }
            catch (Exception ex)
            {
                return new FlowValidationResult
                {
                    IsValid = false,
                    Error = ex.Message
                };
            }
        }

        private Flow ParseFlow(JObject flowJson)
        {
            var flow = new Flow
            {
                Id = flowJson["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                Type = flowJson["type"]?.ToString() ?? "tab",
                Label = flowJson["label"]?.ToString(),
                Disabled = flowJson["disabled"]?.ToObject<bool>() ?? false
            };

            // Parse nodes in flow
            var nodes = flowJson["nodes"] as JArray;
            if (nodes != null)
            {
                foreach (var nodeJson in nodes)
                {
                    if (nodeJson is JObject nodeObj)
                    {
                        var nodeDef = new NodeDefinition
                        {
                            Id = nodeObj["id"]?.ToString(),
                            Type = nodeObj["type"]?.ToString(),
                            FlowId = flow.Id,
                            Config = nodeObj
                        };

                        var node = _nodeRegistry.CreateNode(nodeDef);
                        flow.AddNode(node);
                    }
                }
            }

            return flow;
        }
    }

    public class Flow
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Label { get; set; }
        public bool Disabled { get; set; }
        private readonly List<NodeBase> _nodes = new List<NodeBase>();

        public void AddNode(NodeBase node)
        {
            _nodes.Add(node);
        }

        public async Task StartAsync()
        {
            // Initialize nodes
            foreach (var node in _nodes)
            {
                // Setup node event handlers
                node.OnInput += (sender, args) =>
                {
                    // Handle node input
                };
            }
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            foreach (var node in _nodes)
            {
                node.Close();
            }
            await Task.CompletedTask;
        }

        public JObject ToJson()
        {
            return new JObject
            {
                ["id"] = Id,
                ["type"] = Type,
                ["label"] = Label,
                ["disabled"] = Disabled
            };
        }
    }

    public class FlowValidationResult
    {
        public bool IsValid { get; set; }
        public string Error { get; set; }
    }
}
