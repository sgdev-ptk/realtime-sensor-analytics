namespace Api.Controllers;

using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/ack")]
public sealed class AlertsController(ILogger<AlertsController> logger, IRedisStore store) : ControllerBase
{
    // POST /api/ack/{alertId}
    [HttpPost("{alertId}")]
    public IActionResult Ack(string alertId)
    {
        // In a later task, we would mark the alert as acknowledged in Redis or a DB.
        // For now, just log and return 204 to satisfy contract.
        logger.LogInformation("Acknowledged alert {AlertId}", alertId);
        return NoContent();
    }
}
