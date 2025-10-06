using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface IPaperContractRepository : IRepository<PaperContract>
{
    Task<PaperContract?> GetByContractNumberAsync(string contractNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaperContract>> GetActiveContractsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaperContract>> GetByProductAndMonthAsync(string product, string month, CancellationToken cancellationToken = default);
    Task<string> GenerateContractNumberAsync(CancellationToken cancellationToken = default);
    
    // Additional methods for compatibility
    Task<IEnumerable<PaperContract>> GetOpenPositionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PaperContract>> GetByProductAsync(string productType, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaperContract>> GetByContractMonthAsync(string contractMonth, CancellationToken cancellationToken = default);
}