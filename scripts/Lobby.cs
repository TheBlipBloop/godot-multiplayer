using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

	// Version string used to validate connections from incoming clients.
	[Export]
	protected string version = "0.0.0";

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
		// Called on existing clients AND the server
		// When a client connects to our server, we want to send a message to the client to get it all
		// set up!
		if (Multiplayer.IsServer())
		{
			return;
		}

		// Server asks client to 
		// RpcId(clientID, MethodName.ClientRPC_OnClientConnected);
	}

	protected virtual void OnClientDisconnected(long clientID)
	{
		throw new NotImplementedException("TODO");
	}

	protected virtual void OnConnectionSucceed()
	{
		// Once we've established connection to server, authenticate this client!
		AuthenticateClient();
	}

	protected virtual void OnConnectionFail()
	{
		// Called on clients only!

		throw new NotImplementedException("TODO");
	}

	protected virtual void OnServerDisconnected()
	{
		// Called on clients only!
		throw new NotImplementedException("TODO");
	}


	/*********************************************************************************************/
	/** Authentication */

	protected uint GetAuthenticationHash()
	{
		return version.Hash();
	}

	protected virtual void AuthenticateClient()
	{
		// Send authentication information to the server 

		uint clientAuthentication = GetAuthenticationHash();

		RpcId(1, MethodName.Command_AuthenticateNewClient, clientAuthentication);
	}

	/*********************************************************************************************/
	/** RPC */


	// RPC from server to clients asking clients to begin authentication
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ClientRPC_OnClientConnected(long clientID, string ip)
	{
		uint clientAuthHash = GetAuthenticationHash();

		// TODO : Fetch password and combine with the clientAuthHash here. (Or have as part of GetAuthHash())?

		// This might not be the correct ID, validate!
		int clientId = Multiplayer.GetRemoteSenderId();

		RpcId(0, MethodName.Command_AuthenticateNewClient, clientAuthHash);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void Command_AuthenticateNewClient(int clientID, uint clientAuth)
	{
		uint expectedAuthHash = GetAuthenticationHash();
		bool clientAuthenticated = clientAuth == expectedAuthHash;

		if (clientAuthenticated)
		{

		}
		else
		{
			// bing bong fuck your life
			Multiplayer.MultiplayerPeer.DisconnectPeer(clientID, false);
		}

	}


}
