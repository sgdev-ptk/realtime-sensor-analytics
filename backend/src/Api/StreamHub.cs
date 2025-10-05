using Microsoft.AspNetCore.SignalR;
using Prometheus;

namespace Api;

public class StreamHub : Hub
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
		// Clients call JoinSensor to receive data
		return base.OnConnectedAsync();
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		ConnectionsEnded.Inc();
		return base.OnDisconnectedAsync(exception);
	}

	public Task JoinSensor(string sensorId)
	{
		Joins.Inc();
		return this.Groups.AddToGroupAsync(this.Context.ConnectionId, GroupFor(sensorId));
	}

	public Task LeaveSensor(string sensorId)
	{
		Leaves.Inc();
		return this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, GroupFor(sensorId));
	}

	internal static string GroupFor(string sensorId) => $"sensor:{sensorId}";
}
