using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;
using System.Reflection;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Behaviours;

namespace OilTrading.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Register FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Register AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        // Register MediatR behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        
        // Register application services
        services.AddScoped<IPriceCalculationService, PriceCalculationService>();
        services.AddScoped<IPriceInterpolationService, PriceInterpolationService>();
        services.AddScoped<IRiskCalculationService, RiskCalculationService>();
        services.AddScoped<IBasisCalculationService, BasisCalculationService>();
        services.AddScoped<IPriceValidationService, PriceValidationService>();
        services.AddScoped<INetPositionService, NetPositionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        services.AddScoped<ITradeGroupRiskCalculationService, TradeGroupRiskCalculationService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ISettlementCalculationService, SettlementCalculationService>();
        services.AddScoped<SettlementCalculationEngine>();
        services.AddScoped<PurchaseSettlementService>();
        services.AddScoped<SalesSettlementService>();

        return services;
    }
}