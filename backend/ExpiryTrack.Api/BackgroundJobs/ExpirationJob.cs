using ExpiryTrack.Api.Services;

namespace ExpiryTrack.Api.BackgroundJobs;

public class ExpirationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpirationJob> _logger;

    public ExpirationJob(IServiceScopeFactory scopeFactory, ILogger<ExpirationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // runs once at startup and then every 24 hours
        using var timer = new PeriodicTiemr(TimeSpan.FromHours(24));
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ExpirationService>();
                await service.RunCheckAsync(DateOnly.FromDateTime(DateTime.UtcNow));
            }
            // if shutdown occurs while waiting, WaitForNextTickAsync(stoppingToken) throws and OperationCanceledException
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured during expiration check.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken)); // WaitForNextTick waits asynchonously for the next timer tick 
    }
}