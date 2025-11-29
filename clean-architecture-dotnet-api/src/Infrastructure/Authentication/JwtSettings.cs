namespace Infrastructure.Authentication;

/// <summary>
/// Configuration for JWT token generation and validation.
/// </summary>
public class JwtSettings
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
