namespace ConsensusService;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Data;
using Shared.Enums;
using Shared.Models;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsensusCalculator _calculator;
    private readonly ConsensusOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceScopeFactory scopeFactory,
        ConsensusCalculator calculator,
        IOptions<ConsensusOptions> options,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _calculator = calculator;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Consensus worker started (interval={Interval}s, window={Window}s)",
            _options.IntervalSeconds,
            _options.WindowSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CalculateConsensusAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Consensus calculation failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
        }
    }

    private async Task CalculateConsensusAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var from = now.AddSeconds(-_options.WindowSeconds);

        var readings = await dbContext.SensorReadings
            .Where(r => !r.IsConsensus)
            .Where(r => r.Quality == DataQuality.Good)
            .Where(r => r.Timestamp >= from && r.Timestamp <= now)
            .ToListAsync(cancellationToken);

        var result = _calculator.Calculate(readings, _options);
        if (result is null)
        {
            _logger.LogInformation(
                "Consensus skipped: not enough trustworthy readings in {Window}s window",
                _options.WindowSeconds);
            return;
        }

        foreach (var sensorId in result.MaliciousSensorIds)
        {
            var registry = await dbContext.SensorRegistry
                .FirstOrDefaultAsync(s => s.SensorId == sensorId, cancellationToken);

            if (registry is not null && registry.Quality != DataQuality.Bad)
            {
                registry.Quality = DataQuality.Bad;
                _logger.LogWarning("Sensor {SensorId} marked BAD as BFT outlier", sensorId);
            }
        }

        dbContext.SensorReadings.Add(new SensorReading
        {
            SensorId = "consensus",
            Value = result.Value,
            Timestamp = now,
            AlarmPriority = 0,
            Quality = DataQuality.Good,
            IsConsensus = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Consensus value {Value} stored. Outliers: {Outliers}",
            result.Value,
            result.MaliciousSensorIds.Count == 0
                ? "none"
                : string.Join(", ", result.MaliciousSensorIds));
    }
}
