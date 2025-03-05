using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	protected Dictionary<int, Client> clients = new Dictionary<int, Client>();

	// Version string used to validate connections from incoming clients.
	[Export]
	protected string version = "0.0.0";

	// Default port to use for creating / joining lobbies.
	[Export]
	protected int port = 7777;

	// Max number of connections allowed in this lobby.
	[Export]
	protected int maxClients = 32;

	// Password used when hosting / connecting to servers.
	[Export]
	protected string password = "";

	[Export]
	protected Godot.Collections.Dictionary<int, Client> debug_clients = new Godot.Collections.Dictionary<int, Client>();


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetLobbyInstance(this);
		InitializeNetworkDelegates();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		debug_clients.Clear();
		foreach (var item in clients.Keys)
		{
			debug_clients.Add(item, clients[item]);
		}
	}

	/*********************************************************************************************/
	/** Lobby */

	// Starts server -- no clients
	public virtual Error Host(string bindIP)
	{
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
		Error error = peer.CreateServer(port, maxClients);
		peer.SetBindIP(bindIP);

		if (error != Error.Ok)
		{
			return error;
		}

		Multiplayer.MultiplayerPeer = peer;
		GD.Print(String.Format("Hosting server @ {0}:{1}.", bindIP, port));

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

		GD.Print(String.Format("Connected to server @ {0}:{1}.", address, port));

		return Error.Ok;
	}

	public virtual void Disconnect()
	{
		Multiplayer.MultiplayerPeer.Close();
	}

	public virtual void SetPassword(string newPassword)
	{
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
		// Called on existing clients AND the server
		// When a client connects to our server, we want to send a message to the client to get it all
		// set up!
		if (Multiplayer.IsServer())
		{
			return;
		}

		GD.Print(String.Format("Server: Client connected ({0}).", clientID));
	}

	protected virtual void OnPeerDisconnected(long clientID)
	{
		if (Multiplayer.IsServer())
		{
			return;
		}

		UnregisterClient((int)clientID);
		GD.Print(String.Format("Server: Client disconnected ({0}).", clientID));
	}

	protected virtual void OnConnectionSucceed()
	{
		// Called on clients only!

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

		int clientID = Multiplayer.GetUniqueId();
		uint clientAuthentication = GetAuthenticationHash();

		RpcId(1, MethodName.Command_AuthenticateNewClient, clientID, clientAuthentication);
	}

	/*********************************************************************************************/
	/** RPC */

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void Command_AuthenticateNewClient(int clientID, uint clientAuth)
	{
		uint expectedAuthHash = GetAuthenticationHash();
		bool clientAuthenticated = clientAuth == expectedAuthHash;

		if (clientAuthenticated)
		{
			GD.Print(String.Format("Server: Validated client : ({0}).", clientID));
			RegisterClient(clientID);
		}
		else
		{
			GD.Print(String.Format("Server: Failed to validate client : ({0}). Disconnecting from server.", clientID));
			Multiplayer.MultiplayerPeer.DisconnectPeer(clientID, false);
		}
	}

	/*********************************************************************************************/
	/** Registrations */

	protected void RegisterClient(int clientID)
	{
		if (clients.ContainsKey(clientID))
		{
			throw new Exception("Attempted to add client already in the client list. Client ID : " + clientID.ToString());
		}

		Client newClient = new Client(clientID);
		clients.Add(clientID, newClient);
	}

	protected void UnregisterClient(int clientID)
	{
		if (!clients.ContainsKey(clientID))
		{
			throw new Exception("Attempted to remove client that is not in the client list. Client ID : " + clientID.ToString());
		}

		clients.Remove(clientID);
	}

}
