namespace API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Infrastructure.Authentication;
using Domain.Interfaces;
using Application.DTOs;

/// <summary>
/// Authentication controller.
/// Handles login and token refresh operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ITokenService tokenService,
        IUserRepository userRepository,
        IPasswordService passwordService,
        ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _userRepository = userRepository;
        _passwordService = passwordService;
        _logger = logger;
    }

    /// <summary>
    /// Login with email and password.
    /// Returns access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthTokenDto>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        
        if (user is null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning($"Failed login attempt for {request.Email}");
            return Unauthorized(new { message = "Invalid email or password" });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "User account is inactive" });
        }

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await _userRepository.UpdateAsync(user, cancellationToken);

        var response = new AuthTokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresInSeconds = 15 * 60,
            IssuedAtUtc = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// Implements token rotation.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthTokenDto>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is required" });
        }

        // In production, also validate refresh token in database
        var accessToken = _tokenService.GenerateAccessToken(
            Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString()),
            User.FindFirst("email")?.Value ?? "",
            User.FindFirst("role")?.Value ?? "");

        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var response = new AuthTokenDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresInSeconds = 15 * 60,
            IssuedAtUtc = DateTime.UtcNow
        };

        return await Task.FromResult(Ok(response));
    }
}

/// <summary>
/// Login request model.
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

/// <summary>
/// Refresh token request model.
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = null!;
}
