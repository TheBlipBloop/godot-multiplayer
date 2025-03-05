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
	protected int maxClients = 32;

	[Export]
	protected string password = "";

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
	/** Lobby */

	// Starts server -- no clients
	public virtual Error Host()
	{
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
		Error error = peer.CreateServer(port, maxClients);

		if (error != Error.Ok)
		{
			return error;
		}

		Multiplayer.MultiplayerPeer = peer;

		return Error.Ok;
	}

	public virtual Error Connect(string address)
	{
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
		Error error = peer.CreateClient(address, port);

		if (error != Error.Ok)
		{
			return error;
		}

		Multiplayer.MultiplayerPeer = peer;

		return Error.Ok;
	}

	public virtual void Disconnect()
	{
		Multiplayer.MultiplayerPeer.Close();
	}

	public virtual void SetPassword(string newPassword)
	{
		// Cannot change password while running!
		// if (Multiplayer.MultiplayerPeer)
		{
			// return;
		}
		GD.Print(newPassword);
		password = newPassword;
	}

	/*********************************************************************************************/
	/** Network Delegates */

	private void InitializeNetworkDelegates()
	{
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += OnConnectionSucceed;
		Multiplayer.ConnectionFailed += OnConnectionFail;
		Multiplayer.ServerDisconnected += OnDisconnect;
	}

	protected virtual void OnPeerConnected(long clientID)
	{
		// GD.Print("Client connected!");

		// Called on existing clients AND the server
		// When a client connects to our server, we want to send a message to the client to get it all
		// set up!
		if (Multiplayer.IsServer())
		{
			return;
		}

		GD.Print("Client (" + clientID.ToString() + ") connected!");
	}

	protected virtual void OnPeerDisconnected(long clientID)
	{
		if (Multiplayer.IsServer())
		{
			return;
		}

		GD.Print("Client (" + clientID.ToString() + ") disconnected!");
		// throw new NotImplementedException("TODO");
	}

	protected virtual void OnConnectionSucceed()
	{
		// Called on clients only!
		GD.Print("Client connection established, validating!");

		// Once we've established connection to server, authenticate this client!
		AuthenticateClient();
	}

	protected virtual void OnConnectionFail()
	{
		// Called on clients only!
		GD.Print("Client connection failed!");
		Multiplayer.MultiplayerPeer = null;
	}

	protected virtual void OnDisconnect()
	{
		// Called on clients only!
		Multiplayer.MultiplayerPeer = null;
	}


	/*********************************************************************************************/
	/** Authentication */

	protected uint GetAuthenticationHash()
	{
		return (version + password).Hash();
	}

	protected virtual void AuthenticateClient()
	{
		// Send authentication information to the server 

		int clientId = Multiplayer.GetUniqueId();
		uint clientAuthentication = GetAuthenticationHash();

		RpcId(1, MethodName.Command_AuthenticateNewClient, clientId, clientAuthentication);
	}

	/*********************************************************************************************/
	/** RPC */

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void Command_AuthenticateNewClient(int clientID, uint clientAuth)
	{
		uint expectedAuthHash = GetAuthenticationHash();
		bool clientAuthenticated = clientAuth == expectedAuthHash;

		GD.Print("Expected hash : " + expectedAuthHash.ToString());
		GD.Print("Client hash : " + clientAuth.ToString());


		if (clientAuthenticated)
		{
			GD.Print("Client (" + clientID.ToString() + ") validated!");
		}
		else
		{
			GD.Print("Client (" + clientID.ToString() + ") fucking SUCKS!");
			Multiplayer.MultiplayerPeer.DisconnectPeer(clientID, false);
		}
	}

	/*********************************************************************************************/
	/** Registrations */

	protected void RegisterClient()
	{

	}

}
