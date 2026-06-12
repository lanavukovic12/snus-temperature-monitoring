using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Dtos;
using Shared.Enums;
using Shared.Security;

namespace SensorSimulator;

public class SensorWorker
{
    private static readonly object ConsoleLock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SensorConfig _config;
    private readonly HttpClient _httpClient;
    private readonly string _ingestUrl;
    private readonly string _statusUrl;
    private readonly SecureMessagingOptions _secureMessagingOptions;
    private readonly Random _random = new();
    private SensorStatus? _lastKnownStatus;
    private long _messageId;

    public SensorWorker(
        SensorConfig config,
        HttpClient httpClient,
        string ingestUrl,
        string statusUrl,
        SecureMessagingOptions secureMessagingOptions)
    {
        _config = config;
        _httpClient = httpClient;
        _ingestUrl = ingestUrl;
        _statusUrl = statusUrl;
        _secureMessagingOptions = secureMessagingOptions;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Log($"started (quality={_config.Quality}, malicious={_config.MaliciousMode})");

        while (!cancellationToken.IsCancellationRequested)
        {
            var intervalSeconds = _random.Next(1, 11);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken);
                await SendReadingAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"error: {ex.Message}");
            }
        }

        Log("stopped");
    }

    private async Task SendReadingAsync(CancellationToken cancellationToken)
    {
        var status = await GetStatusAsync(cancellationToken);

        if (status is null)
        {
            Log("not registered yet, attempting first contact");
        }
        else if (status.Status != SensorStatus.Active)
        {
            if (_lastKnownStatus != status.Status)
            {
                Log($"STANDBY — skipping send (status={status.Status})");
                _lastKnownStatus = status.Status;
            }

            return;
        }
        else if (_lastKnownStatus != SensorStatus.Active)
        {
            Log("promoted to ACTIVE — resuming sends");
            _lastKnownStatus = SensorStatus.Active;
        }

        if (_config.MaliciousMode == MaliciousMode.DelayedResponses)
        {
            var delaySeconds = _random.Next(5, 16);
            Log($"malicious delay: waiting {delaySeconds}s before send");
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
        }

        var value = GenerateTemperature();
        var alarmPriority = Shared.AlarmEvaluator.Evaluate(
            value,
            _config.ThresholdPriority1,
            _config.ThresholdPriority2,
            _config.ThresholdPriority3);
        var timestamp = DateTime.UtcNow;

        PrintReading(value, timestamp, alarmPriority);

        var request = new IngestReadingRequest
        {
            SensorId = _config.SensorId,
            Value = value,
            Timestamp = timestamp,
            AlarmPriority = alarmPriority,
            Quality = _config.Quality
        };

        if (_config.MaliciousMode == MaliciousMode.MessageFlooding)
        {
            for (var i = 0; i < 12; i++)
            {
                await PostReadingAsync(request, cancellationToken);
            }

            return;
        }

        await PostReadingAsync(request, cancellationToken);
    }

    private async Task<SensorStatusDto?> GetStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SensorStatusDto>(
                $"{_statusUrl}/{_config.SensorId}",
                JsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private double GenerateTemperature()
    {
        var value = _config.MinTemperature +
                    (_random.NextDouble() * (_config.MaxTemperature - _config.MinTemperature));

        if (_config.MaliciousMode != MaliciousMode.CorruptValues)
        {
            return Math.Round(value, 2);
        }

        var corrupt = _random.Next(3) switch
        {
            0 => value + _random.Next(50, 150),
            1 => -_random.Next(10, 100),
            _ => 0
        };

        return Math.Round(corrupt, 2);
    }

    private async Task PostReadingAsync(IngestReadingRequest request, CancellationToken cancellationToken)
    {
        using var response = _secureMessagingOptions.Enabled
            ? await _httpClient.PostAsJsonAsync(
                _ingestUrl,
                SecureMessageCrypto.Protect(request, Interlocked.Increment(ref _messageId), _secureMessagingOptions),
                JsonOptions,
                cancellationToken)
            : await _httpClient.PostAsJsonAsync(
                _ingestUrl,
                request,
                JsonOptions,
                cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Log($"POST rejected (403): {body.Trim('"')}");
            _lastKnownStatus = SensorStatus.Standby;
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Log($"POST failed ({(int)response.StatusCode}): {body}");
        }
    }

    private void PrintReading(double value, DateTime timestamp, int alarmPriority)
    {
        var message = $"[{timestamp:O}] {_config.SensorId} temp={value:F2}°C alarm={alarmPriority}";

        lock (ConsoleLock)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = alarmPriority switch
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

    private void Log(string message)
    {
        lock (ConsoleLock)
        {
            Console.WriteLine($"[{_config.SensorId}] {message}");
        }
    }
}
