using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jint;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NodeRed.Runtime.Nodes;

namespace NodeRed.Runtime.Registry
{
    /// <summary>
    /// Node Registry - manages node types and instances
    /// Equivalent to @node-red/registry
    /// </summary>
    public class NodeRegistry
    {
        private readonly Dictionary<string, NodeType> _nodeTypes = new Dictionary<string, NodeType>();
        private readonly Dictionary<string, NodeBase> _activeNodes = new Dictionary<string, NodeBase>();
        private readonly ILogger<NodeRegistry> _logger;
        private readonly FlowManager _flowManager;
        private readonly JavaScriptEngine _jsEngine;

        public NodeRegistry(ILogger<NodeRegistry> logger, FlowManager flowManager)
        {
            _logger = logger;
            _flowManager = flowManager;
            _jsEngine = new JavaScriptEngine();
        }

        /// <summary>
        /// Register a node type
        /// </summary>
        public void RegisterType(string typeName, NodeType nodeType)
        {
            if (_nodeTypes.ContainsKey(typeName))
            {
                _logger.LogWarning("Node type {Type} already registered", typeName);
            }
            _nodeTypes[typeName] = nodeType;
            _logger.LogInformation("Registered node type: {Type}", typeName);
        }

        /// <summary>
        /// Load a JavaScript node file
        /// </summary>
        public async Task LoadJavaScriptNodeAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Node file not found: {filePath}");
            }

            var jsCode = await File.ReadAllTextAsync(filePath);
            
            // Execute JavaScript file in Jint
            var engine = new Engine();
            engine.Execute(jsCode);

            // Extract node definition
            var nodeTypeName = ExtractNodeType(jsCode);
            
            // Register node type wrapper
            var nodeType = new NodeType
            {
                Name = nodeTypeName,
                Type = "javascript",
                FilePath = filePath,
                JavaScriptCode = jsCode
            };

            RegisterType(nodeTypeName, nodeType);
        }

        /// <summary>
        /// Create a node instance
        /// </summary>
        public NodeBase CreateNode(NodeDefinition def)
        {
            if (!_nodeTypes.TryGetValue(def.Type, out var nodeType))
            {
                throw new InvalidOperationException($"Node type not found: {def.Type}");
            }

            NodeBase node;
            
            if (nodeType.Type == "javascript")
            {
                // Create JavaScript node wrapper
                node = new JavaScriptNodeWrapper(def, nodeType, this, _flowManager, _jsEngine);
            }
            else
            {
                // Create C# node instance
                node = Activator.CreateInstance(nodeType.CSharpType, def, this, _flowManager) as NodeBase;
            }

            _activeNodes[def.Id] = node;
            return node;
        }

        /// <summary>
        /// Get a node instance by ID
        /// </summary>
        public NodeBase GetNode(string nodeId)
        {
            _activeNodes.TryGetValue(nodeId, out var node);
            return node;
        }

        /// <summary>
        /// Initialize registry
        /// </summary>
        public async Task InitializeAsync()
        {
            // Load core nodes
            await LoadCoreNodesAsync();
        }

        /// <summary>
        /// Load modules from node_modules directory
        /// </summary>
        public async Task LoadModulesAsync()
        {
            var nodeModulesPath = Path.Combine(Directory.GetCurrentDirectory(), "node_modules");
            if (!Directory.Exists(nodeModulesPath))
            {
                _logger.LogWarning("node_modules directory not found");
                return;
            }

            // Scan for Node-RED nodes
            var nodeDirs = Directory.GetDirectories(nodeModulesPath, "*", SearchOption.AllDirectories)
                .Where(d => File.Exists(Path.Combine(d, "package.json")));

            foreach (var dir in nodeDirs)
            {
                try
                {
                    await LoadNodeModuleAsync(dir);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading module from {Directory}", dir);
                }
            }
        }

        private async Task LoadNodeModuleAsync(string moduleDir)
        {
            var packageJsonPath = Path.Combine(moduleDir, "package.json");
            if (!File.Exists(packageJsonPath))
            {
                return;
            }

            var packageJson = JObject.Parse(await File.ReadAllTextAsync(packageJsonPath));
            var nodeRedSection = packageJson["node-red"];
            
            if (nodeRedSection == null)
            {
                return; // Not a Node-RED module
            }

            var nodes = nodeRedSection["nodes"] as JObject;
            if (nodes == null)
            {
                return;
            }

            foreach (var nodeProp in nodes.Properties())
            {
                var nodeFile = Path.Combine(moduleDir, nodeProp.Value.ToString());
                if (File.Exists(nodeFile))
                {
                    await LoadJavaScriptNodeAsync(nodeFile);
                }
            }
        }

        private async Task LoadCoreNodesAsync()
        {
            // Load core nodes from @node-red/nodes package
            var coreNodesPath = Path.Combine("packages", "node_modules", "@node-red", "nodes", "core");
            if (Directory.Exists(coreNodesPath))
            {
                var nodeFiles = Directory.GetFiles(coreNodesPath, "*.js", SearchOption.AllDirectories);
                foreach (var file in nodeFiles)
                {
                    try
                    {
                        await LoadJavaScriptNodeAsync(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading core node {File}", file);
                    }
                }
            }
        }

        private string ExtractNodeType(string jsCode)
        {
            // Simple extraction - in production, use proper parsing
            var match = System.Text.RegularExpressions.Regex.Match(
                jsCode, 
                @"RED\.nodes\.registerType\(['""](.+?)['""]"
            );
            return match.Success ? match.Groups[1].Value : "unknown";
        }

        /// <summary>
        /// Close all nodes
        /// </summary>
        public void CloseAllNodes()
        {
            foreach (var node in _activeNodes.Values)
            {
                try
                {
                    node.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing node {NodeId}", node.Id);
                }
            }
            _activeNodes.Clear();
        }
    }

    public class NodeType
    {
        public string Name { get; set; }
        public string Type { get; set; } // "javascript" or "csharp"
        public string FilePath { get; set; }
        public string JavaScriptCode { get; set; }
        public Type CSharpType { get; set; }
    }

    public class JavaScriptEngine
    {
        private readonly Dictionary<string, Engine> _engines = new Dictionary<string, Engine>();

        public Engine GetEngine(string nodeId)
        {
            if (!_engines.ContainsKey(nodeId))
            {
                _engines[nodeId] = new Engine();
            }
            return _engines[nodeId];
        }
    }
}
