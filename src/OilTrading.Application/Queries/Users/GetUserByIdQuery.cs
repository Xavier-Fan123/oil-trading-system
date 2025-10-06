using MediatR;
using OilTrading.Application.DTOs;

namespace OilTrading.Application.Queries.Users;

public class GetUserByIdQuery : IRequest<UserDto>
{
    public Guid Id { get; set; }

    public GetUserByIdQuery(Guid id)
    {
        Id = id;
    }
}