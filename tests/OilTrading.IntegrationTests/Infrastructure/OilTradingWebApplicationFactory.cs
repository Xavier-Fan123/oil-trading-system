using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using OilTrading.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace OilTrading.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory for integration tests using Testcontainers (requires Docker).
/// Use InMemoryWebApplicationFactory if Docker is not available.
/// </summary>
public class OilTradingWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;

    public OilTradingWebApplicationFactory()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("OilTradingTestDb")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .WithPortBinding(0, 5432)
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(0, 6379)
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));
            services.RemoveAll(typeof(ApplicationReadDbContext));

            // Add test database contexts with PostgreSQL test containers
            var connectionString = _postgresContainer.GetConnectionString();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
            services.AddDbContext<ApplicationReadDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Configure Redis for testing
            var redisConnectionString = _redisContainer.GetConnectionString();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "OilTradingTestApp";
            });

            // Ensure the database is created and migrated
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _redisContainer.StopAsync();
        await base.DisposeAsync();
    }

    public ApplicationDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public T GetService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }
}

/// <summary>
/// In-memory WebApplicationFactory for integration tests (no Docker required).
/// Uses EF Core InMemory database and MemoryCache instead of Redis.
/// </summary>
public class InMemoryWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment FIRST before service configuration
        builder.UseEnvironment("Testing");

        // Set the actual environment variable so DependencyInjection.ConfigureDatabase detects it
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        // Configure connection string for InMemory database
        builder.ConfigureAppConfiguration(configBuilder =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "InMemory" }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbContextDescriptor2 = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationReadDbContext>));
            if (dbContextDescriptor2 != null)
            {
                services.Remove(dbContextDescriptor2);
            }

            // Remove Redis cache registration if exists
            var distributedCacheDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
            if (distributedCacheDescriptor != null)
            {
                services.Remove(distributedCacheDescriptor);
            }

            // Add in-memory database contexts
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });

            services.AddDbContext<ApplicationReadDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });

            // Use MemoryCache instead of Redis
            services.AddDistributedMemoryCache();

            // Build service provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                try
                {
                    db.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    var logger = scopedServices.GetRequiredService<ILogger<InMemoryWebApplicationFactory>>();
                    logger.LogError(ex, "An error occurred creating the test database.");
                }
            }
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Error);
        });
    }

    public ApplicationDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public T GetService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }
}