using OilTrading.Application;
using OilTrading.Application.Services;
using OilTrading.Infrastructure;
using OilTrading.Infrastructure.Services;
using OilTrading.Api.Middleware;
using OilTrading.Api.Filters;
using OilTrading.Api.Services;
using OilTrading.Api.Converters;
using OilTrading.Infrastructure.Data;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Repositories;
using Serilog;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using OfficeOpenXml;
using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using System.Threading.RateLimiting;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using Prometheus;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

var builder = WebApplication.CreateBuilder(args);

// EPPlus License is configured in ExcelImportService constructor

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/oil-trading-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization for consistent date handling
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;

        // Use ISO 8601 format for all DateTime serialization
        options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateTimeConverter());

        // Allow enum values to be either numbers or strings
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Handle nullable values properly
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        // Preserve property names case for API consistency
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// API Versioning - Disabled for simple /api/ routing
// All endpoints use /api/{resource} format without version prefix
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = false;
    // Use URL segment reader but don't enforce it (simple /api/ paths)
    options.ApiVersionReader = new QueryStringApiVersionReader();
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = false;
});

builder.Services.AddEndpointsApiExplorer();

// Add Memory Cache - Required by many services
builder.Services.AddMemoryCache();

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/json", "text/plain" });
});

// Add response caching
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 1024; // 1 MB
    options.UseCaseSensitivePaths = false;
});

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v2.0", new OpenApiInfo
    {
        Title = "Oil Trading API",
        Version = "v2.0",
        Description = "Enterprise Oil Trading and Risk Management System API - v2.0",
        Contact = new OpenApiContact
        {
            Name = "Oil Trading Support",
            Email = "support@oiltrading.com"
        }
    });

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add file upload operation filter
    c.OperationFilter<FileUploadOperationFilter>();

    // Add JWT Bearer authorization
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add comprehensive health checks with dependency monitoring
builder.Services.AddHealthChecks()
    // Self check - basic API availability
    .AddCheck("self", () => HealthCheckResult.Healthy("API is healthy"), tags: new[] { "ready", "live" })

    // Disk space check - critical infrastructure
    .AddCheck("disk-space", () =>
    {
        var drive = new DriveInfo(Directory.GetCurrentDirectory());
        var freeSpacePercentage = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
        return freeSpacePercentage > 10
            ? HealthCheckResult.Healthy($"Free disk space: {freeSpacePercentage:F1}%")
            : HealthCheckResult.Unhealthy($"Low disk space: {freeSpacePercentage:F1}%");
    }, tags: new[] { "infrastructure" })

    // Custom health checks with detailed diagnostics
    .AddCheck<OilTrading.Api.HealthChecks.DatabaseHealthCheck>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db", "sql" })

    .AddCheck<OilTrading.Api.HealthChecks.CacheHealthCheck>(
        "redis-cache",
        failureStatus: HealthStatus.Degraded, // Cache failure is degraded, not unhealthy
        tags: new[] { "cache", "redis" })

    .AddCheck<OilTrading.Api.HealthChecks.RiskEngineHealthCheck>(
        "risk-engine",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "business", "risk" });

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "OilTrading.Api",
            serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.namespace"] = "oil-trading",
            ["service.team"] = "trading-team"
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString());
            };
            options.EnrichWithHttpResponse = (activity, response) =>
            {
                activity.SetTag("http.response.content_length", response.ContentLength);
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                activity.SetTag("http.request.method", request.Method.ToString());
            };
        })
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.SetDbStatementForStoredProcedure = true;
            options.EnrichWithIDbCommand = (activity, command) =>
            {
                activity.SetTag("db.command_timeout", command.CommandTimeout);
            };
        })
        .AddSqlClientInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.RecordException = true;
        })
        .AddRedisInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
            options.Protocol = OtlpExportProtocol.Grpc;
        })
        .AddJaegerExporter(options =>
        {
            options.AgentHost = builder.Configuration["Jaeger:AgentHost"] ?? "localhost";
            options.AgentPort = int.Parse(builder.Configuration["Jaeger:AgentPort"] ?? "6831");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // .AddRuntimeInstrumentation() // Commented out - missing extension
        // .AddProcessInstrumentation() // Commented out - missing extension
        .AddPrometheusExporter());

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableDependencyTrackingTelemetryModule = true;
    options.EnablePerformanceCounterCollectionModule = true;
    options.EnableRequestTrackingTelemetryModule = true;
    options.EnableEventCounterCollectionModule = true;
});

