using Shared.Enums;

namespace Shared.Dtos;

public class SensorStatusDto
{
    public string SensorId { get; set; } = string.Empty;
    public SensorStatus Status { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DataQuality Quality { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? BlockedUntil { get; set; }
}
