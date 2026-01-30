using OilTrading.Core.Entities;
using OilTrading.Core.Enums;

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

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE ENHANCEMENT - Hedge Mapping Methods (v2.18.0)
    // Purpose: Query paper contracts based on hedge designation status
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get all paper contracts designated as hedges
    /// </summary>
    Task<IReadOnlyList<PaperContract>> GetDesignatedHedgesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paper contracts that hedge a specific physical contract
    /// </summary>
    Task<IReadOnlyList<PaperContract>> GetByHedgedContractIdAsync(
        Guid hedgedContractId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paper contracts that hedge a specific physical contract type
    /// </summary>
    Task<IReadOnlyList<PaperContract>> GetByHedgedContractTypeAsync(
        HedgedContractType hedgedContractType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paper contracts available for hedge designation (open and not already designated)
    /// </summary>
    Task<IReadOnlyList<PaperContract>> GetAvailableForHedgeDesignationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paper contracts with low hedge effectiveness (below threshold)
    /// </summary>
    Task<IReadOnlyList<PaperContract>> GetLowEffectivenessHedgesAsync(
        decimal threshold = 80m,
        CancellationToken cancellationToken = default);
}