using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Dtos;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private const int MaxResults = 500;

    private readonly AppDbContext _dbContext;

    public ReportsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("readings")]
    public async Task<ActionResult<IReadOnlyList<ReadingReportDto>>> GetReadings(
        [FromQuery] string? sensorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_dbContext.SensorReadings.AsNoTracking().Where(r => !r.IsConsensus), sensorId, from, to);

        var results = await query
            .OrderByDescending(r => r.Timestamp)
            .Take(MaxResults)
            .Select(r => new ReadingReportDto
            {
                Id = r.Id,
                SensorId = r.SensorId ?? string.Empty,
                Value = r.Value,
                Timestamp = r.Timestamp,
                AlarmPriority = r.AlarmPriority,
                Quality = r.Quality
            })
            .ToListAsync(cancellationToken);

        return Ok(results);
    }

    [HttpGet("consensus")]
    public async Task<ActionResult<IReadOnlyList<ReadingReportDto>>> GetConsensus(
        [FromQuery] string? sensorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_dbContext.SensorReadings.AsNoTracking().Where(r => r.IsConsensus), sensorId, from, to);

        var results = await query
            .OrderByDescending(r => r.Timestamp)
            .Take(MaxResults)
            .Select(r => new ReadingReportDto
            {
                Id = r.Id,
                SensorId = r.SensorId ?? string.Empty,
                Value = r.Value,
                Timestamp = r.Timestamp,
                AlarmPriority = r.AlarmPriority,
                Quality = r.Quality
            })
            .ToListAsync(cancellationToken);

        return Ok(results);
    }

    private static IQueryable<Shared.Models.SensorReading> ApplyFilters(
        IQueryable<Shared.Models.SensorReading> query,
        string? sensorId,
        DateTime? from,
        DateTime? to)
    {
        if (!string.IsNullOrWhiteSpace(sensorId))
        {
            var id = sensorId.Trim();
            query = query.Where(r => r.SensorId == id);
        }

        if (from.HasValue)
        {
            var fromUtc = from.Value.ToUniversalTime();
            query = query.Where(r => r.Timestamp >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = to.Value.ToUniversalTime();
            query = query.Where(r => r.Timestamp <= toUtc);
        }

        return query;
    }
}
