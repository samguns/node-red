using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jint;
using Newtonsoft.Json.Linq;

namespace NodeRed.Runtime.Nodes
{
    /// <summary>
    /// Base class for all Node-RED nodes in C#
    /// Equivalent to Node.js Node class
    /// </summary>
    public abstract class NodeBase : IDisposable
    {
        public string Id { get; protected set; }
        public string Type { get; protected set; }
        public string FlowId { get; protected set; }
        public string GroupId { get; protected set; }
        public string Name { get; protected set; }
        
        protected List<string[]> Wires { get; private set; }
        protected NodeRegistry Registry { get; private set; }
        protected FlowManager FlowManager { get; private set; }
        
        private readonly List<Action> _closeCallbacks = new List<Action>();
        private bool _disposed = false;

        public event EventHandler<NodeMessageEventArgs> OnInput;
        public event EventHandler OnClose;

        protected NodeBase(NodeDefinition def, NodeRegistry registry, FlowManager flowManager)
        {
            Id = def.Id;
            Type = def.Type;
            FlowId = def.FlowId;
            GroupId = def.GroupId;
            Name = def.Name;
            Registry = registry;
            FlowManager = flowManager;
            Wires = def.Wires ?? new List<string[]>();
        }

        /// <summary>
        /// Send a message to connected nodes
        /// Equivalent to node.send() in Node.js
        /// </summary>
        public virtual void Send(params object[] messages)
        {
            if (Wires.Count == 0) return;

            var payloads = new List<object>();
            foreach (var msg in messages)
            {
                payloads.Add(msg);
            }

            // Route messages to connected nodes
            for (int i = 0; i < Wires.Count && i < payloads.Count; i++)
            {
                var wireGroup = Wires[i];
                foreach (var targetNodeId in wireGroup)
                {
                    var targetNode = Registry.GetNode(targetNodeId);
                    if (targetNode != null)
                    {
                        targetNode.Receive(payloads[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Receive a message (called by Send)
        /// </summary>
        public virtual void Receive(object msg)
        {
            OnInput?.Invoke(this, new NodeMessageEventArgs { Message = msg });
        }

        /// <summary>
        /// Update node wiring configuration
        /// </summary>
        public virtual void UpdateWires(List<string[]> wires)
        {
            Wires = wires ?? new List<string[]>();
        }

        /// <summary>
        /// Called when node receives input
        /// Override in derived classes
        /// </summary>
        public virtual Task OnReceiveAsync(object msg)
        {
            // Default implementation - override in derived classes
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when node is closed
        /// </summary>
        public virtual void Close()
        {
            foreach (var callback in _closeCallbacks)
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    // Log error but continue closing
                    Console.Error.WriteLine($"Error in close callback: {ex.Message}");
                }
            }
            
            OnClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Register a callback to be called when node closes
        /// </summary>
        public void OnCloseCallback(Action callback)
        {
            _closeCallbacks.Add(callback);
        }

        /// <summary>
        /// Get node status for status reporting
        /// </summary>
        public virtual NodeStatus GetStatus()
        {
            return new NodeStatus
            {
                Id = Id,
                Type = Type,
                Status = "active"
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Close();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class NodeDefinition
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string FlowId { get; set; }
        public string GroupId { get; set; }
        public string Name { get; set; }
        public List<string[]> Wires { get; set; }
        public JObject Config { get; set; }
    }

    public class NodeMessageEventArgs : EventArgs
    {
        public object Message { get; set; }
    }

    public class NodeStatus
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Text { get; set; }
        public string Fill { get; set; }
        public string Shape { get; set; }
    }
}
