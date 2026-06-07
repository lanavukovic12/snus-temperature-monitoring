namespace IngestionService.Services;

public static class AlarmConsoleWriter
{
    private static readonly object ConsoleLock = new();

    public static void WriteAlarm(string sensorId, double value, int priority, DateTime timestamp)
    {
        var message = $"[ALARM P{priority}] [{timestamp:O}] {sensorId} value={value:F2}";

        lock (ConsoleLock)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = priority switch
            {
                1 => ConsoleColor.Yellow,
                2 => ConsoleColor.DarkYellow,
                3 => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };
            Console.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }
    }
}
