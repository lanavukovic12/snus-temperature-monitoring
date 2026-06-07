using Shared.Enums;

namespace Shared.Dtos;

public class IngestReadingRequest
{
    public string SensorId { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public int AlarmPriority { get; set; }
    public DataQuality Quality { get; set; } = DataQuality.Good;
}
