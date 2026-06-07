using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using Shared.Dtos;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotifyController : ControllerBase
{
    private readonly IHubContext<AlarmHub> _hubContext;
    private readonly ILogger<NotifyController> _logger;

    public NotifyController(IHubContext<AlarmHub> hubContext, ILogger<NotifyController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Notify([FromBody] AlarmNotificationDto? alarm)
    {
        if (alarm is null)
        {
            return BadRequest("Alarm payload is required.");
        }

        _logger.LogWarning(
            "Alarm received: {SensorId} value={Value} priority={Priority}",
            alarm.SensorId,
            alarm.Value,
            alarm.AlarmPriority);

        await _hubContext.Clients.All.SendAsync("AlarmReceived", alarm);

        return Ok();
    }
}
