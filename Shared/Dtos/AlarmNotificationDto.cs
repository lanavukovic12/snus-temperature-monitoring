namespace Shared.Dtos;

public class AlarmNotificationDto
{
    public string SensorId { get; set; } = string.Empty;
    public double Value { get; set; }
    public int AlarmPriority { get; set; }
    public DateTime Timestamp { get; set; }
}
