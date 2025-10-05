using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.Integration;

public class AnomalyAlertingTests : IClassFixture<WebApplicationFactory<Api.Program>>
{
    private readonly WebApplicationFactory<Api.Program> _factory;

    public AnomalyAlertingTests(WebApplicationFactory<Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact(Skip = "Pending implementation: anomaly detection and alerting")]
    public void Anomaly_Raises_Alert_Under_500ms()
    {
        // TODO: pump anomalous data and assert alert raised and delivered < 500ms
    }
}
