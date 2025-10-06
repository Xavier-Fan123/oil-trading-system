using Microsoft.EntityFrameworkCore;
using OilTrading.Core.Entities;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Data;

namespace OilTrading.Infrastructure.Repositories;

public class FinancialReportRepository : Repository<FinancialReport>, IFinancialReportRepository
{
    public FinancialReportRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<FinancialReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialReports
            .Include(fr => fr.TradingPartner)
            .FirstOrDefaultAsync(fr => fr.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialReport>> GetByTradingPartnerIdAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialReports
            .Where(fr => fr.TradingPartnerId == tradingPartnerId)
            .Include(fr => fr.TradingPartner)
            .OrderByDescending(fr => fr.ReportStartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialReport>> GetByTradingPartnerIdAndYearAsync(Guid tradingPartnerId, int year, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialReports
            .Where(fr => fr.TradingPartnerId == tradingPartnerId && fr.ReportStartDate.Year == year)
            .Include(fr => fr.TradingPartner)
            .OrderByDescending(fr => fr.ReportStartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingPeriodAsync(Guid tradingPartnerId, DateTime startDate, DateTime endDate, Guid? excludeReportId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.FinancialReports
            .Where(fr => fr.TradingPartnerId == tradingPartnerId);

        if (excludeReportId.HasValue)
        {
            query = query.Where(fr => fr.Id != excludeReportId.Value);
        }

        return await query.AnyAsync(fr => 
            (fr.ReportStartDate <= endDate && fr.ReportEndDate >= startDate), 
            cancellationToken);
    }

    public async Task<FinancialReport?> GetByTradingPartnerAndDateRangeAsync(Guid tradingPartnerId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialReports
            .Where(fr => fr.TradingPartnerId == tradingPartnerId 
                      && fr.ReportStartDate >= startDate 
                      && fr.ReportEndDate <= endDate)
            .Include(fr => fr.TradingPartner)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialReport>> GetRecentReportsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialReports
            .Include(fr => fr.TradingPartner)
            .OrderByDescending(fr => fr.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialReport>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialReports
            .Where(fr => fr.ReportStartDate >= startDate && fr.ReportEndDate <= endDate)
            .Include(fr => fr.TradingPartner)
            .OrderByDescending(fr => fr.ReportStartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetReportsCountForTradingPartnerAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default)
    {
        return await _context.FinancialReports
            .CountAsync(fr => fr.TradingPartnerId == tradingPartnerId, cancellationToken);
    }

    public async Task<bool> HasOverlappingReportAsync(Guid tradingPartnerId, DateTime startDate, DateTime endDate, Guid? excludeReportId = null, CancellationToken cancellationToken = default)
    {
        return await HasOverlappingPeriodAsync(tradingPartnerId, startDate, endDate, excludeReportId, cancellationToken);
    }

    public async Task<FinancialReport?> GetByIdWithTradingPartnerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialReport>> GetByTradingPartnerAndYearAsync(Guid tradingPartnerId, int year, CancellationToken cancellationToken = default)
    {
        return await GetByTradingPartnerIdAndYearAsync(tradingPartnerId, year, cancellationToken);
    }
}