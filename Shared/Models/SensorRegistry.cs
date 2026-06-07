using Shared.Enums;

namespace Shared.Models;

public class SensorRegistry
{
    public string SensorId { get; set; } = string.Empty;
    public SensorStatus Status { get; set; } = SensorStatus.Standby;
    public DateTime? LastSeenAt { get; set; }
    public DataQuality Quality { get; set; } = DataQuality.Good;
    public DateTime RegisteredAt { get; set; }
    public DateTime? BlockedUntil { get; set; }
}
