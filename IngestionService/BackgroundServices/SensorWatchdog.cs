using Microsoft.Extensions.Options;
using IngestionService.Options;
using IngestionService.Services;

namespace IngestionService.BackgroundServices;

public class SensorWatchdog : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FaultToleranceOptions _options;
    private readonly ILogger<SensorWatchdog> _logger;

    public SensorWatchdog(
        IServiceScopeFactory scopeFactory,
        IOptions<FaultToleranceOptions> options,
        ILogger<SensorWatchdog> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Sensor watchdog started (required active: {Required}, timeout: {Timeout}s)",
            _options.RequiredActiveSensors,
            _options.InactivityTimeoutSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var registryService = scope.ServiceProvider.GetRequiredService<SensorRegistryService>();
                await registryService.RunWatchdogCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sensor watchdog cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.WatchdogIntervalSeconds), stoppingToken);
        }
    }
}
