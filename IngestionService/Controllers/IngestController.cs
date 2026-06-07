using Microsoft.AspNetCore.Mvc;
using Shared.Data;
using Shared.Dtos;
using Shared.Models;
using IngestionService.Services;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly SensorRegistryService _registryService;
    private readonly AlarmDetector _alarmDetector;
    private readonly ILogger<IngestController> _logger;

    public IngestController(
        AppDbContext dbContext,
        SensorRegistryService registryService,
        AlarmDetector alarmDetector,
        ILogger<IngestController> logger)
    {
        _dbContext = dbContext;
        _registryService = registryService;
        _alarmDetector = alarmDetector;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] IngestReadingRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SensorId))
        {
            return BadRequest("SensorId is required.");
        }

        if (request.AlarmPriority is < 0 or > 3)
        {
            return BadRequest("AlarmPriority must be between 0 and 3.");
        }

        if (!double.IsFinite(request.Value))
        {
            return BadRequest("Value must be a finite number.");
        }

        var sensorId = request.SensorId.Trim();
        var registryEntry = await _registryService.GetOrCreateAsync(sensorId, request.Quality, cancellationToken);

        if (!_registryService.CanIngest(registryEntry))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                _registryService.GetIngestRejectionReason(registryEntry));
        }

        var timestamp = request.Timestamp == default ? DateTime.UtcNow : request.Timestamp.ToUniversalTime();

        var reading = new SensorReading
        {
            SensorId = sensorId,
            Value = request.Value,
            Timestamp = timestamp,
            AlarmPriority = request.AlarmPriority,
            Quality = request.Quality,
            IsConsensus = false
        };

        registryEntry.LastSeenAt = timestamp;
        _dbContext.SensorReadings.Add(reading);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (reading.AlarmPriority == 0)
        {
            _logger.LogInformation(
                "Ingested reading from {SensorId}: value={Value}, quality={Quality}",
                reading.SensorId,
                reading.Value,
                reading.Quality);
        }

        await _alarmDetector.ProcessReadingAsync(
            sensorId,
            reading.Value,
            reading.AlarmPriority,
            timestamp,
            cancellationToken);

        return Ok();
    }
}
