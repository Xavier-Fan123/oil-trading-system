using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface IRiskLimitRepository : IRepository<RiskLimit>
{
    Task<IEnumerable<RiskLimit>> GetActiveRiskLimitsAsync();
    Task<IEnumerable<RiskLimit>> GetLimitsByTypeAsync(string limitType);
    Task<IEnumerable<RiskLimit>> GetLimitsByScopeAsync(string limitScope, string scopeValue);
    Task<IEnumerable<RiskLimitBreach>> GetActiveBreachesAsync();
    Task<RiskLimitBreach?> GetBreachByIdAsync(int breachId);
    Task AddBreachAsync(RiskLimitBreach breach);
    Task<IEnumerable<RiskLimitBreach>> GetBreachesForLimitAsync(int limitId);
}