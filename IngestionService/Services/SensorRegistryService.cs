using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Data;
using Shared.Dtos;
using Shared.Enums;
using Shared.Models;
using IngestionService.Options;

namespace IngestionService.Services;

public class SensorRegistryService
{
    private readonly AppDbContext _dbContext;
    private readonly FaultToleranceOptions _options;
    private readonly ILogger<SensorRegistryService> _logger;

    public SensorRegistryService(
        AppDbContext dbContext,
        IOptions<FaultToleranceOptions> options,
        ILogger<SensorRegistryService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SensorRegistry> GetOrCreateAsync(string sensorId, DataQuality quality, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.SensorRegistry
            .FirstOrDefaultAsync(s => s.SensorId == sensorId, cancellationToken);

        if (entry is not null)
        {
            return entry;
        }

        entry = new SensorRegistry
        {
            SensorId = sensorId,
            Status = SensorStatus.Standby,
            Quality = quality,
            RegisteredAt = DateTime.UtcNow
        };

        _dbContext.SensorRegistry.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered new sensor {SensorId} as STANDBY", sensorId);
        return entry;
    }

    public bool CanIngest(SensorRegistry entry)
    {
        if (entry.BlockedUntil.HasValue && entry.BlockedUntil.Value > DateTime.UtcNow)
        {
            return false;
        }

        return entry.Status == SensorStatus.Active;
    }

    public string GetIngestRejectionReason(SensorRegistry entry)
    {
        if (entry.BlockedUntil.HasValue && entry.BlockedUntil.Value > DateTime.UtcNow)
        {
            return $"Sensor {entry.SensorId} is blocked until {entry.BlockedUntil:O}.";
        }

        if (entry.Status != SensorStatus.Active)
        {
            return $"Sensor {entry.SensorId} is {entry.Status} and cannot send readings.";
        }

        return string.Empty;
    }

    public async Task RunWatchdogCycleAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var inactivityCutoff = now.AddSeconds(-_options.InactivityTimeoutSeconds);

        var sensors = await _dbContext.SensorRegistry
            .OrderBy(s => s.RegisteredAt)
            .ThenBy(s => s.SensorId)
            .ToListAsync(cancellationToken);

        foreach (var sensor in sensors)
        {
            if (sensor.BlockedUntil.HasValue && sensor.BlockedUntil.Value <= now)
            {
                sensor.BlockedUntil = null;
            }
        }

        var demotedForInactivity = new HashSet<string>();

        foreach (var sensor in sensors.Where(s => s.Status == SensorStatus.Active))
        {
            if (!sensor.LastSeenAt.HasValue || sensor.LastSeenAt.Value < inactivityCutoff)
            {
                sensor.Status = SensorStatus.Standby;
                demotedForInactivity.Add(sensor.SensorId);
                _logger.LogWarning(
                    "Sensor {SensorId} demoted to STANDBY (last seen: {LastSeenAt})",
                    sensor.SensorId,
                    sensor.LastSeenAt);
            }
        }

        var activeCount = sensors.Count(s => s.Status == SensorStatus.Active);
        var slotsToFill = _options.RequiredActiveSensors - activeCount;

        if (slotsToFill > 0)
        {
            var candidates = sensors
                .Where(s => s.Status == SensorStatus.Standby)
                .Where(s => !demotedForInactivity.Contains(s.SensorId))
                .Where(s => !s.BlockedUntil.HasValue || s.BlockedUntil.Value <= now)
                .Take(slotsToFill)
                .ToList();

            var promoted = 0;
            foreach (var candidate in candidates)
            {
                candidate.Status = SensorStatus.Active;
                promoted++;
                _logger.LogWarning(
                    "Sensor {SensorId} promoted to ACTIVE ({ActiveCount}/{Required} active)",
                    candidate.SensorId,
                    activeCount + promoted,
                    _options.RequiredActiveSensors);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> BlockSensorAsync(string sensorId, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.SensorRegistry
            .FirstOrDefaultAsync(s => s.SensorId == sensorId, cancellationToken);

        if (entry is null)
        {
            return false;
        }

        entry.BlockedUntil = DateTime.UtcNow.AddSeconds(_options.BlockDurationSeconds);
        entry.Status = SensorStatus.Standby;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Sensor {SensorId} blocked for {Seconds}s and set to STANDBY",
            sensorId,
            _options.BlockDurationSeconds);

        return true;
    }

    public async Task<SensorStatusDto?> GetStatusAsync(string sensorId, CancellationToken cancellationToken)
    {
        var entry = await _dbContext.SensorRegistry
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SensorId == sensorId, cancellationToken);

        return entry is null ? null : ToDto(entry);
    }

    public async Task<List<SensorStatusDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entries = await _dbContext.SensorRegistry
            .AsNoTracking()
            .OrderBy(s => s.SensorId)
            .ToListAsync(cancellationToken);

        return entries.Select(ToDto).ToList();
    }

    private static SensorStatusDto ToDto(SensorRegistry entry) => new()
    {
        SensorId = entry.SensorId,
        Status = entry.Status,
        LastSeenAt = entry.LastSeenAt,
        Quality = entry.Quality,
        RegisteredAt = entry.RegisteredAt,
        BlockedUntil = entry.BlockedUntil
    };
}
