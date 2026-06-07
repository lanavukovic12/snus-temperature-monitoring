using Shared.Enums;

namespace Shared.Models;

public class SensorReading
{
    public long Id { get; set; }
    public string? SensorId { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public int AlarmPriority { get; set; }
    public DataQuality Quality { get; set; } = DataQuality.Good;
    public bool IsConsensus { get; set; }
}
