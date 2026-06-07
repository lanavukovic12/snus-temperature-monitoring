using Shared;
using Shared.Data;
using Shared.Dtos;
using Shared.Models;
using IngestionService.Options;

namespace IngestionService.Services;

public class AlarmDetector
{
    private readonly AppDbContext _dbContext;
    private readonly AlarmNotifier _notifier;
    private readonly IReadOnlyDictionary<string, SensorThresholdOptions> _thresholds;
    private readonly ILogger<AlarmDetector> _logger;

    public AlarmDetector(
        AppDbContext dbContext,
        AlarmNotifier notifier,
        IConfiguration configuration,
        ILogger<AlarmDetector> logger)
    {
        _dbContext = dbContext;
        _notifier = notifier;
        _thresholds = configuration.GetSection("SensorThresholds")
            .Get<List<SensorThresholdOptions>>()?
            .ToDictionary(t => t.SensorId, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, SensorThresholdOptions>(StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task ProcessReadingAsync(
        string sensorId,
        double value,
        int sensorAlarmPriority,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        var priority = ResolveAlarmPriority(sensorId, value, sensorAlarmPriority);

        if (priority <= 0)
        {
            return;
        }

        var alarm = new AlarmLog
        {
            SensorId = sensorId,
            Value = value,
            AlarmPriority = priority,
            Timestamp = timestamp
        };

        _dbContext.AlarmLogs.Add(alarm);
        await _dbContext.SaveChangesAsync(cancellationToken);

        AlarmConsoleWriter.WriteAlarm(sensorId, value, priority, timestamp);

        _logger.LogWarning(
            "Alarm P{Priority} on {SensorId}: value={Value}",
            priority,
            sensorId,
            value);

        await _notifier.NotifyAsync(new AlarmNotificationDto
        {
            SensorId = sensorId,
            Value = value,
            AlarmPriority = priority,
            Timestamp = timestamp
        }, cancellationToken);
    }

    private int ResolveAlarmPriority(string sensorId, double value, int sensorAlarmPriority)
    {
        if (!_thresholds.TryGetValue(sensorId, out var thresholds))
        {
            return sensorAlarmPriority;
        }

        var serverPriority = AlarmEvaluator.Evaluate(
            value,
            thresholds.ThresholdPriority1,
            thresholds.ThresholdPriority2,
            thresholds.ThresholdPriority3);

        if (serverPriority != sensorAlarmPriority)
        {
            _logger.LogDebug(
                "Alarm priority mismatch for {SensorId}: sensor={SensorPriority}, server={ServerPriority}",
                sensorId,
                sensorAlarmPriority,
                serverPriority);
        }

        return serverPriority;
    }
}
