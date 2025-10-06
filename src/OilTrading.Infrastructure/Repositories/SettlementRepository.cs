using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class SettlementRepository : Repository<Settlement>, ISettlementRepository
{
    public SettlementRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Settlement?> GetBySettlementNumberAsync(string settlementNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .ThenInclude(p => p.StatusHistory)
            .Include(s => s.Adjustments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .FirstOrDefaultAsync(s => s.SettlementNumber == settlementNumber, cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetByContractIdAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.Adjustments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.ContractId == contractId)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetByStatusAsync(SettlementStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.Status == status)
            .OrderBy(s => s.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetByTradingPartnerAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.Adjustments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.PayerPartyId == tradingPartnerId || s.PayeePartyId == tradingPartnerId)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetPendingSettlementsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.Status == SettlementStatus.Pending || 
                       s.Status == SettlementStatus.Approved || 
                       s.Status == SettlementStatus.Processing)
            .OrderBy(s => s.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetOverdueSettlementsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.DueDate.Date < today && 
                       s.Status != SettlementStatus.Completed && 
                       s.Status != SettlementStatus.Cancelled)
            .OrderBy(s => s.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetDueSettlementsAsync(DateTime? dueDate = null, CancellationToken cancellationToken = default)
    {
        var targetDate = dueDate ?? DateTime.UtcNow.Date;
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.DueDate.Date <= targetDate && 
                       s.Status != SettlementStatus.Completed && 
                       s.Status != SettlementStatus.Cancelled)
            .OrderBy(s => s.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.DueDate.Date >= startDate.Date && s.DueDate.Date <= endDate.Date)
            .OrderBy(s => s.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetByCreatedDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.CreatedDate.Date >= startDate.Date && s.CreatedDate.Date <= endDate.Date)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetWithPendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.Payments.Any(p => p.Status == PaymentStatus.Pending || 
                                           p.Status == PaymentStatus.Initiated || 
                                           p.Status == PaymentStatus.InTransit))
            .OrderBy(s => s.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetByPaymentStatusAsync(PaymentStatus paymentStatus, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.Payments.Any(p => p.Status == paymentStatus))
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetByTypeAsync(SettlementType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.Type == type)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetByTypesAsync(SettlementType[] types, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => types.Contains(s.Type))
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetByAmountRangeAsync(decimal minAmount, decimal maxAmount, string currency = "USD", CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.Amount.Currency == currency && 
                       s.Amount.Amount >= minAmount && 
                       s.Amount.Amount <= maxAmount)
            .OrderByDescending(s => s.Amount.Amount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetLargeSettlementsAsync(decimal threshold, string currency = "USD", CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.Amount.Currency == currency && s.Amount.Amount >= threshold)
            .OrderByDescending(s => s.Amount.Amount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> SearchSettlementsAsync(
        string? settlementNumber = null,
        Guid? contractId = null,
        SettlementStatus? status = null,
        SettlementType? type = null,
        Guid? tradingPartnerId = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        string? currency = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(s => s.Payments)
            .Include(s => s.Adjustments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(settlementNumber))
        {
            query = query.Where(s => s.SettlementNumber.Contains(settlementNumber));
        }

        if (contractId.HasValue)
        {
            query = query.Where(s => s.ContractId == contractId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(s => s.Type == type.Value);
        }

        if (tradingPartnerId.HasValue)
        {
            query = query.Where(s => s.PayerPartyId == tradingPartnerId.Value || s.PayeePartyId == tradingPartnerId.Value);
        }

        if (dueDateFrom.HasValue)
        {
            query = query.Where(s => s.DueDate.Date >= dueDateFrom.Value.Date);
        }

        if (dueDateTo.HasValue)
        {
            query = query.Where(s => s.DueDate.Date <= dueDateTo.Value.Date);
        }

        if (minAmount.HasValue && !string.IsNullOrWhiteSpace(currency))
        {
            query = query.Where(s => s.Amount.Currency == currency && s.Amount.Amount >= minAmount.Value);
        }

        if (maxAmount.HasValue && !string.IsNullOrWhiteSpace(currency))
        {
            query = query.Where(s => s.Amount.Currency == currency && s.Amount.Amount <= maxAmount.Value);
        }

        query = query.OrderByDescending(s => s.CreatedDate);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalSettlementAmountAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        SettlementStatus? status = null,
        string currency = "USD",
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(s => s.Amount.Currency == currency);

        if (startDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate.Date <= endDate.Value.Date);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return await query.SumAsync(s => s.Amount.Amount, cancellationToken);
    }

    public async Task<int> GetSettlementCountAsync(
        SettlementStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate.Date <= endDate.Value.Date);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Dictionary<SettlementStatus, int>> GetSettlementCountByStatusAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate.Date <= endDate.Value.Date);
        }

        return await query
            .GroupBy(s => s.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<Dictionary<string, decimal>> GetSettlementAmountByCurrencyAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        SettlementStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate.Date <= endDate.Value.Date);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        return await query
            .GroupBy(s => s.Amount.Currency)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(s => s.Amount.Amount), cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetRecentSettlementsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .OrderByDescending(s => s.CreatedDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<double> GetAverageSettlementTimeAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(s => s.Status == SettlementStatus.Completed && s.CompletedDate.HasValue);

        if (startDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.CreatedDate.Date <= endDate.Value.Date);
        }

        var settlements = await query.ToListAsync(cancellationToken);
        
        if (!settlements.Any())
            return 0;

        var totalDays = settlements.Sum(s => (s.CompletedDate!.Value - s.CreatedDate).TotalDays);
        return totalDays / settlements.Count;
    }

    public async Task<IEnumerable<Settlement>> GetSettlementsWithAdjustmentsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Adjustments)
            .Include(s => s.Payments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => s.Adjustments.Any())
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Settlement>> GetMultipleByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .Include(s => s.Adjustments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateMultipleAsync(IEnumerable<Settlement> settlements, CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(settlements);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> SettlementNumberExistsAsync(string settlementNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(s => s.SettlementNumber == settlementNumber, cancellationToken);
    }

    public async Task<bool> HasPendingSettlementsForContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(s => s.ContractId == contractId && 
            (s.Status == SettlementStatus.Pending || 
             s.Status == SettlementStatus.Approved || 
             s.Status == SettlementStatus.Processing), cancellationToken);
    }

    public override async Task<Settlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Payments)
            .ThenInclude(p => p.StatusHistory)
            .Include(s => s.Adjustments)
            .Include(s => s.PayerParty)
            .Include(s => s.PayeeParty)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var settlement = await GetByIdAsync(id, cancellationToken);
        if (settlement != null)
        {
            _dbSet.Remove(settlement);
        }
    }
}