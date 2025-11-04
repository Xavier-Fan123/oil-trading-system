using Microsoft.EntityFrameworkCore;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;

namespace OilTrading.Application.Services;

public class ContractExecutionReportService : IContractExecutionReportService
{
    private readonly IContractExecutionReportRepository _reportRepository;
    private readonly IPurchaseContractRepository _purchaseContractRepository;
    private readonly ISalesContractRepository _salesContractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ContractExecutionReportService(
        IContractExecutionReportRepository reportRepository,
        IPurchaseContractRepository purchaseContractRepository,
        ISalesContractRepository salesContractRepository,
        IUnitOfWork unitOfWork)
    {
        _reportRepository = reportRepository;
        _purchaseContractRepository = purchaseContractRepository;
        _salesContractRepository = salesContractRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ContractExecutionReportDto?> GeneratePurchaseContractExecutionReportAsync(
        PurchaseContract contract,
        CancellationToken cancellationToken = default)
    {
        // Get purchase settlements for this contract
        var purchaseSettlements = contract.PurchaseSettlements ?? new List<PurchaseSettlement>();
        var totalSettledAmount = purchaseSettlements.Sum(s => s.TotalSettlementAmount);
        var paidSettledAmount = purchaseSettlements.Count(s => s.Status == Core.Entities.ContractSettlementStatus.Finalized) > 0
            ? totalSettledAmount : 0;

        // Get shipping operations
        var shippingOps = contract.ShippingOperations ?? new List<ShippingOperation>();

        // Calculate execution percentage
        var executedQuantity = shippingOps.Sum(op => op.ActualQuantity?.Value ?? 0);
        var executionPercentage = contract.ContractQuantity.Value > 0
            ? (executedQuantity / contract.ContractQuantity.Value) * 100
            : 0;

        // Determine execution status
        var executionStatus = DetermineExecutionStatus(
            contract.Status.ToString(),
            executionPercentage,
            contract.EstimatedPaymentDate);

        // Get pricing information
        var finalPrice = contract.ContractValue?.Amount;
        var benchmarkPrice = contract.PriceFormula?.BasePrice;

        // Create report
        var report = new ContractExecutionReport(
            contract.Id,
            contract.ContractNumber.Value,
            "Purchase",
            DateTime.UtcNow);

        report.SetBasicInfo(
            contract.TradingPartnerId,
            contract.TradingPartner?.CompanyName ?? string.Empty,
            contract.ProductId,
            contract.Product?.ProductName ?? string.Empty,
            contract.ContractQuantity.Value,
            contract.ContractQuantity.Unit.ToString(),
            contract.Status.ToString(),
            contract.CreatedAt,
            contract.UpdatedAt,
            contract.LaycanStart,
            contract.LaycanEnd,
            contract.ContractValue?.Amount,
            contract.ContractValue?.Currency,
            null, // LoadPort
            null, // DischargePort
            contract.DeliveryTerms.ToString());

        report.UpdateExecutionMetrics(
            executedQuantity,
            DeterminePaymentStatus(totalSettledAmount, paidSettledAmount),
            shippingOps.Count,
            totalSettledAmount,
            paidSettledAmount);

        report.UpdateSettlementInfo(
            purchaseSettlements.Count,
            totalSettledAmount,
            paidSettledAmount);

        report.UpdatePricingInfo(
            benchmarkPrice,
            null, // adjustmentPrice
            finalPrice,
            contract.IsPriceFinalized);

        report.UpdateDeliveryInfo(
            shippingOps.OrderByDescending(op => op.CertificateOfDischargeDate).FirstOrDefault()?.CertificateOfDischargeDate,
            executionStatus,
            executionStatus != "Delayed");

        // Save report
        await _reportRepository.AddAsync(report);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(report);
    }

    public async Task<ContractExecutionReportDto?> GenerateSalesContractExecutionReportAsync(
        SalesContract contract,
        CancellationToken cancellationToken = default)
    {
        // Get sales settlements for this contract
        var salesSettlements = contract.SalesSettlements ?? new List<SalesSettlement>();
        var totalSettledAmount = salesSettlements.Sum(s => s.TotalSettlementAmount);
        var paidSettledAmount = salesSettlements.Count(s => s.Status == Core.Entities.ContractSettlementStatus.Finalized) > 0
            ? totalSettledAmount : 0;

        // Get shipping operations
        var shippingOps = contract.ShippingOperations ?? new List<ShippingOperation>();

        // Calculate execution percentage
        var executedQuantity = shippingOps.Sum(op => op.ActualQuantity?.Value ?? 0);
        var executionPercentage = contract.ContractQuantity.Value > 0
            ? (executedQuantity / contract.ContractQuantity.Value) * 100
            : 0;

        // Determine execution status
        var executionStatus = DetermineExecutionStatus(
            contract.Status.ToString(),
            executionPercentage,
            contract.EstimatedPaymentDate);

        // Get pricing information
        var finalPrice = contract.ContractValue?.Amount;
        var benchmarkPrice = contract.PriceFormula?.BasePrice;

        // Create report
        var report = new ContractExecutionReport(
            contract.Id,
            contract.ContractNumber.Value,
            "Sales",
            DateTime.UtcNow);

        report.SetBasicInfo(
            contract.TradingPartnerId,
            contract.TradingPartner?.CompanyName ?? string.Empty,
            contract.ProductId,
            contract.Product?.ProductName ?? string.Empty,
            contract.ContractQuantity.Value,
            contract.ContractQuantity.Unit.ToString(),
            contract.Status.ToString(),
            contract.CreatedAt,
            contract.UpdatedAt,
            contract.LaycanStart,
            contract.LaycanEnd,
            contract.ContractValue?.Amount,
            contract.ContractValue?.Currency,
            null, // LoadPort
            null, // DischargePort
            contract.DeliveryTerms.ToString());

        report.UpdateExecutionMetrics(
            executedQuantity,
            DeterminePaymentStatus(totalSettledAmount, paidSettledAmount),
            shippingOps.Count,
            totalSettledAmount,
            paidSettledAmount);

        report.UpdateSettlementInfo(
            salesSettlements.Count,
            totalSettledAmount,
            paidSettledAmount);

        report.UpdatePricingInfo(
            benchmarkPrice,
            null, // adjustmentPrice
            finalPrice,
            contract.IsPriceFinalized);

        report.UpdateDeliveryInfo(
            shippingOps.OrderByDescending(op => op.CertificateOfDischargeDate).FirstOrDefault()?.CertificateOfDischargeDate,
            executionStatus,
            executionStatus != "Delayed");

        // Save report
        await _reportRepository.AddAsync(report);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(report);
    }

    public async Task GenerateAllExecutionReportsAsync(CancellationToken cancellationToken = default)
    {
        var purchaseContracts = (await _purchaseContractRepository.GetAllAsync()).ToList();
        var salesContracts = (await _salesContractRepository.GetAllAsync()).ToList();

        foreach (var contract in purchaseContracts)
        {
            await GeneratePurchaseContractExecutionReportAsync(contract, cancellationToken);
        }

        foreach (var contract in salesContracts)
        {
            await GenerateSalesContractExecutionReportAsync(contract, cancellationToken);
        }
    }

    public async Task<ContractExecutionReportDto?> GenerateExecutionReportByContractIdAsync(
        Guid contractId,
        bool isPurchaseContract,
        CancellationToken cancellationToken = default)
    {
        if (isPurchaseContract)
        {
            var contract = await _purchaseContractRepository.GetByIdAsync(contractId);
            if (contract == null) return null;
            return await GeneratePurchaseContractExecutionReportAsync(contract, cancellationToken);
        }
        else
        {
            var contract = await _salesContractRepository.GetByIdAsync(contractId);
            if (contract == null) return null;
            return await GenerateSalesContractExecutionReportAsync(contract, cancellationToken);
        }
    }

    private string DetermineExecutionStatus(string contractStatus, decimal executionPercentage, DateTime? estimatedPaymentDate)
    {
        if (contractStatus == "Completed" || executionPercentage >= 100)
        {
            return "Completed";
        }

        if (contractStatus == "Cancelled")
        {
            return "Cancelled";
        }

        if (estimatedPaymentDate.HasValue && estimatedPaymentDate.Value < DateTime.UtcNow && executionPercentage < 100)
        {
            return "Delayed";
        }

        return "OnTrack";
    }

    private string DeterminePaymentStatus(decimal totalSettledAmount, decimal paidSettledAmount)
    {
        if (totalSettledAmount == 0)
            return "NotDue";

        if (paidSettledAmount >= totalSettledAmount)
            return "Paid";

        if (paidSettledAmount > 0)
            return "PartiallyPaid";

        return "NotPaid";
    }

    private ContractExecutionReportDto MapToDto(ContractExecutionReport report)
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
