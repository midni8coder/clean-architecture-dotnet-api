namespace Infrastructure.Security;

using Domain.Interfaces;
using BCrypt.Net;

/// <summary>
/// Password hashing service using BCrypt.
/// </summary>
public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        return BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
