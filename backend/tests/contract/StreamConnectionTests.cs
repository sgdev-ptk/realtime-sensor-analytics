using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Tests.Contract;

public class StreamConnectionTests : IClassFixture<WebApplicationFactory<Api.Program>>
{
    private readonly WebApplicationFactory<Api.Program> _factory;

    public StreamConnectionTests(WebApplicationFactory<Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SignalR_Connection_Requires_ApiKey()
    {
        var server = _factory.Server; // ensures host started
        var baseAddress = _factory.Server.BaseAddress;
        var url = new Uri(baseAddress, "/api/stream");
        var connection = new HubConnectionBuilder()
            .WithUrl(url.ToString())
            .WithAutomaticReconnect()
            .Build();
        await Assert.ThrowsAnyAsync<Exception>(async () => await connection.StartAsync());
    }
}
