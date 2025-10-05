using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Contract;

public class MetricsEndpointTests : IClassFixture<WebApplicationFactory<Api.Program>>
{
    private readonly WebApplicationFactory<Api.Program> _factory;

    public MetricsEndpointTests(WebApplicationFactory<Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Metrics_Endpoint_Returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/metrics");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 OK (or 404 until implemented), got {(int)response.StatusCode}");
        // The first run can be NotFound since endpoint not yet implemented; this test should fail until implemented
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
