using Xunit;
using FluentAssertions;
using Domain.Entities;

namespace Domain.Tests.Entities;

/// <summary>
/// Unit tests for User aggregate.
/// </summary>
public class UserTests
{
    [Fact]
    public void Create_WithValidParameters_CreatesUser()
    {
        // Arrange
        var email = "test@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var passwordHash = "hashedpassword";

        // Act
        var user = User.Create(email, firstName, lastName, passwordHash);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBe(Guid.Empty);
        user.Email.Should().Be(email);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.PasswordHash.Should().Be(passwordHash);
        user.IsActive.Should().BeTrue();
        user.Role.Should().Be("User");
    }

    [Fact]
    public void Create_WithEmptyEmail_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(
            () => User.Create("", "John", "Doe", "hashedpassword"));
        
        ex.ParamName.Should().Be("email");
    }

    [Fact]
    public void UpdateProfile_WithValidData_UpdatesUser()
    {
        // Arrange
        var user = User.Create("test@example.com", "John", "Doe", "hashedpassword");
        var newFirstName = "Jane";
        var newLastName = "Smith";

        // Act
        user.UpdateProfile(newFirstName, newLastName);

        // Assert
        user.FirstName.Should().Be(newFirstName);
        user.LastName.Should().Be(newLastName);
        user.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void SetRefreshToken_WithValidToken_StoresToken()
    {
        // Arrange
        var user = User.Create("test@example.com", "John", "Doe", "hashedpassword");
        var token = "refresh_token_value";
        var expiry = DateTime.UtcNow.AddDays(7);

        // Act
        user.SetRefreshToken(token, expiry);

        // Assert
        user.RefreshToken.Should().Be(token);
        user.RefreshTokenExpiryUtc.Should().Be(expiry);
    }

    [Fact]
    public void IsRefreshTokenValid_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var user = User.Create("test@example.com", "John", "Doe", "hashedpassword");
        user.SetRefreshToken("token", DateTime.UtcNow.AddDays(7));

        // Act
        var isValid = user.IsRefreshTokenValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsRefreshTokenValid_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var user = User.Create("test@example.com", "John", "Doe", "hashedpassword");
        user.SetRefreshToken("token", DateTime.UtcNow.AddDays(-1));

        // Act
        var isValid = user.IsRefreshTokenValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenCalled_DeactivatesUser()
    {
        // Arrange
        var user = User.Create("test@example.com", "John", "Doe", "hashedpassword");
        user.SetRefreshToken("token", DateTime.UtcNow.AddDays(7));

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiryUtc.Should().BeNull();
    }
}
