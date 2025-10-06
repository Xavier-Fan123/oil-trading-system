using OilTrading.Core.Entities;
using OilTrading.Core.Common;

namespace OilTrading.Core.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetTradersAsync(CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<User>> GetPagedAsync(
        int page = 1,
        int pageSize = 10,
        string searchTerm = "",
        UserRole? role = null,
        bool? isActive = null,
        string sortBy = "LastName",
        bool sortDescending = false,
        CancellationToken cancellationToken = default);
}