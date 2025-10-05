namespace Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Prometheus;

[ApiController]
[Route("api/metrics")]
public sealed class MetricsController : ControllerBase
{
    private static readonly Counter ContractsOk = Metrics.CreateCounter(
        "contracts_metrics_endpoint_ok_total", "Number of times /api/metrics was called successfully");

    // GET /api/metrics -> 200 OK with minimal body per contract tests; Prometheus scrape is at /metrics
    [HttpGet]
    public IActionResult Get()
    {
        ContractsOk.Inc();
        return Content("ok", "text/plain");
    }
}
