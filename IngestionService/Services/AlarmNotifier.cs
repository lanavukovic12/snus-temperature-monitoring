using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Shared.Dtos;
using IngestionService.Options;

namespace IngestionService.Services;

public class AlarmNotifier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly NotificationOptions _options;
    private readonly ILogger<AlarmNotifier> _logger;

    public AlarmNotifier(
        HttpClient httpClient,
        IOptions<NotificationOptions> options,
        ILogger<AlarmNotifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyAsync(AlarmNotificationDto alarm, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var url = $"{_options.BaseUrl.TrimEnd('/')}/api/notify";

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(url, alarm, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to forward alarm for {SensorId} to NotificationService ({StatusCode})",
                    alarm.SensorId,
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(
                ex,
                "NotificationService unreachable at {Url} — alarm for {SensorId} not forwarded",
                url,
                alarm.SensorId);
        }
    }
}
