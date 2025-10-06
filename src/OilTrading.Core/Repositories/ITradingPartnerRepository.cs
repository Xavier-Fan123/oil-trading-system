using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface ITradingPartnerRepository : IRepository<TradingPartner>
{
    Task<TradingPartner?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<TradingPartner?> GetByCompanyCodeAsync(string companyCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TradingPartner>> GetByTypeAsync(TradingPartnerType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TradingPartner>> GetActivePartnersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TradingPartner>> GetSuppliersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TradingPartner>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TradingPartner>> GetByCountryAsync(string country, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TradingPartner>> GetByCreditStatusAsync(bool exceeded, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task UpdateExposureAsync(Guid partnerId, decimal exposure, CancellationToken cancellationToken = default);
    Task<string> GenerateCompanyCodeAsync(CancellationToken cancellationToken = default);
}