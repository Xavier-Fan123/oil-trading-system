using MediatR;
using OilTrading.Application.Common;
using OilTrading.Application.DTOs;
using OilTrading.Core.Entities;

namespace OilTrading.Application.Queries.Users;

public class GetUsersQuery : IRequest<PagedResult<UserSummaryDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SearchTerm { get; set; } = string.Empty;
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; } = true;
    public string SortBy { get; set; } = "LastName";
    public bool SortDescending { get; set; } = false;
}