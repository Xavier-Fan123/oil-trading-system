using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using OilTrading.Application.Services;
using OilTrading.Core.Repositories;
using OilTrading.Core.Services;
using OilTrading.Infrastructure.Data;
using OilTrading.Infrastructure.Repositories;
using OilTrading.Infrastructure.Services;
using System;
using Npgsql;
using StackExchange.Redis;

namespace OilTrading.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        // Configure database based on connection string and environment
        ConfigureDatabase(services, configuration, connectionString, environment);
        
        // Configure caching
        ConfigureCaching(services, configuration);
        
        // Configure repositories and services
        ConfigureRepositories(services);
        ConfigureApplicationServices(services, configuration);
        
        return services;
    }
    
    private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration, string connectionString, string environment)
    {
        var databaseConfig = configuration.GetSection("Database");
        
        if (connectionString == "InMemory" || environment == "Testing")
        {
            // In-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("OilTradingDb")
                       .EnableSensitiveDataLogging()
                       .EnableDetailedErrors());
            services.AddDbContext<ApplicationReadDbContext>(options =>
                options.UseInMemoryDatabase("OilTradingDb"));
        }
        else if (connectionString.Contains("Data Source=") || connectionString.Contains(".db"))
        {
            // SQLite connection (fallback for development)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString)
                       .EnableSensitiveDataLogging(environment == "Development")
                       .EnableDetailedErrors(environment == "Development"));
            services.AddDbContext<ApplicationReadDbContext>(options =>
                options.UseSqlite(connectionString));
        }
        else
        {
            // PostgreSQL - Production configuration
            ConfigurePostgreSQL(services, configuration, connectionString, environment, databaseConfig);
        }
    }
    
    private static void ConfigurePostgreSQL(IServiceCollection services, IConfiguration configuration, 
        string connectionString, string environment, IConfigurationSection databaseConfig)
    {
        var enableRetryOnFailure = databaseConfig.GetValue<bool>("EnableRetryOnFailure", true);
        var maxRetryCount = databaseConfig.GetValue<int>("MaxRetryCount", 3);
        var maxRetryDelay = databaseConfig.GetValue<TimeSpan>("MaxRetryDelay", TimeSpan.FromSeconds(30));
        var commandTimeout = databaseConfig.GetValue<int>("CommandTimeout", 60);
        var enableSensitiveDataLogging = databaseConfig.GetValue<bool>("EnableSensitiveDataLogging", false);
        var enableDetailedErrors = databaseConfig.GetValue<bool>("EnableDetailedErrors", false);
        
        // Main write context
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var npgsqlOptions = options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(commandTimeout);
                npgsqlOptions.MigrationsAssembly("OilTrading.Infrastructure");
                
                if (enableRetryOnFailure)
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount, maxRetryDelay, null);
                }
            });
            
            if (enableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
                
            if (enableDetailedErrors)
                options.EnableDetailedErrors();
                
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        });
        
        // Read context (potentially read replica)
        var readConnectionString = configuration.GetConnectionString("ReadConnection") ?? connectionString;
        services.AddDbContext<ApplicationReadDbContext>(options =>
        {
            options.UseNpgsql(readConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(commandTimeout);
                
                if (enableRetryOnFailure)
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount, maxRetryDelay, null);
                }
            });
            
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });
    }
    
    private static void ConfigureCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        var cacheConfig = configuration.GetSection("Cache");
        
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            try
            {
                // Configure Redis with advanced options
                var redisConfig = cacheConfig.GetSection("Redis");
                
                // Register IConnectionMultiplexer for Redis
                services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
                {
                    var configOptions = new ConfigurationOptions
                    {
                        EndPoints = { redisConnectionString },
                        ConnectRetry = redisConfig.GetValue<int>("ConnectRetry", 3),
                        ConnectTimeout = redisConfig.GetValue<int>("ConnectTimeout", 5000),
                        SyncTimeout = redisConfig.GetValue<int>("SyncTimeout", 5000),
                        AsyncTimeout = redisConfig.GetValue<int>("AsyncTimeout", 5000),
                        KeepAlive = redisConfig.GetValue<int>("KeepAlive", 60),
                        AllowAdmin = redisConfig.GetValue<bool>("AllowAdmin", false),
                        AbortOnConnectFail = false
                    };
                    return ConnectionMultiplexer.Connect(configOptions);
                });
                
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                    options.InstanceName = redisConfig.GetValue<string>("InstanceName", "OilTrading");
                    
                    options.ConfigurationOptions = new ConfigurationOptions
                    {
                        EndPoints = { redisConnectionString },
                        ConnectRetry = redisConfig.GetValue<int>("ConnectRetry", 3),
                        ConnectTimeout = redisConfig.GetValue<int>("ConnectTimeout", 5000),
                        SyncTimeout = redisConfig.GetValue<int>("SyncTimeout", 5000),
                        AsyncTimeout = redisConfig.GetValue<int>("AsyncTimeout", 5000),
                        KeepAlive = redisConfig.GetValue<int>("KeepAlive", 60),
                        AllowAdmin = redisConfig.GetValue<bool>("AllowAdmin", false),
                        AbortOnConnectFail = false
                    };
                });
            }
            catch (Exception)
            {
                // Fallback to memory cache if Redis configuration fails
                ConfigureMemoryCache(services, cacheConfig);
                ConfigureFallbackRedis(services);
            }
        }
        else
        {
            ConfigureMemoryCache(services, cacheConfig);
            ConfigureFallbackRedis(services);
        }
    }
    
    private static void ConfigureMemoryCache(IServiceCollection services, IConfigurationSection cacheConfig)
    {
        var memoryCacheConfig = cacheConfig.GetSection("MemoryCache");
        
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = memoryCacheConfig.GetValue<long?>("SizeLimit", 1073741824); // 1GB default
            options.CompactionPercentage = memoryCacheConfig.GetValue<double>("CompactionPercentage", 0.20);
            options.ExpirationScanFrequency = memoryCacheConfig.GetValue<TimeSpan>("ExpirationScanFrequency", TimeSpan.FromMinutes(5));
        });
        
        services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
    }
    
    private static void ConfigureRepositories(IServiceCollection services)
    {
        // Core repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPurchaseContractRepository, PurchaseContractRepository>();
        services.AddScoped<ISalesContractRepository, SalesContractRepository>();
        services.AddScoped<ITradingPartnerRepository, TradingPartnerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IShippingOperationRepository, ShippingOperationRepository>();
        services.AddScoped<IPricingEventRepository, PricingEventRepository>();
        services.AddScoped<IMarketDataRepository, MarketDataRepository>();
        services.AddScoped<IPaperContractRepository, PaperContractRepository>();
        services.AddScoped<IFuturesDealRepository, FuturesDealRepository>();
        services.AddScoped<IPhysicalContractRepository, PhysicalContractRepository>();
        services.AddScoped<IPriceBenchmarkRepository, PriceBenchmarkRepository>();
        services.AddScoped<ISettlementRepository, SettlementRepository>();
        
        // Inventory repositories
        services.AddScoped<IInventoryLocationRepository, InventoryLocationRepository>();
        services.AddScoped<IInventoryPositionRepository, InventoryPositionRepository>();
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();
        
        // Trade management repositories
        services.AddScoped<ITradeGroupRepository, TradeGroupRepository>();
        services.AddScoped<ITradeChainRepository, TradeChainRepository>();
        
        // Tag management repositories
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IContractTagRepository, ContractTagRepository>();
        
        // Settlement repositories
        services.AddScoped<IContractSettlementRepository, ContractSettlementRepository>();
        
        // Financial Reporting
        services.AddScoped<IFinancialReportRepository, FinancialReportRepository>();
    }
    
    private static void ConfigureApplicationServices(IServiceCollection services, IConfiguration configuration)
    {
        // Database services
        services.AddScoped<ITimescaleDbService, TimescaleDbService>();
        services.AddScoped<IPositionCacheService, PositionCacheService>();
        
        // Cache services
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        
        // Audit and security services
        services.AddScoped<IAuditLogService, AuditLogService>();
        
        // Production enhancement services
        services.AddScoped<IBasisCalculationService, BasisCalculationService>();
        services.AddScoped<IPriceValidationService, PriceValidationService>();
        services.AddScoped<ITransactionCoordinatorService, TransactionCoordinatorService>();
        services.AddScoped<IDataArchivalService, DataArchivalService>();
        services.AddScoped<IMultiLayerCacheService, MultiLayerCacheService>();
        services.AddScoped<ICacheManagementService, CacheManagementService>();
        services.AddScoped<IRealTimeInventoryService, RealTimeInventoryService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IRealTimeRiskMonitoringService, RealTimeRiskMonitoringService>();
        services.AddScoped<IComplianceReportingService, ComplianceReportingService>();

        // Configuration management services
        // NOTE: IConfigurationManagementService is currently not implemented
        // This service would provide runtime configuration management features including:
        // - Dynamic system configuration updates without restart
        // - Configuration version control and rollback
        // - Environment-specific configuration management
        // - Configuration validation and schema enforcement
        // - Audit trail for configuration changes
        //
        // IMPLEMENTATION REQUIREMENTS:
        // 1. Create interface: IConfigurationManagementService in OilTrading.Core.Services
        // 2. Implement service: ConfigurationManagementService in OilTrading.Infrastructure.Services
        // 3. Add database entity: SystemConfiguration with fields:
        //    - ConfigurationKey (string, unique)
        //    - ConfigurationValue (JSON string)
        //    - Environment (Development/Staging/Production)
        //    - IsActive (bool)
        //    - Version (int)
        //    - ChangedBy (string)
        //    - ChangedAt (DateTime)
        // 4. Add repository: IConfigurationRepository
        // 5. Implement caching layer for configuration values (Redis)
        // 6. Add API endpoints for configuration CRUD operations
        // 7. Implement configuration change event notifications
        //
        // Once implemented, uncomment the line below:
        // services.AddScoped<IConfigurationManagementService, ConfigurationManagementService>();

        services.AddScoped<IDataValidationService, DataValidationService>();
        
        // Database optimization services
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<DatabaseConfigurationValidator>();
        services.AddScoped<DatabaseMaintenanceService>();
        
        // Register ApplicationDbContext as DbContext for services that need generic DbContext
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
    }
    
    private static void ConfigureFallbackRedis(IServiceCollection services)
    {
        // Register a fake IConnectionMultiplexer for when Redis is not available
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            // Create a minimal Redis configuration for fallback
            var config = new ConfigurationOptions
            {
                EndPoints = { "localhost:6379" },
                AbortOnConnectFail = false,
                ConnectTimeout = 1000,
                SyncTimeout = 1000
            };
            
            try
            {
                return ConnectionMultiplexer.Connect(config);
            }
            catch
            {
                // If even fallback Redis connection fails, create a NullConnectionMultiplexer
                // This will be handled gracefully by the services that use it
                return ConnectionMultiplexer.Connect("localhost:1"); // Will fail but create a valid object
            }
        });
    }
}