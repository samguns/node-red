using System;
using System.Threading.Tasks;
using Jint;
using Microsoft.Extensions.Logging;
using NodeRed.Runtime.Flows;
using NodeRed.Runtime.Nodes;
using NodeRed.Runtime.Registry;

namespace NodeRed.Runtime.Nodes
{
    /// <summary>
    /// Wrapper for JavaScript-based nodes
    /// Executes JavaScript node code using Jint
    /// </summary>
    public class JavaScriptNodeWrapper : NodeBase
    {
        private readonly NodeType _nodeType;
        private readonly JavaScriptEngine _jsEngine;
        private readonly ILogger<JavaScriptNodeWrapper> _logger;
        private Engine _engine;

        public JavaScriptNodeWrapper(
            NodeDefinition def,
            NodeType nodeType,
            NodeRegistry registry,
            FlowManager flowManager,
            JavaScriptEngine jsEngine) 
            : base(def, registry, flowManager)
        {
            _nodeType = nodeType;
            _jsEngine = jsEngine;
            _logger = registry.GetLogger<JavaScriptNodeWrapper>();
            InitializeJavaScriptEngine();
        }

        private void InitializeJavaScriptEngine()
        {
            _engine = _jsEngine.GetEngine(Id);

            // Setup Node-RED API in JavaScript context
            _engine.SetValue("RED", new
            {
                nodes = new
                {
                    registerType = new Action<string, object>(RegisterType),
                    createNode = new Action<object, object>(CreateNode)
                },
                log = new
                {
                    warn = new Action<object>(msg => _logger.LogWarning(msg?.ToString())),
                    error = new Action<object>(msg => _logger.LogError(msg?.ToString())),
                    info = new Action<object>(msg => _logger.LogInformation(msg?.ToString())),
                    debug = new Action<object>(msg => _logger.LogDebug(msg?.ToString()))
                }
            });

            // Execute the node's JavaScript code
            try
            {
                _engine.Execute(_nodeType.JavaScriptCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing JavaScript for node {NodeId}", Id);
            }
        }

        public override async Task OnReceiveAsync(object msg)
        {
            try
            {
                // Convert C# object to JavaScript object
                var jsMsg = ConvertToJavaScriptObject(msg);

                // Call the node's input handler
                _engine.Invoke("onInput", jsMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JavaScript node {NodeId}", Id);
            }

            await Task.CompletedTask;
        }

        private object ConvertToJavaScriptObject(object obj)
        {
            // Convert C# object to JavaScript object
            // This is simplified - in production, use proper serialization
            if (obj == null) return null;
            
            // For now, pass as JSON string and parse in JS
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            return _engine.Evaluate($"JSON.parse('{json}')");
        }

        private void RegisterType(string typeName, object nodeDef)
        {
            // JavaScript node registration
            // This is called from within the JavaScript code
        }

        private void CreateNode(object config, object def)
        {
            // JavaScript node creation
            // This is called from within the JavaScript code
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cleanup JavaScript engine
                _engine?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
