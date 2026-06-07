namespace Shared.Models;

public class AlarmLog
{
    public long Id { get; set; }
    public string SensorId { get; set; } = string.Empty;
    public double Value { get; set; }
    public int AlarmPriority { get; set; }
    public DateTime Timestamp { get; set; }
}
