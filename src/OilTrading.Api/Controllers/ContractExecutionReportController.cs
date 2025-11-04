using MediatR;
using Microsoft.AspNetCore.Mvc;
using OilTrading.Application.DTOs;
using OilTrading.Application.Queries.ContractExecutionReports;

namespace OilTrading.Api.Controllers;

[ApiController]
[Route("api/contract-execution-reports")]
public class ContractExecutionReportController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContractExecutionReportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get contract execution report by contract ID
    /// </summary>
    [HttpGet("{contractId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ContractExecutionReportDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContractExecutionReport(
        Guid contractId,
        [FromQuery] bool isPurchaseContract = true,
        CancellationToken cancellationToken = default)
    {
        var query = new GetContractExecutionReportQuery
        {
            ContractId = contractId,
            IsPurchaseContract = isPurchaseContract
        };

        var report = await _mediator.Send(query, cancellationToken);

        if (report == null)
        {
            return NotFound(new { message = $"Contract execution report not found for contract {contractId}" });
        }

        return Ok(report);
    }

    /// <summary>
    /// Get paginated list of contract execution reports
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<ContractExecutionReportDto>))]
    public async Task<IActionResult> GetContractExecutionReports(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? contractType = null,
        [FromQuery] string? executionStatus = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? tradingPartnerId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] string? sortBy = "ReportGeneratedDate",
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = new GetContractExecutionReportsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ContractType = contractType,
            ExecutionStatus = executionStatus,
            FromDate = fromDate,
            ToDate = toDate,
            TradingPartnerId = tradingPartnerId,
            ProductId = productId,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get execution reports for a trading partner
    /// </summary>
    [HttpGet("trading-partner/{tradingPartnerId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<ContractExecutionReportDto>))]
    public async Task<IActionResult> GetTradingPartnerExecutionReports(
        Guid tradingPartnerId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetContractExecutionReportsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TradingPartnerId = tradingPartnerId,
            SortBy = "ReportGeneratedDate",
            SortDescending = true
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get execution reports for a product
    /// </summary>
    [HttpGet("product/{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<ContractExecutionReportDto>))]
    public async Task<IActionResult> GetProductExecutionReports(
        Guid productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetContractExecutionReportsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ProductId = productId,
            SortBy = "ReportGeneratedDate",
            SortDescending = true
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get execution reports filtered by execution status
    /// </summary>
    [HttpGet("status/{executionStatus}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<ContractExecutionReportDto>))]
    public async Task<IActionResult> GetExecutionReportsByStatus(
        string executionStatus,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetContractExecutionReportsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ExecutionStatus = executionStatus,
            SortBy = "ReportGeneratedDate",
            SortDescending = true
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get execution reports for a date range
    /// </summary>
    [HttpGet("date-range")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<ContractExecutionReportDto>))]
    public async Task<IActionResult> GetExecutionReportsByDateRange(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!fromDate.HasValue || !toDate.HasValue)
        {
            return BadRequest(new { message = "Both fromDate and toDate are required" });
        }

        if (fromDate > toDate)
        {
            return BadRequest(new { message = "fromDate must be before toDate" });
        }

        var query = new GetContractExecutionReportsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            FromDate = fromDate,
            ToDate = toDate,
            SortBy = "ReportGeneratedDate",
            SortDescending = true
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
