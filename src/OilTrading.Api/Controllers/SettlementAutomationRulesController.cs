using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Commands.SettlementAutomationRules;
using OilTrading.Application.Queries.SettlementAutomationRules;
using FluentValidation;
using System.Text.Json;

namespace OilTrading.Api.Controllers;

/// <summary>
/// REST API Controller for managing settlement automation rules
/// Provides endpoints for CRUD operations, testing, and execution of settlement automation rules
/// </summary>
[ApiController]
[Route("api/settlement-automation-rules")]
public class SettlementAutomationRulesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SettlementAutomationRulesController> _logger;

    public SettlementAutomationRulesController(
        IMediator mediator,
        ILogger<SettlementAutomationRulesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new settlement automation rule
    /// </summary>
    /// <param name="command">The command to create the rule</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created rule with all details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SettlementAutomationRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSettlementAutomationRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating settlement automation rule: {RuleName}", command.Name);

            var result = await _mediator.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation error creating rule: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating settlement automation rule");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to create rule" });
        }
    }

    /// <summary>
    /// Get a settlement automation rule by ID
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rule details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SettlementAutomationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting settlement automation rule: {RuleId}", id);

            var result = await _mediator.Send(new GetSettlementAutomationRuleQuery { RuleId = id }, cancellationToken);

            if (result == null)
                return NotFound(new { error = $"Rule {id} not found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settlement automation rule: {RuleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve rule" });
        }
    }

    /// <summary>
    /// Get all settlement automation rules with optional filtering
    /// </summary>
    /// <param name="isEnabled">Filter by enabled status</param>
    /// <param name="ruleType">Filter by rule type</param>
    /// <param name="status">Filter by rule status</param>
    /// <param name="pageNum">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of rules matching the criteria</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<SettlementAutomationRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isEnabled = null,
        [FromQuery] string? ruleType = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNum = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting all settlement automation rules");

            var query = new GetAllSettlementAutomationRulesQuery
            {
                IsEnabled = isEnabled,
                RuleType = ruleType,
                Status = status,
                PageNum = pageNum,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settlement automation rules");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve rules" });
        }
    }

    /// <summary>
    /// Update an existing settlement automation rule
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="command">The update command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated rule</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SettlementAutomationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSettlementAutomationRuleCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != command.Id)
                return BadRequest(new { error = "Route ID does not match command ID" });

            _logger.LogInformation("Updating settlement automation rule: {RuleId}", id);

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation error updating rule: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Rule not found: {RuleId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settlement automation rule: {RuleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to update rule" });
        }
    }

    /// <summary>
    /// Enable a settlement automation rule
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="modifiedBy">The user making the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated rule</returns>
    [HttpPatch("{id}/enable")]
    [ProducesResponseType(typeof(SettlementAutomationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Enable(
        [FromRoute] Guid id,
        [FromQuery] string modifiedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(modifiedBy))
                return BadRequest(new { error = "modifiedBy parameter is required" });

            _logger.LogInformation("Enabling settlement automation rule: {RuleId}", id);

            var command = new EnableSettlementAutomationRuleCommand
            {
                RuleId = id,
                ModifiedBy = modifiedBy
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Rule not found: {RuleId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling settlement automation rule: {RuleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to enable rule" });
        }
    }

    /// <summary>
    /// Disable a settlement automation rule
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="modifiedBy">The user making the change</param>
    /// <param name="reason">The reason for disabling (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated rule</returns>
    [HttpPatch("{id}/disable")]
    [ProducesResponseType(typeof(SettlementAutomationRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Disable(
        [FromRoute] Guid id,
        [FromQuery] string modifiedBy,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(modifiedBy))
                return BadRequest(new { error = "modifiedBy parameter is required" });

            _logger.LogInformation("Disabling settlement automation rule: {RuleId}", id);

            var command = new DisableSettlementAutomationRuleCommand
            {
                RuleId = id,
                Reason = reason,
                ModifiedBy = modifiedBy
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Rule not found: {RuleId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling settlement automation rule: {RuleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to disable rule" });
        }
    }

    /// <summary>
    /// Test a settlement automation rule configuration
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="settlementId">The settlement to test with</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The test result</returns>
    [HttpPost("{id}/test")]
    [ProducesResponseType(typeof(RuleTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Test(
        [FromRoute] Guid id,
        [FromQuery] Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (settlementId == Guid.Empty)
                return BadRequest(new { error = "settlementId parameter is required" });

            _logger.LogInformation("Testing settlement automation rule: {RuleId} with settlement {SettlementId}", id, settlementId);

            var command = new TestSettlementAutomationRuleCommand
            {
                RuleId = id,
                SettlementId = settlementId
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Rule or settlement not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing settlement automation rule: {RuleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to test rule" });
        }
    }

    /// <summary>
    /// Execute a settlement automation rule
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="executedBy">The user executing the rule</param>
    /// <param name="settlementIds">Optional list of specific settlements to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The orchestration result</returns>
    [HttpPost("{id}/execute")]
    [ProducesResponseType(typeof(OrchestrationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Execute(
        [FromRoute] Guid id,
        [FromQuery] string executedBy,
        [FromQuery] List<Guid>? settlementIds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(executedBy))
                return BadRequest(new { error = "executedBy parameter is required" });

            _logger.LogInformation("Executing settlement automation rule: {RuleId}", id);

            var command = new ExecuteSettlementAutomationRuleCommand
            {
                RuleId = id,
                SettlementIds = settlementIds,
                ExecutedBy = executedBy
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Rule not found or not enabled: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing settlement automation rule: {RuleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to execute rule" });
        }
    }

    /// <summary>
    /// Get settlement automation rule execution history
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="pageNum">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of execution records</returns>
    [HttpGet("{id}/execution-history")]
    [ProducesResponseType(typeof(List<RuleExecutionRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetExecutionHistory(
        [FromRoute] Guid id,
        [FromQuery] int pageNum = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting execution history for rule: {RuleId}", id);

            var query = new GetRuleExecutionHistoryQuery
            {
                RuleId = id,
                PageNum = pageNum,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution history for rule: {RuleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve execution history" });
        }
    }

    /// <summary>
    /// Get settlement automation rule analytics
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rule analytics</returns>
    [HttpGet("{id}/analytics")]
    [ProducesResponseType(typeof(RuleAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAnalytics(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting analytics for rule: {RuleId}", id);

            var query = new GetRuleAnalyticsQuery { RuleId = id };

            var result = await _mediator.Send(query, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for rule: {RuleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve analytics" });
        }
    }

    /// <summary>
    /// Delete a settlement automation rule
    /// </summary>
    /// <param name="id">The rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting settlement automation rule: {RuleId}", id);

            // Use soft delete by disabling the rule
            var command = new DisableSettlementAutomationRuleCommand
            {
                RuleId = id,
                Reason = "Deleted by user",
                ModifiedBy = "System"
            };

            await _mediator.Send(command, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Rule not found: {RuleId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting settlement automation rule: {RuleId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to delete rule" });
        }
    }
}
