namespace IngestionService.Options;

public class SensorThresholdOptions
{
    public string SensorId { get; set; } = string.Empty;
    public double ThresholdPriority1 { get; set; }
    public double ThresholdPriority2 { get; set; }
    public double ThresholdPriority3 { get; set; }
}
