namespace API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Application.Queries.GetUserById;
using Application.Commands.CreateUser;

/// <summary>
/// Users API controller.
/// Demonstrates RESTful API design with async patterns and authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get user by ID.
    /// Requires authentication.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<dynamic>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new user.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<dynamic>> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
    }
}