// Add Prometheus metrics
builder.Services.AddSingleton(Metrics.DefaultRegistry);

// Add application services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Configure options for application services
builder.Services.Configure<OilTrading.Application.Services.DataArchivalOptions>(options =>
{
    options.ArchiveStoragePath = Path.Combine("Data", "Archives");
    options.BackupStoragePath = Path.Combine("Data", "Backups");
    options.AuditLogRetentionYears = 7;
    options.ContractRetentionYears = 5;
    options.PricingEventRetentionMonths = 24;
    options.EnableCompression = true;
    options.EnableEncryption = false;
});

// Add CSV import service (replaces problematic Excel import)
builder.Services.AddScoped<CsvImportService>();

// Add Contract Number Generator
builder.Services.AddScoped<IContractNumberGenerator, ContractNumberGenerator>();

// Excel import service for market data upload
builder.Services.AddScoped<ExcelImportService>();

// Transaction operation factory for business workflows
builder.Services.AddScoped<ITransactionOperationFactory, TransactionOperationFactory>();

// Business workflow service with integrated risk management
builder.Services.AddScoped<IBusinessWorkflowService, BusinessWorkflowService>();

// Trade chain tracking service
builder.Services.AddScoped<ITradeChainService, TradeChainService>();
builder.Services.AddScoped<ITradeChainRepository, TradeChainRepository>();

// GraphQL services removed for production stability

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:4000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    
    // Add policy for file:// protocol and general development
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Applying pending database migrations...");
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            logger.LogInformation("Found {MigrationCount} pending migrations. Applying...", pendingMigrations.Count());
            await context.Database.MigrateAsync();
            logger.LogInformation("Successfully applied all pending migrations");
        }
        else
        {
            logger.LogInformation("No pending migrations to apply");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to apply database migrations. Application will continue but database may not be properly initialized.");
    }
}

// Handle command line arguments for database operations
// Only handle specific database commands, not framework arguments like --environment
if (args.Length > 0 && args.Any(arg => arg.StartsWith("--") && !arg.StartsWith("--environment") && !arg.StartsWith("--contentRoot") && !arg.StartsWith("--applicationName")))
{
    var dbCommands = args.Where(arg =>
        arg.Equals("--initialize-database", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("--validate-database", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("--create-indexes", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("--validate-config", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("-h", StringComparison.OrdinalIgnoreCase)
    ).ToArray();

    if (dbCommands.Length > 0)
    {
        await HandleCommandLineArgumentsAsync(app, dbCommands);
        return;
    }
}

// Enhanced database initialization based on environment and connection string
// await InitializeDatabaseAsync(app); // Commented out - function signature mismatch

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v2.0/swagger.json", "Oil Trading API v2.0");
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Oil Trading API v1 (Legacy)");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.DefaultModelsExpandDepth(-1); // Hide schemas by default for cleaner UI
    });
}

// Add global exception handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// Add risk checking middleware for high-risk operations
app.UseMiddleware<RiskCheckMiddleware>();

// CORS must be configured early in the pipeline
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowReactApp");
}

// Use response compression
app.UseResponseCompression();

// Use response caching
app.UseResponseCaching();

// Use rate limiting
app.UseRateLimiter();

app.UseSerilogRequestLogging();

// Disable HTTPS redirection in development for easier testing
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add Prometheus metrics endpoint
app.UseMetricServer();
app.UseHttpMetrics();

