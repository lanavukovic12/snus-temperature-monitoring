using Shared.Models;

namespace ConsensusService;

public sealed record ConsensusCalculation(double Value, IReadOnlyCollection<string> MaliciousSensorIds);

public class ConsensusCalculator
{
    public ConsensusCalculation? Calculate(IReadOnlyCollection<SensorReading> readings, ConsensusOptions options)
    {
        var latestBySensor = readings
            .Where(r => !string.IsNullOrWhiteSpace(r.SensorId))
            .GroupBy(r => r.SensorId!)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .OrderBy(r => r.Value)
            .ToList();

        if (latestBySensor.Count < options.MinimumGoodSensors)
        {
            return null;
        }

        var median = Median(latestBySensor.Select(r => r.Value).ToList());
        var accepted = latestBySensor
            .Where(r => Math.Abs(r.Value - median) <= options.OutlierTolerance)
            .ToList();

        if (accepted.Count < options.MinimumGoodSensors)
        {
            return null;
        }

        var consensusValue = Math.Round(accepted.Average(r => r.Value), 2);
        var malicious = latestBySensor
            .Where(r => Math.Abs(r.Value - median) > options.OutlierTolerance)
            .Select(r => r.SensorId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ConsensusCalculation(consensusValue, malicious);
    }

    private static double Median(IReadOnlyList<double> sortedValues)
    {
        var midpoint = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 1
            ? sortedValues[midpoint]
            : (sortedValues[midpoint - 1] + sortedValues[midpoint]) / 2;
    }
}
