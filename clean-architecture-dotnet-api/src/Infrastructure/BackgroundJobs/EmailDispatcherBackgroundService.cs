namespace Infrastructure.BackgroundJobs;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Infrastructure.Email;

/// <summary>
/// Background worker for processing pending email tasks.
/// Demonstrates graceful shutdown and async patterns.
/// </summary>
public class EmailDispatcherBackgroundService : BackgroundService
{
    private readonly ILogger<EmailDispatcherBackgroundService> _logger;
    private readonly IEmailService _emailService;
    private const int DelayBetweenChecksMsec = 5000;

    public EmailDispatcherBackgroundService(
        ILogger<EmailDispatcherBackgroundService> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Dispatcher Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Simulate processing pending emails
                // In real app: query database for pending emails, send, mark as sent
                await ProcessPendingEmailsAsync(stoppingToken);

                await Task.Delay(DelayBetweenChecksMsec, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Email Dispatcher Background Service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Email Dispatcher Background Service");
            }
        }

        _logger.LogInformation("Email Dispatcher Background Service stopped");
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken cancellationToken)
    {
        // Mock implementation
        // In production: fetch pending emails from database
        await Task.CompletedTask;
    }
}
