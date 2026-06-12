namespace ConsensusService;

public class ConsensusOptions
{
    public const string SectionName = "Consensus";

    public int IntervalSeconds { get; set; } = 60;
    public int WindowSeconds { get; set; } = 60;
    public double OutlierTolerance { get; set; } = 15;
    public int MinimumGoodSensors { get; set; } = 3;
}
