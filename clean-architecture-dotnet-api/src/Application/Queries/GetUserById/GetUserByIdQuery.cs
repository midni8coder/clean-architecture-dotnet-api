namespace Application.Queries.GetUserById;

using MediatR;
using Application.DTOs;

/// <summary>
/// Query to retrieve a user by ID.
/// </summary>
public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;
