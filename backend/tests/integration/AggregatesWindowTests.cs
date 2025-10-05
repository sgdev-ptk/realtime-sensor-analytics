using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Integration;

public class AggregatesWindowTests : IClassFixture<WebApplicationFactory<Api.Program>>
{
    private readonly WebApplicationFactory<Api.Program> _factory;

    public AggregatesWindowTests(WebApplicationFactory<Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact(Skip = "Pending implementation: metrics/aggregates query endpoints")]
    public void Aggregates_Computed_For_Windows()
    {
        // TODO: simulate stream, then query aggregates for windows {1m,5m,15m,1h,24h} and assert structure
    }
}
