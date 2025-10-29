using Microsoft.AspNetCore.Mvc;
using MediatR;
using OilTrading.Application.Queries.Contracts;
using OilTrading.Application.DTOs;

namespace OilTrading.Api.Controllers;

/// <summary>
/// Controller for contract resolution operations
/// Resolves contracts from external contract numbers to internal GUIDs
/// </summary>
[ApiController]
[Route("api/contracts")]
[Produces("application/json")]
public class ContractResolutionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ContractResolutionController> _logger;

    public ContractResolutionController(
        IMediator mediator,
        ILogger<ContractResolutionController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Resolves a contract GUID from its external contract number
    /// </summary>
    /// <remarks>
    /// This endpoint attempts to find a contract by its external contract number.
    /// If a single contract is found, it returns success with the contract ID.
    /// If multiple contracts match, it returns the candidates for user selection (disambiguation).
    /// If no contracts match, it returns an error.
    /// </remarks>
    /// <param name="externalContractNumber">The external contract number to search for</param>
    /// <param name="contractType">Optional: Contract type filter (Purchase or Sales)</param>
    /// <param name="tradingPartnerId">Optional: Trading partner ID filter for disambiguation</param>
    /// <param name="productId">Optional: Product ID filter for disambiguation</param>
    /// <returns>Contract resolution result with contract ID or candidates</returns>
    [HttpGet("resolve")]
    [ProducesResponseType(typeof(ContractResolutionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveByExternalNumber(
        [FromQuery(Name = "externalContractNumber")] string externalContractNumber,
        [FromQuery(Name = "contractType")] string? contractType = null,
        [FromQuery(Name = "tradingPartnerId")] Guid? tradingPartnerId = null,
        [FromQuery(Name = "productId")] Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalContractNumber))
        {
            _logger.LogWarning("Contract resolution attempted with empty external contract number");
            return BadRequest(new ContractResolutionResultDto
            {
                Success = false,
                ErrorMessage = "External contract number is required"
            });
        }

        try
        {
            var query = new ResolveContractByExternalNumberQuery
            {
                ExternalContractNumber = externalContractNumber,
                ExpectedContractType = contractType,
                ExpectedTradingPartnerId = tradingPartnerId,
                ExpectedProductId = productId
            };

            var result = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation(
                "Contract resolution for external number '{ExternalNumber}': Success={Success}, CandidateCount={Count}",
                externalContractNumber, result.Success, result.Candidates.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving contract by external number: {ExternalNumber}", externalContractNumber);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ContractResolutionResultDto
                {
                    Success = false,
                    ErrorMessage = "An error occurred while resolving the contract"
                });
        }
    }

    /// <summary>
    /// Searches for contracts by external contract number (partial match)
    /// </summary>
    /// <remarks>
    /// This is a convenience endpoint that returns all contracts matching the external number
    /// (supports partial matching). Use /resolve for exact resolution with disambiguation.
    /// </remarks>
    /// <param name="externalContractNumber">The external contract number to search for (partial match)</param>
    /// <param name="pageNumber">Page number for pagination (1-based)</param>
    /// <param name="pageSize">Number of results per page</param>
    /// <returns>List of matching contracts</returns>
    [HttpGet("search-by-external")]
    [ProducesResponseType(typeof(List<ContractCandidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchByExternalNumber(
        [FromQuery(Name = "externalContractNumber")] string externalContractNumber,
        [FromQuery(Name = "pageNumber")] int pageNumber = 1,
        [FromQuery(Name = "pageSize")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalContractNumber))
        {
            return BadRequest("External contract number is required");
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        try
        {
            var query = new ResolveContractByExternalNumberQuery
            {
                ExternalContractNumber = externalContractNumber
            };

            var result = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation(
                "Contract search for external number '{ExternalNumber}' returned {Count} candidates",
                externalContractNumber, result.Candidates.Count);

            // For search endpoint, return all candidates (even if marked as not successful for single match)
            return Ok(result.Candidates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching contracts by external number: {ExternalNumber}", externalContractNumber);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                "An error occurred while searching for contracts");
        }
    }
}
