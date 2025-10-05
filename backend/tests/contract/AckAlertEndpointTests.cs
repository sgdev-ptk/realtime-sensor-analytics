using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Contract;

public class AckAlertEndpointTests : IClassFixture<WebApplicationFactory<Api.Program>>
{
    private readonly WebApplicationFactory<Api.Program> _factory;

    public AckAlertEndpointTests(WebApplicationFactory<Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ack_Returns_401_Without_ApiKey()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/ack/test-id", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Ack_Returns_204_With_Valid_ApiKey()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", "test-key");
        var response = await client.PostAsync("/api/ack/test-id", content: null);
        // Should be NotFound or 204 until implemented; assert 204 to fail now
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
