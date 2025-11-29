namespace Domain.Interfaces;

/// <summary>
/// Interface for password hashing and verification.
/// </summary>
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
