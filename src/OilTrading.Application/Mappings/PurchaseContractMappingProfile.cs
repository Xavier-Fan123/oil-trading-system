using AutoMapper;
using OilTrading.Core.Entities;
using OilTrading.Application.DTOs;
using OilTrading.Application.Commands.PurchaseContracts;

namespace OilTrading.Application.Mappings;

public class PurchaseContractMappingProfile : Profile
{
    public PurchaseContractMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<PurchaseContract, PurchaseContractDto>()
            .ForMember(dest => dest.ContractNumber, opt => opt.MapFrom(src => new ContractNumberDto { Value = src.ContractNumber.Value }))
            .ForMember(dest => dest.Supplier, opt => opt.MapFrom(src => new SupplierDto 
            { 
                Id = src.TradingPartnerId,
                Name = src.TradingPartner != null ? src.TradingPartner.CompanyName : "",
                Code = src.TradingPartner != null ? src.TradingPartner.CompanyCode : ""
            }))
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => new ContractProductDto 
            { 
                Id = src.ProductId,
                Name = src.Product != null ? src.Product.ProductName : "",
                Code = src.Product != null ? src.Product.ProductCode : ""
            }))
            .ForMember(dest => dest.TraderName, opt => opt.MapFrom(src => src.Trader != null ? $"{src.Trader.FirstName} {src.Trader.LastName}" : ""))
            .ForMember(dest => dest.TraderEmail, opt => opt.MapFrom(src => src.Trader != null ? src.Trader.Email : ""))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.ContractQuantity.Value))
            .ForMember(dest => dest.QuantityUnit, opt => opt.MapFrom(src => src.ContractQuantity.Unit))
            .ForMember(dest => dest.PricingFormula, opt => opt.MapFrom(src => src.PriceFormula != null ? src.PriceFormula.Formula : null))
            .ForMember(dest => dest.ContractValue, opt => opt.MapFrom(src => src.ContractValue != null ? src.ContractValue.Amount : (decimal?)null))
            .ForMember(dest => dest.ContractValueCurrency, opt => opt.MapFrom(src => src.ContractValue != null ? src.ContractValue.Currency : null))
            .ForMember(dest => dest.Premium, opt => opt.MapFrom(src => src.Premium != null ? src.Premium.Amount : (decimal?)null))
            .ForMember(dest => dest.Discount, opt => opt.MapFrom(src => src.Discount != null ? src.Discount.Amount : (decimal?)null))
            .ForMember(dest => dest.DeliveryTerms, opt => opt.MapFrom(src => src.DeliveryTerms))
            .ForMember(dest => dest.SettlementType, opt => opt.MapFrom(src => src.SettlementType))
            .ForMember(dest => dest.BenchmarkContractNumber, opt => opt.MapFrom(src => src.BenchmarkContract != null ? src.BenchmarkContract.ContractNumber.Value : null))
            .ForMember(dest => dest.LinkedSalesContracts, opt => opt.MapFrom(src => src.LinkedSalesContracts))
            .ForMember(dest => dest.ShippingOperations, opt => opt.MapFrom(src => src.ShippingOperations))
            .ForMember(dest => dest.PricingEvents, opt => opt.MapFrom(src => src.PricingEvents));

        CreateMap<PurchaseContract, PurchaseContractListDto>()
            .ForMember(dest => dest.ContractNumber, opt => opt.MapFrom(src => src.ContractNumber.Value))
            .ForMember(dest => dest.ContractType, opt => opt.MapFrom(src => src.ContractType))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.SupplierId, opt => opt.MapFrom(src => src.TradingPartnerId))
            .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.TradingPartner != null ? src.TradingPartner.CompanyName : ""))
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : ""))
            .ForMember(dest => dest.TraderName, opt => opt.MapFrom(src => src.Trader != null ? $"{src.Trader.FirstName} {src.Trader.LastName}" : ""))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.ContractQuantity.Value))
            .ForMember(dest => dest.QuantityUnit, opt => opt.MapFrom(src => src.ContractQuantity.Unit))
            .ForMember(dest => dest.ContractValue, opt => opt.MapFrom(src => src.ContractValue != null ? src.ContractValue.Amount : (decimal?)null))
            .ForMember(dest => dest.ContractValueCurrency, opt => opt.MapFrom(src => src.ContractValue != null ? src.ContractValue.Currency : null))
            .ForMember(dest => dest.ShippingOperationsCount, opt => opt.MapFrom(src => src.ShippingOperations != null ? src.ShippingOperations.Count : 0))
            .ForMember(dest => dest.LinkedSalesContractsCount, opt => opt.MapFrom(src => src.LinkedSalesContracts != null ? src.LinkedSalesContracts.Count : 0))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        // DTO to Command mappings
        CreateMap<CreatePurchaseContractDto, CreatePurchaseContractCommand>();
        CreateMap<UpdatePurchaseContractDto, UpdatePurchaseContractCommand>();

        // Sales Contract mappings
        CreateMap<SalesContract, SalesContractDto>()
            .ForMember(dest => dest.ContractNumber, opt => opt.MapFrom(src => new ContractNumberDto { Value = src.ContractNumber.Value }))
            .ForMember(dest => dest.Customer, opt => opt.MapFrom(src => new CustomerDto 
            { 
                Id = src.TradingPartnerId,
                Name = src.TradingPartner != null ? src.TradingPartner.CompanyName : "",
                Code = src.TradingPartner != null ? src.TradingPartner.CompanyCode : ""
            }))
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => new ContractProductDto 
            { 
                Id = src.ProductId,
                Name = src.Product != null ? src.Product.ProductName : "",
                Code = src.Product != null ? src.Product.ProductCode : ""
            }))
            .ForMember(dest => dest.TraderName, opt => opt.MapFrom(src => src.Trader != null ? $"{src.Trader.FirstName} {src.Trader.LastName}" : ""))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.ContractQuantity.Value))
            .ForMember(dest => dest.QuantityUnit, opt => opt.MapFrom(src => src.ContractQuantity.Unit))
            .ForMember(dest => dest.PricingFormula, opt => opt.MapFrom(src => src.PriceFormula != null ? src.PriceFormula.Formula : null))
            .ForMember(dest => dest.ContractValue, opt => opt.MapFrom(src => src.ContractValue != null ? src.ContractValue.Amount : (decimal?)null))
            .ForMember(dest => dest.ContractValueCurrency, opt => opt.MapFrom(src => src.ContractValue != null ? src.ContractValue.Currency : null))
            .ForMember(dest => dest.ProfitMargin, opt => opt.MapFrom(src => src.ProfitMargin != null ? src.ProfitMargin.Amount : (decimal?)null))
            .ForMember(dest => dest.Premium, opt => opt.MapFrom(src => src.Premium != null ? src.Premium.Amount : (decimal?)null))
            .ForMember(dest => dest.Discount, opt => opt.MapFrom(src => src.Discount != null ? src.Discount.Amount : (decimal?)null))
            .ForMember(dest => dest.DeliveryTerms, opt => opt.MapFrom(src => src.DeliveryTerms))
            .ForMember(dest => dest.SettlementType, opt => opt.MapFrom(src => src.SettlementType))
            .ForMember(dest => dest.LinkedPurchaseContractNumber, opt => opt.MapFrom(src => src.LinkedPurchaseContract != null ? src.LinkedPurchaseContract.ContractNumber.Value : null))
            .ForMember(dest => dest.ShippingOperations, opt => opt.MapFrom(src => src.ShippingOperations))
            .ForMember(dest => dest.PricingEvents, opt => opt.MapFrom(src => src.PricingEvents))
            // Business calculation fields - these would need business logic implementation
            .ForMember(dest => dest.EstimatedProfit, opt => opt.MapFrom(src => CalculateEstimatedProfit(src)))
            .ForMember(dest => dest.Margin, opt => opt.MapFrom(src => CalculateMargin(src)))
            .ForMember(dest => dest.RiskMetrics, opt => opt.MapFrom(src => CalculateRiskMetrics(src)));

        CreateMap<SalesContract, SalesContractSummaryDto>()
            .ForMember(dest => dest.ContractNumber, opt => opt.MapFrom(src => src.ContractNumber.Value))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.TradingPartnerId))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.TradingPartner != null ? src.TradingPartner.CompanyName : ""))
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : ""))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.ContractQuantity.Value))
            .ForMember(dest => dest.QuantityUnit, opt => opt.MapFrom(src => src.ContractQuantity.Unit))
            .ForMember(dest => dest.ContractValue, opt => opt.MapFrom(src => src.ContractValue != null ? src.ContractValue.Amount : (decimal?)null))
            .ForMember(dest => dest.EstimatedProfit, opt => opt.MapFrom(src => CalculateEstimatedProfit(src)))
            .ForMember(dest => dest.Margin, opt => opt.MapFrom(src => CalculateMargin(src)))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        // Shipping Operation mappings
        CreateMap<ShippingOperation, ShippingOperationSummaryDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.PlannedQuantity, opt => opt.MapFrom(src => src.PlannedQuantity.Value))
            .ForMember(dest => dest.PlannedQuantityUnit, opt => opt.MapFrom(src => src.PlannedQuantity.Unit.ToString()))
            .ForMember(dest => dest.ActualQuantity, opt => opt.MapFrom(src => src.ActualQuantity != null ? src.ActualQuantity.Value : (decimal?)null));

        // Pricing Event mappings
        CreateMap<PricingEvent, PricingEventSummaryDto>()
            .ForMember(dest => dest.EventType, opt => opt.MapFrom(src => src.EventType.ToString()));
    }

    // Business calculation methods for Sales Contracts
    private static decimal? CalculateEstimatedProfit(SalesContract contract)
    {
        // Example calculation - in a real system this would be more sophisticated
        if (contract.ContractValue?.Amount > 0 && contract.LinkedPurchaseContract?.ContractValue?.Amount > 0)
        {
            return contract.ContractValue.Amount - contract.LinkedPurchaseContract.ContractValue.Amount;
        }
        
        // Fallback to profit margin if available
        return contract.ProfitMargin?.Amount;
    }

    private static decimal? CalculateMargin(SalesContract contract)
    {
        // Margin percentage calculation
        if (contract.ContractValue?.Amount > 0 && contract.LinkedPurchaseContract?.ContractValue?.Amount > 0)
        {
            var profit = contract.ContractValue.Amount - contract.LinkedPurchaseContract.ContractValue.Amount;
            return (profit / contract.ContractValue.Amount) * 100;
        }
        
        // Fallback to existing profit margin
        if (contract.ProfitMargin?.Amount != null && contract.ContractValue?.Amount > 0)
        {
            return (contract.ProfitMargin.Amount / contract.ContractValue.Amount) * 100;
        }
        
        return null;
    }

    private static RiskMetricsDto? CalculateRiskMetrics(SalesContract contract)
    {
        // Example risk metrics calculation - in a real system this would involve VaR models
        if (contract.ContractValue?.Amount > 0)
        {
            var contractValue = contract.ContractValue.Amount;
            var var95 = contractValue * 0.05m; // 5% VaR assumption
            var exposure = contractValue * 0.03m; // 3% exposure assumption
            
            return new RiskMetricsDto
            {
                Var95 = var95,
                Exposure = exposure
            };
        }
        
        return null;
    }
}