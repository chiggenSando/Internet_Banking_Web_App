namespace Mcba.CustomerWebsite.Services;

// Runs the BillPayProcessor on startup and then every few seconds. The schedule lives in the
// database, so payments that fell due while the server was stopped are processed on startup.
public class BillPayBackgroundService(IServiceProvider services, ILogger<BillPayBackgroundService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        // Process immediately on startup, then once per tick.
        do
        {
            try
            {
                // McbaContext is scoped, so each run gets its own scope.
                using var scope = services.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<BillPayProcessor>();
                await processor.ProcessDueAsync(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                // Keep the service alive so the next tick can try again.
                logger.LogError(ex, "An error occurred processing scheduled bill payments.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
