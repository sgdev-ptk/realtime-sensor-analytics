using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Api;

public class StreamHub(ILogger<StreamHub> logger) : Hub
{
	private static readonly Counter ConnectionsStarted = Metrics.CreateCounter(
		"streamhub_connections_total", "Number of SignalR connections started");
	private static readonly Counter ConnectionsEnded = Metrics.CreateCounter(
		"streamhub_disconnections_total", "Number of SignalR connections ended");
	private static readonly Counter Joins = Metrics.CreateCounter(
		"streamhub_group_joins_total", "Number of sensor group joins");
	private static readonly Counter Leaves = Metrics.CreateCounter(
		"streamhub_group_leaves_total", "Number of sensor group leaves");

	public override Task OnConnectedAsync()
	{
		ConnectionsStarted.Inc();
		logger.LogInformation("SignalR connected connectionId={ConnectionId}", this.Context.ConnectionId);
		// Clients call JoinSensor to receive data
		return base.OnConnectedAsync();
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		ConnectionsEnded.Inc();
		logger.LogInformation("SignalR disconnected connectionId={ConnectionId}", this.Context.ConnectionId);
		return base.OnDisconnectedAsync(exception);
	}

	public Task JoinSensor(string sensorId)
	{
		Joins.Inc();
		logger.LogDebug("Join sensor group sensor={SensorId} connectionId={ConnectionId}", sensorId, this.Context.ConnectionId);
		return this.Groups.AddToGroupAsync(this.Context.ConnectionId, GroupFor(sensorId));
	}

	public Task LeaveSensor(string sensorId)
	{
		Leaves.Inc();
		logger.LogDebug("Leave sensor group sensor={SensorId} connectionId={ConnectionId}", sensorId, this.Context.ConnectionId);
		return this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, GroupFor(sensorId));
	}

	internal static string GroupFor(string sensorId) => $"sensor:{sensorId}";
}