// Add comprehensive health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message
            })
        }, new JsonSerializerOptions { WriteIndented = true });
        
        await context.Response.WriteAsync(result);
    }
}).WithTags("Health");

// Kubernetes-style health checks
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            })
        }, new JsonSerializerOptions { WriteIndented = true });

        await context.Response.WriteAsync(result);
    }
}).WithTags("Health");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow
        }));
    }
}).WithTags("Health");

// Detailed health endpoint with business metrics
app.MapGet("/health/detailed", async (IServiceProvider services) =>
{
    var healthCheckService = services.GetRequiredService<HealthCheckService>();
    var report = await healthCheckService.CheckHealthAsync();
    
    // Add business-specific health metrics
    var dbContext = services.GetService<ApplicationDbContext>();
    var businessMetrics = new Dictionary<string, object>();
    
    try
    {
        if (dbContext != null)
        {
            businessMetrics["activeContracts"] = await dbContext.PurchaseContracts
                .CountAsync(c => c.Status == ContractStatus.Active);
            businessMetrics["todayPricingEvents"] = await dbContext.PricingEvents
                .CountAsync(p => p.EventDate.Date == DateTime.UtcNow.Date);
            businessMetrics["totalTradingPartners"] = await dbContext.TradingPartners
                .CountAsync(t => t.IsActive);
        }
    }
    catch (Exception ex)
    {
        businessMetrics["databaseError"] = ex.Message;
    }
    
    return new
    {
        status = report.Status.ToString(),
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName,
        version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
        uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
        businessMetrics,
        systemChecks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration.TotalMilliseconds
        })
    };
}).WithTags("Health");

