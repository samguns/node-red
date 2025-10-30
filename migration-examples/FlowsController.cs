using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NodeRed.Runtime;
using NodeRed.Runtime.Flows;
using Newtonsoft.Json.Linq;

namespace NodeRed.EditorApi.Controllers
{
    /// <summary>
    /// Flows API Controller
    /// Maintains compatibility with Node-RED admin API
    /// </summary>
    [ApiController]
    [Route("flows")]
    public class FlowsController : ControllerBase
    {
        private readonly NodeRedRuntime _runtime;
        private readonly FlowManager _flowManager;
        private readonly ILogger<FlowsController> _logger;

        public FlowsController(
            NodeRedRuntime runtime,
            FlowManager flowManager,
            ILogger<FlowsController> logger)
        {
            _runtime = runtime;
            _flowManager = flowManager;
            _logger = logger;
        }

        /// <summary>
        /// GET /flows
        /// Get current flows
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFlows()
        {
            try
            {
                var flows = await _flowManager.GetFlowsAsync();
                return Ok(flows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flows");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST /flows
        /// Set flows (deploy)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SetFlows([FromBody] JArray flows)
        {
            try
            {
                // Validate flows
                var validationResult = await _flowManager.ValidateFlowsAsync(flows);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { error = validationResult.Error });
                }

                // Set flows
                await _flowManager.SetFlowsAsync(flows);

                // Start flows
                await _flowManager.StartFlowsAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting flows");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET /flows/:id
        /// Get a specific flow
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFlow(string id)
        {
            try
            {
                var flow = await _flowManager.GetFlowAsync(id);
                if (flow == null)
                {
                    return NotFound(new { error = "Flow not found" });
                }
                return Ok(flow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flow {FlowId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
