using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace NodeRed.Runtime.Storage
{
    /// <summary>
    /// Flow Storage - handles persistence of flows
    /// Equivalent to storage module in @node-red/runtime
    /// </summary>
    public class FlowStorage
    {
        private readonly ILogger<FlowStorage> _logger;
        private readonly string _flowsFile;
        private readonly string _userDir;

        public FlowStorage(ILogger<FlowStorage> logger, RuntimeSettings settings)
        {
            _logger = logger;
            _userDir = settings.UserDir ?? ".node-red";
            _flowsFile = Path.Combine(_userDir, settings.FlowsFile ?? "flows.json");
            
            // Ensure user directory exists
            if (!Directory.Exists(_userDir))
            {
                Directory.CreateDirectory(_userDir);
            }
        }

        /// <summary>
        /// Initialize storage
        /// </summary>
        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing flow storage at {Path}", _userDir);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Load flows from storage
        /// </summary>
        public async Task<JArray> LoadFlowsAsync()
        {
            try
            {
                if (!File.Exists(_flowsFile))
                {
                    _logger.LogInformation("Flows file not found, starting with empty flows");
                    return new JArray();
                }

                var json = await File.ReadAllTextAsync(_flowsFile);
                var flows = JArray.Parse(json);
                
                _logger.LogInformation("Loaded flows from {Path}", _flowsFile);
                return flows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading flows from {Path}", _flowsFile);
                return new JArray();
            }
        }

        /// <summary>
        /// Save flows to storage
        /// </summary>
        public async Task SaveFlowsAsync(JArray flows)
        {
            try
            {
                var json = flows.ToString(Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(_flowsFile, json);
                
                _logger.LogInformation("Saved flows to {Path}", _flowsFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving flows to {Path}", _flowsFile);
                throw;
            }
        }

        /// <summary>
        /// Load settings from storage
        /// </summary>
        public async Task<JObject> LoadSettingsAsync()
        {
            var settingsFile = Path.Combine(_userDir, "settings.json");
            try
            {
                if (!File.Exists(settingsFile))
                {
                    return new JObject();
                }

                var json = await File.ReadAllTextAsync(settingsFile);
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings from {Path}", settingsFile);
                return new JObject();
            }
        }

        /// <summary>
        /// Save settings to storage
        /// </summary>
        public async Task SaveSettingsAsync(JObject settings)
        {
            var settingsFile = Path.Combine(_userDir, "settings.json");
            try
            {
                var json = settings.ToString(Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(settingsFile, json);
                
                _logger.LogInformation("Saved settings to {Path}", settingsFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings to {Path}", settingsFile);
                throw;
            }
        }
    }
}
