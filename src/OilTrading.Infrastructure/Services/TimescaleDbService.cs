using Microsoft.EntityFrameworkCore;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Services;

public interface ITimescaleDbService
{
    Task SetupHypertablesAsync();
    Task RefreshMaterializedViewsAsync();
}

public class TimescaleDbService : ITimescaleDbService
{
    private readonly ApplicationDbContext _context;

    public TimescaleDbService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SetupHypertablesAsync()
    {
        try
        {
            // Create hypertable for MarketData if it doesn't exist
            await _context.Database.ExecuteSqlRawAsync(@"
                SELECT create_hypertable('MarketData', 'Timestamp', 
                    chunk_time_interval => INTERVAL '1 day',
                    if_not_exists => TRUE
                );");

            // Create hypertable for PriceIndices if it doesn't exist
            await _context.Database.ExecuteSqlRawAsync(@"
                SELECT create_hypertable('PriceIndices', 'Timestamp', 
                    chunk_time_interval => INTERVAL '1 day',
                    if_not_exists => TRUE
                );");

            // Create hypertable for ContractEvents if it doesn't exist
            await _context.Database.ExecuteSqlRawAsync(@"
                SELECT create_hypertable('ContractEvents', 'Timestamp', 
                    chunk_time_interval => INTERVAL '1 day',
                    if_not_exists => TRUE
                );");
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail the application startup
            // This allows the application to work even without TimescaleDB extension
            Console.WriteLine($"Warning: Could not setup TimescaleDB hypertables: {ex.Message}");
        }
    }

    public async Task RefreshMaterializedViewsAsync()
    {
        try
        {
            await _context.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY daily_market_data;");
            await _context.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY daily_price_indices;");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not refresh materialized views: {ex.Message}");
        }
    }
}