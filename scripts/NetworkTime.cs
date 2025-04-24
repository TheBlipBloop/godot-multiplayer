using Godot;
using System;

// TODO : Investigate the effect of join time (and by extension relative placement of pings and time syncs)
// TODO : Smoothed pings (k running avg maybe temporially weighted)
// TODO : Modular architecture for the NetworkManager class? I feel this should not be its own node
// TODO : Documentation
// TODO : ? It could be interesting to use integer milliseconds for our running averages -- would be more stable than fpo

/// <summary>
/// Node responsible for tracking network time across clients / server
/// </summary>
public partial class NetworkTime : Node
{
	/*********************************************************************************************/
	/** Network Time */

	[Export]
	protected float syncFrequency = 5f;

	private float nextSyncTime = 0;

	private float localToServerTimeOffset;

	// Round trip time from client to server in seconds
	protected float ping;

	protected float smoothedPing;

	protected float cumPing;

	private int sumPingMS;

	// private int smoothedPingSamples;

	private float localPingTime;

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
		if (Multiplayer.HasMultiplayerPeer() && !Multiplayer.IsServer() && GetLocalTime() > nextSyncTime)
		{
			localPingTime = GetLocalTime();
			RpcId(1, MethodName.Command_Ping);
			nextSyncTime = GetLocalTime() + 1f / syncFrequency;
		}

		if (Multiplayer.HasMultiplayerPeer() && Multiplayer.IsServer() && GetLocalTime() > nextSyncTime)
		{
			GD.Print("Syncing time from server");
			SyncNetworkTime();
			nextSyncTime = GetLocalTime() + 1f / syncFrequency;
		}

		ProcessDebugging();
	}

	/*********************************************************************************************/
	/** Server */

	protected void SyncNetworkTime()
	{
		float a = GetLocalTime();
		Rpc(MethodName.RPC_SyncNetworkTime, a);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
	private void RPC_SyncNetworkTime(float newNetworkTime)
	{
		localToServerTimeOffset = newNetworkTime - GetLocalTime();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
	private void RPC_Pong()
	{
		ping = GetLocalTime() - localPingTime;
		cumPing += ping;
		smoothedPingSamples++;
		
		smoothedPing = cumPing / smoothedPingSamples;

		float pingMs = 1000f * ping;

		GD.Print("P:" + pingMs.ToString());
		GD.Print("S:" + (1000f * smoothedPing).ToString());
	}

	public float GetNetworkTime()
	{
		float delayFromServer = smoothedPing / 2f;
		return GetLocalTime() + localToServerTimeOffset + delayFromServer;
	}

	public float GetLocalTime()
	{
		return (float)Time.GetTicksMsec() / 1000f;
	}

	/*********************************************************************************************/
	/** Client */

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
	private void Command_Ping()
	{
		GD.Print("Ping");
		int clientId = Multiplayer.GetRemoteSenderId();
		RpcId(clientId, MethodName.RPC_Pong);
	}
	

	/*********************************************************************************************/
	/** Debugging */

	private void ProcessDebugging()
	{
		debug_NetworkTime = GetNetworkTime();
		debug_timeLabel.Text = GetNetworkTime().ToString();
	}
}
