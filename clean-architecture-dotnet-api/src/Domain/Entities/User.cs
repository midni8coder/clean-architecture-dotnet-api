namespace Domain.Entities;

/// <summary>
/// User aggregate root - represents a user account in the system.
/// Contains business logic for user operations.
/// </summary>
public class User : AggregateRoot
{
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryUtc { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string Role { get; private set; } = "User";

    private User() { }

    public static User Create(string email, string firstName, string lastName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));
        
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = passwordHash,
            IsActive = true,
            Role = "User",
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        FirstName = firstName;
        LastName = lastName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetRefreshToken(string token, DateTime expiryUtc)
    {
        RefreshToken = token;
        RefreshTokenExpiryUtc = expiryUtc;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiryUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsRefreshTokenValid()
    {
        return !string.IsNullOrEmpty(RefreshToken) && 
               RefreshTokenExpiryUtc.HasValue && 
               RefreshTokenExpiryUtc > DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        ClearRefreshToken();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignRole(string role)
    {
        Role = role;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
