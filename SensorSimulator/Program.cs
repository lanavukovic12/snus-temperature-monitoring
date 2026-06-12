using Microsoft.Extensions.Configuration;
using SensorSimulator;
using Shared.Security;

var configFile = ResolveConfigFile();
var configPath = Path.Combine(AppContext.BaseDirectory, configFile);

if (!File.Exists(configPath))
{
    Console.WriteLine($"Config file not found: {configPath}");
    Console.WriteLine("Set SENSOR_CONFIG to machine-a, machine-b, or a full .json filename.");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile(configFile, optional: false)
    .AddEnvironmentVariables()
    .Build();

var baseUrl = configuration["INGESTION_BASE_URL"]
    ?? configuration["IngestionService:BaseUrl"]
    ?? "http://localhost:5055";

var secureMessaging = configuration
    .GetSection(SecureMessagingOptions.SectionName)
    .Get<SecureMessagingOptions>() ?? new SecureMessagingOptions { Enabled = false };

var serviceBase = baseUrl.TrimEnd('/');
var ingestUrl = secureMessaging.Enabled
    ? $"{serviceBase}/api/ingest/secure"
    : $"{serviceBase}/api/ingest";
var statusUrl = $"{serviceBase}/api/registry";

var sensors = configuration.GetSection("Sensors").Get<List<SensorConfig>>() ?? [];

if (sensors.Count == 0)
{
    Console.WriteLine($"No sensors configured in {configFile}.");
    return 1;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("Temperature sensor simulator");
Console.WriteLine($"Config: {configFile}");
Console.WriteLine($"Posting to {ingestUrl}");
Console.WriteLine($"Secure messaging: {(secureMessaging.Enabled ? "enabled" : "disabled")}");
Console.WriteLine($"Sensors: {string.Join(", ", sensors.Select(s => s.SensorId))}");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

var workers = sensors
    .Select(config => new SensorWorker(config, httpClient, ingestUrl, statusUrl, secureMessaging))
    .Select(worker => worker.RunAsync(cts.Token))
    .ToArray();

await Task.WhenAll(workers);
Console.WriteLine("All sensors stopped.");
return 0;

static string ResolveConfigFile()
{
    var sensorConfig = Environment.GetEnvironmentVariable("SENSOR_CONFIG");

    if (string.IsNullOrWhiteSpace(sensorConfig))
    {
        return "appsettings.json";
    }

    return sensorConfig.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
        ? sensorConfig
        : $"appsettings.{sensorConfig}.json";
}
