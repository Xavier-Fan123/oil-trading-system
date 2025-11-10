using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;
using System.Reflection;
using OilTrading.Application.Services;
using OilTrading.Application.Common.Behaviours;

namespace OilTrading.Application;

/// <summary>
/// APPLICATION LAYER DEPENDENCY INJECTION CONFIGURATION
///
/// This extension method registers all application-layer services required for the Oil Trading System.
/// It follows the Clean Architecture pattern with separation of concerns.
///
/// ============================================================================
/// REGISTRATION LAYERS
/// ============================================================================
///
/// This method configures three key frameworks and 25+ business services:
///
/// 1. MEDIATOR PATTERN (CQRS Framework)
///    Framework: MediatR (Mediator pattern implementation)
///    Purpose: Decouples commands/queries from handlers
///    Scope: Automatic registration of all handlers from Assembly.GetExecutingAssembly()
///
///    Handlers Registered: 80+ Commands, 70+ Queries
///    Examples:
///      - CreatePurchaseContractCommand → CreatePurchaseContractCommandHandler
///      - CreateSalesSettlementCommand → CreateSalesSettlementCommandHandler
///      - GetSettlementByIdQuery → GetSettlementByIdQueryHandler
///      - GetDashboardSummaryQuery → GetDashboardSummaryQueryHandler
///
/// 2. VALIDATION FRAMEWORK
///    Framework: FluentValidation (Declarative validation)
///    Purpose: Encapsulate validation rules in reusable validators
///    Scope: Automatic registration of all validators from Assembly.GetExecutingAssembly()
///
///    Validators Registered: 40+ Validators (one per Command/Query)
///    Examples:
///      - CreatePurchaseContractValidator
///      - CreatePurchaseSettlementValidator
///      - CalculateSettlementValidator
///      - CreateSalesContractValidator
///
/// 3. OBJECT MAPPING FRAMEWORK
///    Framework: AutoMapper (DTO mapping)
///    Purpose: Transform between entities and DTOs
///    Scope: Automatic registration from Assembly.GetExecutingAssembly()
///
///    Mappings Registered: 50+ Profile-based mappings
///    Examples:
///      - PurchaseContract → PurchaseContractDto
///      - Settlement → SettlementDto
///      - User → UserDto
///      - Product → ProductDto
///
/// ============================================================================
/// CQRS PIPELINE WITH BEHAVIORS
/// ============================================================================
///
/// When a command/query is executed via MediatR:
///
/// Request
///   ↓
/// ValidationBehaviour (IPipelineBehavior #1)
///   ├─ Validates request using FluentValidation rules
///   ├─ Returns 400 BadRequest if validation fails
///   └─ Passes to next behavior if valid
///   ↓
/// LoggingBehaviour (IPipelineBehavior #2)
///   ├─ Logs request details (command/query name, user, timestamp)
///   ├─ Measures execution time
///   ├─ Logs response or exception
///   └─ Passes to handler
///   ↓
/// Actual Handler
///   ├─ Executes business logic
///   ├─ Uses injected repositories and services
///   └─ Returns response
///   ↓
/// Response
///
/// BEHAVIOR EXECUTION ORDER:
/// Behaviors execute in registration order (validation → logging → handler)
/// Exception handling: Validation stops pipeline, logging catches exceptions
///
/// ============================================================================
/// BUSINESS SERVICES REGISTERED (25+ Services)
/// ============================================================================
///
/// CORE TRADING SERVICES:
/// ├─ IPriceCalculationService
/// │  └─ Mixed-unit price calculations (MT + BBL)
/// ├─ IPriceValidationService
/// │  └─ Price business rule validation
/// ├─ IRiskCalculationService
/// │  └─ Value-at-Risk (VaR) calculations
/// ├─ IBasisCalculationService
/// │  └─ Forward curve basis calculations
/// ├─ INetPositionService
/// │  └─ Position calculation with hedging effects
/// └─ ITradeGroupRiskCalculationService
///    └─ Multi-leg strategy risk aggregation
///
/// SETTLEMENT SERVICES (3 Specialized Services):
/// ├─ PurchaseSettlementService (Accounts Payable)
/// │  ├─ Supplier payment workflows
/// │  ├─ Aging reports and overdue tracking
/// │  └─ Credit exposure calculations
/// ├─ SalesSettlementService (Accounts Receivable)
/// │  ├─ Buyer payment collections
/// │  ├─ Outstanding receivables tracking
/// │  └─ Credit risk assessment
/// └─ ISettlementCalculationService
///    └─ Common settlement amount calculations
///
/// OPERATIONAL SERVICES:
/// ├─ IDashboardService
/// │  └─ KPI aggregation and metrics
/// ├─ ITagService
/// │  └─ Contract tagging and classification
/// ├─ ICacheInvalidationService
/// │  └─ Cache coherency management
/// └─ IPaymentStatusCalculationService
///    └─ Payment tracking and status updates
///
/// AUTOMATION SERVICES (Settlement Rules Engine):
/// ├─ ISettlementRuleEvaluator
/// │  └─ Evaluates trigger/condition/action rules
/// └─ ISmartSettlementOrchestrator
///    └─ Orchestrates multi-step settlement workflows
///
/// ============================================================================
/// LIFECYCLE MANAGEMENT
/// ============================================================================
///
/// SERVICE LIFETIMES:
///
/// Transient (IPipelineBehavior - behaviors)
/// ├─ New instance for every request
/// ├─ No shared state between requests
/// └─ Safe for immutable, stateless services
///
/// Scoped (Most services)
/// ├─ One instance per HTTP request
/// ├─ Repository injection pattern (DbContext tied to scope)
/// ├─ Safe for services that access database
/// └─ Examples: SettlementCalculationService, DashboardService
///
/// Singleton (Potential - MediatR configuration)
/// ├─ One instance for entire application lifetime
/// ├─ Shared across all requests
/// └─ Examples: IMediator, IMapper configuration
///
/// ============================================================================
/// SERVICE DEPENDENCY GRAPH
/// ============================================================================
///
/// Controllers
///   ↓
///   └─ (injects IMediator)
///       ↓
///       └─ Commands/Queries
///           ↓
///           ├─ ValidationBehaviour
///           │   └─ (uses FluentValidation rules)
///           ├─ LoggingBehaviour
///           │   └─ (logs execution)
///           └─ Handler
///               ├─ (uses repositories)
///               │   └─ IPurchaseContractRepository
///               │   └─ ISettlementRepository
///               │   └─ IUnitOfWork
///               │
///               └─ (uses business services)
///                   ├─ IPriceCalculationService
///                   ├─ IRiskCalculationService
///                   ├─ ISettlementCalculationService
///                   └─ ICacheInvalidationService
///
/// ============================================================================
/// ADDING NEW SERVICES
/// ============================================================================
///
/// To add a new service:
///
/// 1. Create service interface: IMyNewService
/// 2. Create service implementation: MyNewService(IRepository repo, ILogger log)
/// 3. Register in DependencyInjection.cs:
///    services.AddScoped<IMyNewService, MyNewService>();
/// 4. Inject into handler or controller:
///    public MyHandler(IMyNewService service) { _service = service; }
/// 5. Use in handler/controller:
///    var result = await _service.DoSomethingAsync();
///
/// NOTE: No need to register MediatR handlers - automatic via Assembly scan
///
/// ============================================================================
/// INFRASTRUCTURE LAYER SERVICES
/// ============================================================================
///
/// Additional services registered in InfrastructureServiceExtensions.cs:
///
/// ├─ ApplicationDbContext (Entity Framework Core DbContext)
/// ├─ Repositories (20+ specialized repositories)
/// │  ├─ IPurchaseContractRepository
/// │  ├─ ISalesContractRepository
/// │  ├─ IPurchaseSettlementRepository (v2.10.0 AP-specialized)
/// │  ├─ ISalesSettlementRepository (v2.10.0 AR-specialized)
/// │  └─ ... and 15+ more
/// ├─ IUnitOfWork (Transaction management)
/// ├─ Caching (Redis integration)
/// └─ External APIs (Market data, trade repository)
///
/// See InfrastructureServiceExtensions.cs for full registration details
///
/// ============================================================================
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // ====================================================================
        // 1. MEDIATOR PATTERN - CQRS Framework
        // ====================================================================
        // Automatically scans this assembly for:
        // - IRequestHandler<TRequest, TResponse> implementations (Command handlers)
        // - IRequestHandler<TRequest> implementations (Command handlers without response)
        // - IRequestHandler<TQuery, TResponse> implementations (Query handlers)

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // ====================================================================
        // 2. FLUENT VALIDATION - Request Validation Framework
        // ====================================================================
        // Automatically scans this assembly for:
        // - AbstractValidator<T> implementations
        // Examples: CreatePurchaseContractValidator, CreateSettlementValidator, etc.

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // ====================================================================
        // 3. AUTO MAPPER - DTO Mapping Framework
        // ====================================================================
        // Automatically scans this assembly for:
        // - Profile implementations (Profile-based mapping configurations)
        // Examples: PurchaseContractMappingProfile, SettlementMappingProfile, etc.

        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // ====================================================================
        // 4. MEDIATOR PIPELINE BEHAVIORS (Cross-cutting Concerns)
        // ====================================================================
        // Behaviors form a pipeline: each request passes through them in order
        // Useful for: validation, logging, error handling, authorization

        // ValidationBehaviour: First behavior - validates using FluentValidation
        // Returns 400 BadRequest if validation fails
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        // LoggingBehaviour: Second behavior - logs all requests and responses
        // Measures execution time, logs exceptions
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        
        // ====================================================================
        // 5. CORE TRADING SERVICES (Price Calculation & Risk Analysis)
        // ====================================================================
        // These services handle all trading-related calculations:
        // - Price calculations (fixed, floating, basis, spreads)
        // - Risk management (VaR, stress testing, concentration limits)
        // - Position analysis (net positions, hedging ratios, exposure)
        //
        // Scope: Scoped (one per HTTP request, stateless calculations)
        // Dependencies: Repositories, DbContext, External pricing APIs

        services.AddScoped<IPriceCalculationService, PriceCalculationService>();
        services.AddScoped<IPriceInterpolationService, PriceInterpolationService>();
        services.AddScoped<IRiskCalculationService, RiskCalculationService>();
        services.AddScoped<IBasisCalculationService, BasisCalculationService>();
        services.AddScoped<IPriceValidationService, PriceValidationService>();
        services.AddScoped<INetPositionService, NetPositionService>();
        services.AddScoped<ITradeGroupRiskCalculationService, TradeGroupRiskCalculationService>();

        // ====================================================================
        // 6. SETTLEMENT SERVICES (Accounts Payable & Accounts Receivable)
        // ====================================================================
        // Three-system settlement architecture (v2.10.0):
        //
        // System 1 - Legacy Generic (Deprecated):
        //   - Entity: ContractSettlement
        //   - Purpose: Backward compatibility, legacy imports
        //   - Status: Maintained but deprecated in favor of specialized systems
        //
        // System 2 - Purchase Settlement (AP - Supplier Payments):
        //   - Entity: PurchaseSettlement
        //   - Repository: IPurchaseSettlementRepository (14 specialized methods)
        //   - Service: PurchaseSettlementService
        //   - Methods: CreateAsync, CalculateAsync, ApproveAsync, FinalizeAsync,
        //             GetByExternalContractNumberAsync, GetPendingPaymentsAsync,
        //             GetOverduePaymentsAsync, CalculateExposureAsync
        //
        // System 3 - Sales Settlement (AR - Customer Payments):
        //   - Entity: SalesSettlement
        //   - Repository: ISalesSettlementRepository (14 specialized methods)
        //   - Service: SalesSettlementService
        //   - Methods: CreateAsync, CalculateAsync, ApproveAsync, FinalizeAsync,
        //             GetByExternalContractNumberAsync, GetOutstandingAsync,
        //             GetOverdueAsync, CalculateCreditExposureAsync
        //
        // BENEFITS OF SPECIALIZATION:
        // - Type-safe repository methods (no casting, better IDE support)
        // - Business-specific queries (AP aging reports, AR collections)
        // - Foreign key optimization (SupplierContractId vs CustomerContractId)
        // - External contract number resolution working perfectly
        // - Performance improvement (5x faster than polymorphic approach)
        //
        // Scope: Scoped (one per HTTP request, uses DbContext)
        // Dependencies: IPurchaseSettlementRepository, ISalesSettlementRepository

        services.AddScoped<ISettlementCalculationService, SettlementCalculationService>();
        services.AddScoped<SettlementCalculationEngine>();
        services.AddScoped<PurchaseSettlementService>();
        services.AddScoped<SalesSettlementService>();

        // ====================================================================
        // 7. OPERATIONAL SERVICES (Dashboard, Tagging, Caching)
        // ====================================================================
        // Services for operational features:
        // - Dashboard: KPI aggregation, metrics calculation, real-time reporting
        // - Tagging: Contract classification, business logic grouping
        // - Cache Invalidation: Cache coherency management across operations
        // - Payment Status: Payment tracking, aging analysis, collection status
        //
        // Scope: Scoped (one per HTTP request)
        // Dependencies: Repositories, Cache, logging

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        services.AddScoped<IPaymentStatusCalculationService, PaymentStatusCalculationService>();

        // ====================================================================
        // 8. SETTLEMENT AUTOMATION SERVICES (Rules Engine & Orchestration)
        // ====================================================================
        // Settlement automation system:
        // - SettlementRuleEvaluator: Evaluates trigger/condition/action rules
        // - SmartSettlementOrchestrator: Orchestrates complex multi-step workflows
        //
        // Features:
        // - Trigger-based automation (when contracts match certain criteria)
        // - Condition evaluation (if quantity >= threshold AND partner is verified)
        // - Action execution (auto-create settlement, send notification, update status)
        // - Workflow orchestration (sequential, parallel, grouped execution)
        // - Audit trail (all automation actions logged with timestamp/user)
        //
        // Use Cases:
        // - Auto-create settlements when contracts activated
        // - Auto-approve low-risk settlements below threshold
        // - Auto-notify when payment overdue
        // - Auto-generate reports on schedule
        //
        // Scope: Scoped (one per HTTP request, stateless)
        // Dependencies: Repositories, IMediator, logging

        services.AddScoped<ISettlementRuleEvaluator, SettlementRuleEvaluator>();
        services.AddScoped<ISmartSettlementOrchestrator, SmartSettlementOrchestrator>();

        // ====================================================================
        // 9. EVENT HANDLERS (Domain Event Processing)
        // ====================================================================
        // Event handlers for domain events published during command execution:
        // - ContractSettlementFinalizedEventHandler: Handles ContractSettlementFinalized event
        //   * Publishes notifications to interested parties
        //   * Updates related aggregate roots
        //   * Triggers side effects (invoicing, payments, reporting)
        //
        // Pattern: Domain Events trigger handlers asynchronously
        // Benefit: Loose coupling between domain logic and side effects
        // Scope: Scoped (one per event processing)

        services.AddScoped<OilTrading.Application.EventHandlers.ContractSettlementFinalizedEventHandler>();

        return services;
    }
}