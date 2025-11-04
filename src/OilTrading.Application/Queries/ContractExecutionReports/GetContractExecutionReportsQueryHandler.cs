using MediatR;
using Microsoft.EntityFrameworkCore;
using OilTrading.Application.DTOs;
using OilTrading.Core.Common;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Queries.ContractExecutionReports;

public class GetContractExecutionReportsQueryHandler : IRequestHandler<GetContractExecutionReportsQuery, PagedResult<ContractExecutionReportDto>>
{
    private readonly IContractExecutionReportRepository _reportRepository;

    public GetContractExecutionReportsQueryHandler(IContractExecutionReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<PagedResult<ContractExecutionReportDto>> Handle(
        GetContractExecutionReportsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _reportRepository.GetAllAsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.ContractType))
        {
            query = query.Where(r => r.ContractType == request.ContractType);
        }

        if (!string.IsNullOrEmpty(request.ExecutionStatus))
        {
            query = query.Where(r => r.ExecutionStatus == request.ExecutionStatus);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(r => r.ReportGeneratedDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(r => r.ReportGeneratedDate <= request.ToDate.Value);
        }

        if (request.TradingPartnerId.HasValue)
        {
            query = query.Where(r => r.TradingPartnerId == request.TradingPartnerId.Value);
        }

        if (request.ProductId.HasValue)
        {
            query = query.Where(r => r.ProductId == request.ProductId.Value);
        }

        // Apply sorting
        query = ApplyOrdering(query, request.SortBy, request.SortDescending);

        // Get total count for pagination
        var totalCount = query.Count();

        // Apply pagination
        var reports = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var reportDtos = reports.Select(MapToDto).ToList();

        return new PagedResult<ContractExecutionReportDto>
        {
            Items = reportDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private static IQueryable<Core.Entities.ContractExecutionReport> ApplyOrdering(
        IQueryable<Core.Entities.ContractExecutionReport> query,
        string? sortBy,
        bool descending)
    {
        var orderByClause = (sortBy?.ToLower()) switch
        {
            "contractnumber" => query.OrderBy(r => r.ContractNumber),
            "contracttype" => query.OrderBy(r => r.ContractType),
            "tradingpartnername" => query.OrderBy(r => r.TradingPartnerName),
            "productname" => query.OrderBy(r => r.ProductName),
            "executionstatus" => query.OrderBy(r => r.ExecutionStatus),
            "executionpercentage" => query.OrderBy(r => r.ExecutionPercentage),
            "paymentstatus" => query.OrderBy(r => r.PaymentStatus),
            "createddate" => query.OrderBy(r => r.CreatedDate),
            "completiondate" => query.OrderBy(r => r.CompletionDate),
            _ => query.OrderBy(r => r.ReportGeneratedDate)
        };

        return descending ? ((IOrderedQueryable<Core.Entities.ContractExecutionReport>)orderByClause).Reverse() : orderByClause;
    }

    private static ContractExecutionReportDto MapToDto(Core.Entities.ContractExecutionReport report)
    {
        return new ContractExecutionReportDto
        {
            Id = report.Id,
            ContractId = report.ContractId,
            ContractNumber = report.ContractNumber,
            ContractType = report.ContractType,
            ReportGeneratedDate = report.ReportGeneratedDate,
            TradingPartnerId = report.TradingPartnerId,
            TradingPartnerName = report.TradingPartnerName,
            ProductId = report.ProductId,
            ProductName = report.ProductName,
            Quantity = report.Quantity,
            QuantityUnit = report.QuantityUnit,
            ContractStatus = report.ContractStatus,
            ContractValue = report.ContractValue,
            Currency = report.Currency,
            ExecutedQuantity = report.ExecutedQuantity,
            ExecutionPercentage = report.ExecutionPercentage,
            CreatedDate = report.CreatedDate,
            ActivatedDate = report.ActivatedDate,
            LaycanStart = report.LaycanStart,
            LaycanEnd = report.LaycanEnd,
            EstimatedDeliveryDate = report.EstimatedDeliveryDate,
            ActualDeliveryDate = report.ActualDeliveryDate,
            SettlementDate = report.SettlementDate,
            CompletionDate = report.CompletionDate,
            SettlementCount = report.SettlementCount,
            TotalSettledAmount = report.TotalSettledAmount,
            PaidSettledAmount = report.PaidSettledAmount,
            UnpaidSettledAmount = report.UnpaidSettledAmount,
            PaymentStatus = report.PaymentStatus,
            ShippingOperationCount = report.ShippingOperationCount,
            LoadPort = report.LoadPort,
            DischargePort = report.DischargePort,
            DeliveryTerms = report.DeliveryTerms,
            DaysToActivation = report.DaysToActivation,
            DaysToCompletion = report.DaysToCompletion,
            IsOnSchedule = report.IsOnSchedule,
            ExecutionStatus = report.ExecutionStatus,
            BenchmarkPrice = report.BenchmarkPrice,
            AdjustmentPrice = report.AdjustmentPrice,
            FinalPrice = report.FinalPrice,
            IsPriceFinalized = report.IsPriceFinalized,
            HasRiskViolations = report.HasRiskViolations,
            IsCompliant = report.IsCompliant,
            Notes = report.Notes,
            LastUpdatedDate = report.LastUpdatedDate
        };
    }
}
