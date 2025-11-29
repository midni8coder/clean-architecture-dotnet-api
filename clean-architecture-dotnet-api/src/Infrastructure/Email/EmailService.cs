namespace Infrastructure.Email;

using Microsoft.Extensions.Logging;

/// <summary>
/// Interface for email service abstraction.
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

/// <summary>
/// Mock email service for development.
/// In production, replace with SendGrid, AWS SES, or similar.
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Email sent to {to}: {subject}");
        return Task.CompletedTask;
    }
}
