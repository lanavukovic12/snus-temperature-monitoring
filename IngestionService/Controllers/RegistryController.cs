using Microsoft.AspNetCore.Mvc;
using IngestionService.Services;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistryController : ControllerBase
{
    private readonly SensorRegistryService _registryService;

    public RegistryController(SensorRegistryService registryService)
    {
        _registryService = registryService;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll(CancellationToken cancellationToken)
    {
        var sensors = await _registryService.GetAllAsync(cancellationToken);
        return Ok(sensors);
    }

    [HttpGet("{sensorId}")]
    public async Task<ActionResult> GetStatus(string sensorId, CancellationToken cancellationToken)
    {
        var status = await _registryService.GetStatusAsync(sensorId, cancellationToken);

        if (status is null)
        {
            return NotFound($"Sensor {sensorId} is not registered.");
        }

        return Ok(status);
    }

    [HttpPost("{sensorId}/block")]
    public async Task<ActionResult> Block(string sensorId, CancellationToken cancellationToken)
    {
        var blocked = await _registryService.BlockSensorAsync(sensorId, cancellationToken);

        if (!blocked)
        {
            return NotFound($"Sensor {sensorId} is not registered.");
        }

        return Ok(new { message = $"Sensor {sensorId} blocked for testing." });
    }
}
