using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface IFinancialReportRepository : IRepository<FinancialReport>
{
    new Task<FinancialReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialReport>> GetByTradingPartnerIdAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialReport>> GetByTradingPartnerIdAndYearAsync(Guid tradingPartnerId, int year, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingPeriodAsync(Guid tradingPartnerId, DateTime startDate, DateTime endDate, Guid? excludeReportId = null, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingReportAsync(Guid tradingPartnerId, DateTime startDate, DateTime endDate, Guid? excludeReportId = null, CancellationToken cancellationToken = default);
    Task<FinancialReport?> GetByTradingPartnerAndDateRangeAsync(Guid tradingPartnerId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<FinancialReport?> GetByIdWithTradingPartnerAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialReport>> GetByTradingPartnerAndYearAsync(Guid tradingPartnerId, int year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialReport>> GetRecentReportsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialReport>> GetReportsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<int> GetReportsCountForTradingPartnerAsync(Guid tradingPartnerId, CancellationToken cancellationToken = default);
}