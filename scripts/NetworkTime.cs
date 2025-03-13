using Godot;
using System;


/// PLANNING
/// A) Syncronize the current time to all clients, each client ticks up its own time
/// B) Syncronize an OFFSET to the scene load time to all clients 
/// C) Sync a specified sync time to all clients, each caluclates its networkTime as an offset from that sync time

public struct NetworkTimeSync
{
	public float currentNetworkTime;

	public float currentLocalTime;

	public NetworkTimeSync(float _currentNetworkTime, float _currentLocalTime)
	{
		currentNetworkTime = _currentNetworkTime;
		currentLocalTime = _currentLocalTime;
	}
}

// TODO : Track latency
// TODO : The network time sync should be activate by the lobby 

/// <summary>
/// Node responsible for tracking network time across clients / server
/// </summary>
public partial class NetworkTime : Node
{
	/*********************************************************************************************/
	/** Network Time */

	[Export]
	protected float syncFrequency = 1f;

	private float nextSyncTime = 0;

	private NetworkTimeSync latestNetworkTime = new NetworkTimeSync();

	// Copy of network time exported for debugging purposes.
	[Export]
	private float debug_NetworkTime;

	[Export]
	private Label debug_timeLabel;

	/*********************************************************************************************/
	/** Engine Methods */

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Multiplayer.HasMultiplayerPeer() && Multiplayer.IsServer() && GetLocalTime() > nextSyncTime)
		{
			SyncNetworkTime();
			nextSyncTime = 1f / syncFrequency;
		}
		ProcessDebugging();
	}

	/*********************************************************************************************/
	/** Server */

	protected void SyncNetworkTime()
	{
		Rpc(MethodName.RPC_SyncNetworkTime, GetLocalTime());
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RPC_SyncNetworkTime(float newNetworkTime)
	{
		latestNetworkTime = new NetworkTimeSync(newNetworkTime, GetLocalTime());
	}

	public float GetNetworkTime()
	{
		float offset = latestNetworkTime.currentNetworkTime - latestNetworkTime.currentLocalTime;
		return GetLocalTime() + offset;
	}

	public float GetLocalTime()
	{
		return (float)Time.GetTicksMsec() / 1000f;
	}

	/*********************************************************************************************/
	/** Debugging */

	private void ProcessDebugging()
	{
		debug_NetworkTime = GetNetworkTime();
		debug_timeLabel.Text = GetNetworkTime().ToString();
	}
}
