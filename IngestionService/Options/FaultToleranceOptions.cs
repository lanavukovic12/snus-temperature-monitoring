namespace IngestionService.Options;

public class FaultToleranceOptions
{
    public const string SectionName = "FaultTolerance";

    public int RequiredActiveSensors { get; set; } = 5;
    public int InactivityTimeoutSeconds { get; set; } = 10;
    public int BlockDurationSeconds { get; set; } = 30;
    public int WatchdogIntervalSeconds { get; set; } = 2;
}
