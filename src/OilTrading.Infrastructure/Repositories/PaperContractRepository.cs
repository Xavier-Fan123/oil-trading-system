using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Enums;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class PaperContractRepository : Repository<PaperContract>, IPaperContractRepository
{
    public PaperContractRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PaperContract?> GetByContractNumberAsync(string contractNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.ContractNumber == contractNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<PaperContract>> GetActiveContractsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == PaperContractStatus.Open)
            .OrderBy(p => p.ContractMonth)
            .ThenBy(p => p.ProductType)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaperContract>> GetByProductAndMonthAsync(string product, string month, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductType == product && p.ContractMonth == month)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateContractNumberAsync(CancellationToken cancellationToken = default)
    {
        var prefix = $"PAPER-{DateTime.UtcNow:yyyyMM}-";
        var lastNumber = await _dbSet
            .Where(p => p.ContractNumber.StartsWith(prefix))
            .OrderByDescending(p => p.ContractNumber)
            .Select(p => p.ContractNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(lastNumber))
            return $"{prefix}001";

        var numberPart = lastNumber.Substring(prefix.Length);
        if (int.TryParse(numberPart, out var number))
            return $"{prefix}{(number + 1):D3}";

        return $"{prefix}001";
    }

    public async Task<IEnumerable<PaperContract>> GetOpenPositionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == PaperContractStatus.Open)
            .OrderBy(p => p.ContractMonth)
            .ThenBy(p => p.ProductType)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaperContract>> GetByProductAsync(string productType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ProductType == productType)
            .OrderBy(p => p.ContractMonth)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaperContract>> GetByContractMonthAsync(string contractMonth, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ContractMonth == contractMonth)
            .OrderBy(p => p.ProductType)
            .ToListAsync(cancellationToken);
    }

    public override async Task<PaperContract> AddAsync(PaperContract paperContract, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(paperContract, cancellationToken);
        return paperContract;
    }

    public override async Task UpdateAsync(PaperContract paperContract, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(paperContract);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
            _dbSet.Remove(entity);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DATA LINEAGE ENHANCEMENT - Hedge Mapping Methods (v2.18.0)
    // Purpose: Query paper contracts based on hedge designation status
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaperContract>> GetDesignatedHedgesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsDesignatedHedge)
            .OrderBy(p => p.HedgeDesignationDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaperContract>> GetByHedgedContractIdAsync(
        Guid hedgedContractId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.HedgedContractId == hedgedContractId && p.IsDesignatedHedge)
            .OrderBy(p => p.HedgeDesignationDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaperContract>> GetByHedgedContractTypeAsync(
        HedgedContractType hedgedContractType,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.HedgedContractType == hedgedContractType && p.IsDesignatedHedge)
            .OrderBy(p => p.HedgeDesignationDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaperContract>> GetAvailableForHedgeDesignationAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Status == PaperContractStatus.Open && !p.IsDesignatedHedge)
            .OrderBy(p => p.ContractMonth)
            .ThenBy(p => p.ProductType)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaperContract>> GetLowEffectivenessHedgesAsync(
        decimal threshold = 80m,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsDesignatedHedge &&
                        p.HedgeEffectiveness.HasValue &&
                        p.HedgeEffectiveness.Value < threshold)
            .OrderBy(p => p.HedgeEffectiveness)
            .ToListAsync(cancellationToken);
    }
}