using MediatR;
using OilTrading.Application.Common;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Queries.Users;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserSummaryDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedResult<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetPagedAsync(
            page: request.Page,
            pageSize: request.PageSize,
            searchTerm: request.SearchTerm,
            role: request.Role,
            isActive: request.IsActive,
            sortBy: request.SortBy,
            sortDescending: request.SortDescending
        );

        var userDtos = users.Items.Select(u => new UserSummaryDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            Role = u.Role,
            RoleName = u.Role.ToString(),
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt
        }).ToList();

        return new PagedResult<UserSummaryDto>(userDtos, users.TotalCount, users.PageNumber, users.PageSize);
    }
}