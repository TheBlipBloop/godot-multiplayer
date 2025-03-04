using Godot;
using System;
using System.Collections.Generic;

public partial class Lobby : Node
{
	/*********************************************************************************************/
	/** Globals */

	protected static Lobby instance;

	public static Lobby GetLobbyInstance()
	{
		return instance;
	}

	private void SetLobbyInstance(Lobby newLobbyInstance)
	{
		instance = newLobbyInstance;
	}

	/*********************************************************************************************/
	/** Lobby */

	// Mapping of unique ID's to client information.
	protected Dictionary<long, Client> clients = new Dictionary<long, Client>();

	// Default port to use for creating / joining lobbies.
	[Export]
	protected int port = 7777;

	// Max number of connections allowed in this lobby.
	[Export]
	protected int maxConnections = 32;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetLobbyInstance(this);
		InitializeNetworkDelegates();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	/*********************************************************************************************/
	/** Network Delegates */

	private void InitializeNetworkDelegates()
	{
		Multiplayer.PeerConnected += OnClientConnected;
		Multiplayer.PeerDisconnected += OnClientDisconnected;
		Multiplayer.ConnectedToServer += OnConnectionSucceed;
		Multiplayer.ConnectionFailed += OnConnectionFail;
		Multiplayer.ServerDisconnected += OnServerDisconnected;
	}

	protected virtual void OnClientConnected(long clientID)
	{
		// When a client connects to our server, we want to send a message to the client to get it all
		// set up!
		RpcId(clientID, MethodName.OnClientConnected);
	}

	protected virtual void OnClientDisconnected(long clientID)
	{
		throw new NotImplementedException("TODO");
	}

	protected virtual void OnConnectionSucceed()
	{
		throw new NotImplementedException("TODO");
	}

	protected virtual void OnConnectionFail()
	{
		throw new NotImplementedException("TODO");
	}

	protected virtual void OnServerDisconnected()
	{
		throw new NotImplementedException("TODO");
	}

	/*********************************************************************************************/
	/** RPC */

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RegisterClient(long clientID, string ip)
	{
		// int newPlayerId = Multiplayer.GetRemoteSenderId();
		// _players[newPlayerId] = newPlayerInfo;
		// EmitSignal(SignalName.PlayerConnected, newPlayerId, newPlayerInfo);
	}

}
