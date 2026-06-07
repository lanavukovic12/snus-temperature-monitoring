using Shared.Enums;

namespace SensorSimulator;

public class SensorConfig
{
    public string SensorId { get; set; } = string.Empty;
    public double MinTemperature { get; set; }
    public double MaxTemperature { get; set; }
    public double ThresholdPriority1 { get; set; }
    public double ThresholdPriority2 { get; set; }
    public double ThresholdPriority3 { get; set; }
    public DataQuality Quality { get; set; } = DataQuality.Good;
    public MaliciousMode MaliciousMode { get; set; } = MaliciousMode.None;
}