try
{
    Log.Information("Starting Oil Trading API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Command line argument handling for database operations
static async Task HandleCommandLineArgumentsAsync(WebApplication app, string[] args)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        foreach (var arg in args)
        {
            switch (arg.ToLower())
            {
                case "--initialize-database":
                    await InitializeDatabaseAsync(scope, logger);
                    break;
                    
                case "--validate-database":
                    await ValidateDatabaseAsync(scope, logger);
                    break;
                    
                case "--create-indexes":
                    await CreateOptimalIndexesAsync(scope, logger);
                    break;
                    
                case "--validate-config":
                    await ValidateConfigurationAsync(scope, logger);
                    break;
                    
                case "--help":
                case "-h":
                    ShowHelp(logger);
                    break;
                    
                default:
                    logger.LogWarning("Unknown argument: {Argument}", arg);
                    break;
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling command line arguments");
        Environment.Exit(1);
    }
}

static async Task InitializeDatabaseAsync(IServiceScope scope, ILogger logger)
{
    logger.LogInformation("Initializing database with enhanced features...");
    
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbInitializer = new DatabaseInitializer(context, scope.ServiceProvider.GetRequiredService<ILogger<DatabaseInitializer>>());
    
    await dbInitializer.InitializeAsync();
    logger.LogInformation("Database initialization completed successfully");
}

static async Task ValidateDatabaseAsync(IServiceScope scope, ILogger logger)
{
    logger.LogInformation("Validating database integrity...");
    
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbInitializer = new DatabaseInitializer(context, scope.ServiceProvider.GetRequiredService<ILogger<DatabaseInitializer>>());
    
    var result = await dbInitializer.ValidateDatabaseAsync();
    logger.LogInformation("Database validation result: {Result}", result.GetSummary());
    
    if (!result.IsValid)
    {
        foreach (var error in result.Errors)
        {
            logger.LogError("Validation error: {Error}", error);
        }
        Environment.Exit(1);
    }
}

static async Task CreateOptimalIndexesAsync(IServiceScope scope, ILogger logger)
{
    logger.LogInformation("Creating optimal database indexes...");
    
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var dbInitializer = new DatabaseInitializer(context, scope.ServiceProvider.GetRequiredService<ILogger<DatabaseInitializer>>());
    
    await dbInitializer.CreateOptimalIndexesAsync();
    logger.LogInformation("Optimal indexes created successfully");
}

static async Task ValidateConfigurationAsync(IServiceScope scope, ILogger logger)
{
    logger.LogInformation("Validating database configuration...");
    
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var configValidator = new DatabaseConfigurationValidator(configuration, scope.ServiceProvider.GetRequiredService<ILogger<DatabaseConfigurationValidator>>());
    
    var result = await configValidator.ValidateConfigurationAsync();
    logger.LogInformation("Configuration validation result: {Result}", result.GetSummary());
    
    var recommendations = configValidator.GenerateRecommendations();
    recommendations.PrintRecommendations(logger);
    
    if (!result.IsValid)
    {
        foreach (var error in result.Errors)
        {
            logger.LogError("Configuration error: {Error}", error);
        }
        Environment.Exit(1);
    }
}

static void ShowHelp(ILogger logger)
{
    logger.LogInformation("Oil Trading System - Database Management Commands");
    logger.LogInformation("");
    logger.LogInformation("Available commands:");
    logger.LogInformation("  --initialize-database    Initialize database with enhanced features and seed data");
    logger.LogInformation("  --validate-database      Validate database integrity and structure");
    logger.LogInformation("  --create-indexes         Create optimal performance indexes");
    logger.LogInformation("  --validate-config        Validate database configuration settings");
    logger.LogInformation("  --help, -h              Show this help message");
    logger.LogInformation("");
    logger.LogInformation("Example usage:");
    logger.LogInformation("  dotnet run -- --initialize-database");
    logger.LogInformation("  dotnet run -- --validate-database --validate-config");
}

// Enhanced database initialization method - REMOVED DUE TO DUPLICATION

static async Task InitializeInMemoryDatabaseAsync(ApplicationDbContext context, ILogger logger)
{
    logger.LogInformation("Setting up in-memory database");
    
    // Ensure database is created
    await context.Database.EnsureCreatedAsync();
    
    // Seed with basic test data
    await SeedBasicTestDataAsync(context, logger);
}

static async Task InitializeSqliteDatabaseAsync(ApplicationDbContext context, ILogger logger)
{
    logger.LogInformation("Setting up SQLite database");

    try
    {
        // Apply all migrations - this will create the database if it doesn't exist
        // EnsureCreatedAsync() doesn't work well with IsRowVersion() on SQLite, so use MigrateAsync() instead
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        logger.LogInformation("Found {Count} pending migrations", pendingMigrations.Count());

        await context.Database.MigrateAsync();
        logger.LogInformation("Successfully applied all migrations");

        // Seed with comprehensive test data
        await SeedBasicTestDataAsync(context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during SQLite database initialization");
        throw;
    }
}

static async Task InitializePostgreSQLDatabaseAsync(IServiceScope scope, ApplicationDbContext context, ILogger logger, string environment)
{
    logger.LogInformation("Setting up PostgreSQL database for environment: {Environment}", environment);
    
    try
    {
        // Check database connectivity
        if (!await context.Database.CanConnectAsync())
        {
            logger.LogWarning("Cannot connect to PostgreSQL database. Waiting for database to be ready...");
            
            // Wait up to 60 seconds for database to be ready
            var maxRetries = 12;
            var retryCount = 0;
            while (!await context.Database.CanConnectAsync() && retryCount < maxRetries)
            {
                await Task.Delay(5000); // Wait 5 seconds
                retryCount++;
                logger.LogInformation("Retry {Retry}/{MaxRetries} - Checking database connectivity", retryCount, maxRetries);
            }
            
            if (retryCount >= maxRetries)
            {
                throw new InvalidOperationException("Cannot establish connection to PostgreSQL database after maximum retries");
            }
        }
        
        // Apply migrations
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Applying {Count} pending migrations to PostgreSQL", pendingMigrations.Count());
            await context.Database.MigrateAsync();
        }
        else
        {
            logger.LogInformation("No pending migrations found");
        }
        
        // Initialize database with optimizations
        var dbInitializer = scope.ServiceProvider.GetService<DatabaseInitializer>();
        if (dbInitializer != null)
        {
            await dbInitializer.InitializeAsync();
        }
        
        // Seed data based on environment
        if (environment == "Development" || environment == "Staging")
        {
            await SeedBasicTestDataAsync(context, logger);
        }
        else if (environment == "Production")
        {
            // In production, only seed reference data if tables are empty
            await SeedProductionReferenceDataAsync(context, logger);
        }
        
        logger.LogInformation("PostgreSQL database setup completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize PostgreSQL database");
        throw;
    }
}

static async Task SeedBasicTestDataAsync(ApplicationDbContext context, ILogger logger)
{
    // Check if data already exists
    var needsUsers = !await context.Users.AnyAsync();
    var needsBenchmarks = !await context.PriceBenchmarks.AnyAsync();
    var needsProducts = !await context.Products.AnyAsync();
    var needsPartners = !await context.TradingPartners.AnyAsync();
    
    if (!needsUsers && !needsBenchmarks && !needsProducts && !needsPartners)
    {
        logger.LogInformation("Database already contains test data, skipping seeding");
        return;
    }
    
    logger.LogInformation("Seeding basic test data");
    
    // Add test users
    if (needsUsers)
    {
        var testUsers = new[]
        {
            new User
            {
                Email = "trader@oiltrading.com",
                FirstName = "Test",
                LastName = "Trader",
                PasswordHash = "hashed_password",
                Role = UserRole.Trader,
                IsActive = true
            },
            new User
            {
                Email = "admin@oiltrading.com",
                FirstName = "Admin",
                LastName = "User",
                PasswordHash = "hashed_password",
                Role = UserRole.Administrator,
                IsActive = true
            }
        };
        context.Users.AddRange(testUsers);
    }
    
    // Add test products
    if (needsProducts)
    {
        var products = new[]
        {
            new Product 
            { 
                Code = "BRENT",
                Name = "Brent Crude Oil",
                ProductName = "Brent Crude Oil",
                ProductCode = "BRENT",
                Type = ProductType.CrudeOil,
                ProductType = ProductType.CrudeOil,
                Grade = "Light Sweet",
                Specification = "API 38, Sulfur 0.37%",
                UnitOfMeasure = "BBL",
                Density = 835.0m,
                Origin = "North Sea",
                IsActive = true
            },
            new Product 
            { 
                Code = "WTI",
                Name = "West Texas Intermediate",
                ProductName = "West Texas Intermediate",
                ProductCode = "WTI",
                Type = ProductType.CrudeOil,
                ProductType = ProductType.CrudeOil,
                Grade = "Light Sweet",
                Specification = "API 39.6, Sulfur 0.24%",
                UnitOfMeasure = "BBL",
                Density = 827.0m,
                Origin = "United States",
                IsActive = true
            },
            new Product 
            { 
                Code = "MGO",
                Name = "Marine Gas Oil",
                ProductName = "Marine Gas Oil",
                ProductCode = "MGO",
                Type = ProductType.RefinedProducts,
                ProductType = ProductType.RefinedProducts,
                Grade = "0.5% Sulfur",
                Specification = "ISO 8217:2017",
                UnitOfMeasure = "MT",
                Density = 890.0m,
                Origin = "Singapore",
                IsActive = true
            }
        };
        context.Products.AddRange(products);
    }
    
    // Add test trading partners
    if (needsPartners)
    {
        var partners = new[]
        {
            new TradingPartner 
            { 
                Code = "SHELL",
                Name = "Shell Trading",
                CompanyName = "Royal Dutch Shell",
                CompanyCode = "SHELL",
                Type = TradingPartnerType.Both,
                ContactEmail = "trading@shell.com",
                ContactPhone = "+65 6384 8000",
                Address = "12 Marina Boulevard, Singapore",
                Country = "Singapore",
                TaxId = "199404942G",
                IsActive = true,
                CreditLimit = 50000000m,
                CreditRating = "AA"
            },
            new TradingPartner 
            { 
                Code = "BP",
                Name = "BP Trading",
                CompanyName = "BP plc",
                CompanyCode = "BP",
                Type = TradingPartnerType.Both,
                ContactEmail = "trading@bp.com",
                ContactPhone = "+65 6349 3888",
                Address = "1 Harbour Front Place, Singapore",
                Country = "Singapore",
                TaxId = "200001234K",
                IsActive = true,
                CreditLimit = 45000000m,
                CreditRating = "AA"
            }
        };
        context.TradingPartners.AddRange(partners);
    }
    
    // Add price benchmarks
    if (needsBenchmarks)
    {
        var benchmarks = new[]
        {
            new PriceBenchmark("ICE_BRENT_FUTURE", BenchmarkType.ICE, "Crude Oil", "USD", "BBL"),
            new PriceBenchmark("BRENT_1ST_LINE", BenchmarkType.BRENT, "Crude Oil", "USD", "BBL"),
            new PriceBenchmark("MOPS_FO_380CST_FOB_SG", BenchmarkType.MOPS, "Fuel Oil", "USD", "MT"),
            new PriceBenchmark("MOPS_MARINE_FUEL_0_5PCT", BenchmarkType.MOPS, "Marine Fuel", "USD", "MT")
        };
        
        foreach (var benchmark in benchmarks)
        {
            benchmark.UpdateDetails(
                $"{benchmark.BenchmarkName} price benchmark for {benchmark.ProductCategory}", 
                "Market Data Provider", 
                "system");
        }
        
        context.PriceBenchmarks.AddRange(benchmarks);
    }
    
    await context.SaveChangesAsync();
    logger.LogInformation("Basic test data seeded successfully");
}

static async Task SeedProductionReferenceDataAsync(ApplicationDbContext context, ILogger logger)
{
    logger.LogInformation("Checking production reference data");
    
    // Only seed essential reference data if tables are completely empty
    var needsUsers = !await context.Users.AnyAsync();
    var needsProducts = !await context.Products.AnyAsync();
    
    if (needsUsers)
    {
        logger.LogInformation("Creating default admin user for production");
        var adminUser = new User
        {
            Email = "admin@oiltrading.com",
            FirstName = "System",
            LastName = "Administrator",
            PasswordHash = "change_on_first_login",
            Role = UserRole.Administrator,
            IsActive = true
        };
        context.Users.Add(adminUser);
    }
    
    if (needsProducts)
    {
        logger.LogInformation("Creating essential product catalog for production");
        var essentialProducts = new[]
        {
            new Product 
            { 
                Code = "BRENT",
                Name = "Brent Crude Oil",
                ProductName = "Brent Crude Oil",
                ProductCode = "BRENT",
                Type = ProductType.CrudeOil,
                ProductType = ProductType.CrudeOil,
                Grade = "Light Sweet",
                Specification = "API 38, Sulfur 0.37%",
                UnitOfMeasure = "BBL",
                Density = 835.0m,
                Origin = "North Sea",
                IsActive = true
            },
            new Product 
            { 
                Code = "WTI",
                Name = "West Texas Intermediate",
                ProductName = "West Texas Intermediate",
                ProductCode = "WTI",
                Type = ProductType.CrudeOil,
                ProductType = ProductType.CrudeOil,
                Grade = "Light Sweet",
                Specification = "API 39.6, Sulfur 0.24%",
                UnitOfMeasure = "BBL",
                Density = 827.0m,
                Origin = "United States",
                IsActive = true
            }
        };
        context.Products.AddRange(essentialProducts);
    }
    
    if (needsUsers || needsProducts)
    {
        await context.SaveChangesAsync();
        logger.LogInformation("Production reference data seeded successfully");
    }
    else
    {
        logger.LogInformation("Production database already contains reference data");
    }
}

// Make Program class accessible for testing
public partial class Program { }