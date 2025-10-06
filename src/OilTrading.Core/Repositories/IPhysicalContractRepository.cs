using OilTrading.Core.Entities;

namespace OilTrading.Core.Repositories;

public interface IPhysicalContractRepository : IRepository<PhysicalContract>
{
    new Task<PhysicalContract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PhysicalContract?> GetByContractNumberAsync(string contractNumber, CancellationToken cancellationToken = default);
    new Task<IReadOnlyList<PhysicalContract>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhysicalContract>> GetByPartnerAsync(Guid partnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhysicalContract>> GetByTypeAsync(PhysicalContractType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhysicalContract>> GetActiveContractsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhysicalContract>> GetActiveContractsForPositionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhysicalContract>> GetUnsettledContractsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhysicalContract>> GetByLaycanPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<string> GenerateContractNumberAsync(PhysicalContractType type, CancellationToken cancellationToken = default);
    Task<decimal> CalculatePartnerExposureAsync(Guid partnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PhysicalContract>> GetByProductAndPeriodAsync(string productType, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}