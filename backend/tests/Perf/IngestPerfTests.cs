using Xunit;

namespace Perf;

public class IngestPerfTests
{
    [Fact(Skip = "Perf harness placeholder; requires running env and metrics integration")]
    public void Sustains_1k_per_sec_p95_under_250ms()
    {
        // TODO: Implement perf runner that pushes load into simulator settings,
        // observes end-to-end latency via metrics, and asserts budgets.
        Assert.True(true);
    }
}
