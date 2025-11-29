namespace Application.DTOs;

/// <summary>
/// Authentication response with tokens.
/// </summary>
public class AuthTokenDto
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public int ExpiresInSeconds { get; set; }
    public DateTime IssuedAtUtc { get; set; }
}
