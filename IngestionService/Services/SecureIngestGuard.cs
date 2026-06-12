using Microsoft.Extensions.Options;
using Shared.Dtos;
using Shared.Security;

namespace IngestionService.Services;

public class SecureIngestGuard
{
    private readonly SecureMessagingOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SecureIngestGuard> _logger;
    private readonly object _lock = new();
    private readonly Dictionary<string, SensorTrafficState> _traffic = new(StringComparer.OrdinalIgnoreCase);

    public SecureIngestGuard(
        IOptions<SecureMessagingOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<SecureIngestGuard> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<GuardResult> ValidateAsync(SecureIngestRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SensorId))
        {
            return GuardResult.Rejected("SensorId is required.");
        }

        var now = DateTime.UtcNow;
        var sentAt = request.SentAt.ToUniversalTime();

        if (Math.Abs((now - sentAt).TotalSeconds) > _options.MaxClockSkewSeconds)
        {
            return GuardResult.Rejected("Message timestamp is outside allowed clock skew.");
        }

        bool shouldBlock;
        string? replayReason = null;

        lock (_lock)
        {
            var state = GetState(request.SensorId);

            if (request.MessageId <= state.LastAcceptedMessageId)
            {
                replayReason = "Replay detected: message id is not greater than the last accepted id.";
            }
            else
            {
                state.RecentMessages.Enqueue(now);
                while (state.RecentMessages.Count > 0 &&
                       (now - state.RecentMessages.Peek()).TotalSeconds > 1)
                {
                    state.RecentMessages.Dequeue();
                }
            }

            shouldBlock = state.RecentMessages.Count > 10;
        }

        if (replayReason is not null)
        {
            _logger.LogWarning("Rejected replay from {SensorId}: messageId={MessageId}", request.SensorId, request.MessageId);
            return GuardResult.Rejected(replayReason);
        }

        if (shouldBlock)
        {
            using var scope = _scopeFactory.CreateScope();
            var registryService = scope.ServiceProvider.GetRequiredService<SensorRegistryService>();
            await registryService.BlockSensorAsync(request.SensorId, cancellationToken);
            return GuardResult.Rejected("Rate limit exceeded: more than 10 messages per second.");
        }

        if (!SecureMessageCrypto.TryUnprotect(request, _options, out var payload, out var error) || payload is null)
        {
            return GuardResult.Rejected(error);
        }

        lock (_lock)
        {
            var state = GetState(request.SensorId);
            state.LastAcceptedMessageId = request.MessageId;
        }

        return GuardResult.Accepted(payload);
    }

    private SensorTrafficState GetState(string sensorId)
    {
        if (!_traffic.TryGetValue(sensorId, out var state))
        {
            state = new SensorTrafficState();
            _traffic[sensorId] = state;
        }

        return state;
    }

    private sealed class SensorTrafficState
    {
        public long LastAcceptedMessageId { get; set; }
        public Queue<DateTime> RecentMessages { get; } = new();
    }
}

public sealed record GuardResult(bool IsAccepted, IngestReadingRequest? Payload, string? Error)
{
    public static GuardResult Accepted(IngestReadingRequest payload) => new(true, payload, null);
    public static GuardResult Rejected(string error) => new(false, null, error);
}
