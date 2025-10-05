using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Integration;

public class ReconnectBackfillTests : IClassFixture<WebApplicationFactory<Api.Program>>
{
    private readonly WebApplicationFactory<Api.Program> _factory;

    public ReconnectBackfillTests(WebApplicationFactory<Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact(Skip = "Pending implementation: define reconnect/backfill API")]
    public async Task Reconnect_Returns_Last_5s()
    {
        // TODO: Arrange: simulate some data, disconnect, reconnect
        // Act: call backfill API or hub negotiation with since=now-5s
        // Assert: at least some points returned and timestamps within 5s window
    }
}
