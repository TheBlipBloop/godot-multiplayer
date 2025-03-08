using Godot;
using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Represents a multiplayer lobby.
/// Handles hosting, client connection, authentication, and spawning player nodes.
/// </summary> 
public partial class Lobby : Node
{
	/*********************************************************************************************/
	/** Singlton */

	// Lobby singleton.
	private static Lobby instance;

	/// <summary>
	/// Gets the current lobby singleton.
	/// </summary>
	/// <returns>The current lobby singleton.</returns>
	public static Lobby GetLobbyInstance()
	{
		return instance;
	}

	/// <summary>
	/// Updates the current lobby singleton to @newLobbyInstance.
	/// </summary>
	/// <param name="newLobbyInstance"></param>
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

	// Time in seconds that server must receive authentication info from clients before kicking.
	protected float maxAuthenticationTime = 1.0f;

	// Player scene. Every client has one spawned player scene that they control.
	[Export]
	protected PackedScene playerScene;

	// TODO : Maybe use this? It seems like we can just do this manually and then users only need to configure one node 
	[Export]
	protected MultiplayerSpawner playerSpawner;

	/*********************************************************************************************/
	/** Egnine Methods */

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Multiplayer.MultiplayerPeer = null;
		// ((SceneMultiplayer)MultiplayerApi).ServerRelay

		InitializeNetworkDelegates();
		SetLobbyInstance(this);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		ProcessDebugging();
	}

	/*********************************************************************************************/
	/** Debugging - Data */

	[Export]
	protected Label clientListDebugLabel;

	[Export]
	protected Godot.Collections.Dictionary<int, Client> debug_clients = new Godot.Collections.Dictionary<int, Client>();

	/*********************************************************************************************/
	/** Debugging - Functions */

	protected void ProcessDebugging()
	{
		debug_clients.Clear();
		foreach (var item in clients.Keys)
		{
			debug_clients.Add(item, clients[item]);
		}

		clientListDebugLabel.Text = GetClientListString();
	}

	public string GetClientListString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		int clientIndex = 0;
		foreach (var item in clients.Keys)
		{
			stringBuilder.Append(String.Format("{0}.\tClient : {1}\t({2})\n", clientIndex, item, clients[item].DEBUG_GetLifetime()));
			clientIndex++;
		}

		return stringBuilder.ToString();
	}

	/*********************************************************************************************/
	/** Lobby */

	// Starts server
	public virtual Error Host(string bindIP)
	{
		if (Multiplayer.MultiplayerPeer != null)
		{
			return Error.AlreadyExists;
		}

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
		if (Multiplayer.MultiplayerPeer != null)
		{
			return Error.AlreadyExists;
		}

		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
		Error error = peer.CreateClient(address, port);

		if (error != Error.Ok)
		{
			return error;
		}

		Multiplayer.MultiplayerPeer = peer;

		GD.Print(String.Format("Connecting to server @ {0}:{1}.", address, port));

		return Error.Ok;
	}

	public virtual Error Disconnect()
	{
		if (Multiplayer.MultiplayerPeer == null)
		{
			return Error.DoesNotExist;
		}

		Multiplayer.MultiplayerPeer.Close();

		return Error.Ok;
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
		if (!Multiplayer.IsServer())
		{
			return;
		}

		GD.Print(String.Format("Server: Client connected ({0}).", clientID));

		// Disconnect the new client after maxAuthenticationTime if they have failed to send authentication.
		TryDisconnectInvalidClient(maxAuthenticationTime, (int)clientID);
	}

	protected virtual void OnPeerDisconnected(long clientID)
	{
		if (!Multiplayer.IsServer())
		{
			return;
		}

		// Unregister the client if it was validated
		if (IsClientValid((int)clientID))
		{
			UnregisterClient((int)clientID);
		}

		GD.Print(String.Format("Server: Client disconnected ({0}).", clientID));
	}

	protected virtual void OnConnectionSucceed()
	{
		// Called on clients only!
		GD.Print(String.Format("Client: Connection established."));

		// Once we've established connection to server, authenticate this client!
		AuthenticateClient();
	}

	protected virtual void OnConnectionFail()
	{
		// Called on clients only!
		GD.Print("Client connection failed!");
		Reset();
	}

	protected virtual void OnDisconnect()
	{
		// Called on clients only!
		GD.Print(String.Format("Client: Disconnected."));
		Reset();
	}

	// Reset lobby completely (clears clients, disconnects peer, etc)
	private void Reset()
	{
		// Generate list of all local clients.
		int[] clientIDs = new int[clients.Count];
		clients.Keys.CopyTo(clientIDs, 0);

		// Unregister all local clients
		for (int i = 0; i < clientIDs.Length; i++)
		{
			UnregisterLocalClient(clientIDs[i], ref clients);
		}
		Multiplayer.MultiplayerPeer = null;
		clients.Clear();
	}


	/*********************************************************************************************/
	/** Client Authentication */

	protected virtual uint GetAuthenticationHash()
	{
		return (version + password).Hash();
	}

	private void AuthenticateClient()
	{
		// Send authentication information to the server 

		int clientID = Multiplayer.GetUniqueId();
		uint clientAuthentication = GetAuthenticationHash();

		RpcId(1, MethodName.Command_AuthenticateNewClient, clientID, clientAuthentication);
	}

	public bool IsClientValid(int clientID)
	{
		return clients.ContainsKey(clientID);
	}

	private async void TryDisconnectInvalidClient(float delay, int clientID)
	{
		await Task.Delay((int)(delay * 1000f));
		TryDisconnectInvalidClient(clientID);
	}

	private void TryDisconnectInvalidClient(int clientID)
	{
		GD.Print(String.Format("Server: Ensuring client is valid {0}.", clientID));

		// FIXME : This function will throw an error if the peer has already disconnected.
		// TEST : Occures when client authenticates and then leaves before @maxAuthenticationTime elapses.

		if (!IsClientValid(clientID))
		{
			DisconnectInvalidClient(clientID);
		}
	}

	private void DisconnectInvalidClient(int clientID)
	{
		Debug.Assert(Multiplayer.IsServer(), "Illegal network operation: Attempted to disconnect a client from another client.");
		GD.Print(String.Format("Server: Failed to validate client : ({0}). Disconnecting from server.", clientID));
		Multiplayer.MultiplayerPeer.DisconnectPeer(clientID, false);
	}

	/*********************************************************************************************/
	/** RPC - Client => Server */

	// Sent from clients to the server on initial connection
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void Command_AuthenticateNewClient(int clientID, uint clientAuth)
	{
		Debug.Assert(Multiplayer.IsServer(), "Illegal network operation: Attempted to authenticate a client on remote.");

		uint expectedAuthHash = GetAuthenticationHash();
		bool clientAuthenticated = clientAuth == expectedAuthHash;

		if (clientAuthenticated)
		{
			GD.Print(String.Format("Server: Validated client : ({0}).", clientID));
			RegisterClient(clientID);
		}
		else
		{
			DisconnectInvalidClient(clientID);
		}
	}

	/*********************************************************************************************/
	/** RPC - Server => Client*/

	// Sends set of clients to all clients in the following format:
	// [N, CLIENT_0_ID, CLIENT_1_ID, ... CLIENT_N_ID]
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RPC_SyncClientList(int[] clientData)
	{
		Debug.Assert(!Multiplayer.IsServer(), "Illegal network operation: Client sync recieved by server.");

		UpdateClients(clientData);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RPC_SpawnPlayerForClient(int clientID)
	{
		Debug.Assert(!Multiplayer.IsServer(), "Illegal network operation: RPC_SpawnPlayerForClient recieved by server.");

		SpawnPlayerForClient(clientID);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RPC_DespawnPlayerForClient(int clientID)
	{
		Debug.Assert(!Multiplayer.IsServer(), "Illegal network operation: RPC_DespawnPlayerForClient recieved by server.");

		DespawnPlayerForClient(clientID);
	}

	/*********************************************************************************************/
	/** Client Registration (server) */

	protected void RegisterClient(int clientID)
	{
		Debug.Assert(Multiplayer.IsServer(), "Illegal network operation: Attempted to register a client on remote.");

		if (clients.ContainsKey(clientID))
		{
			throw new Exception("Attempted to add client already in the client list. Client ID : " + clientID.ToString());
		}

		// Register client to list
		RegisterLocalClient(clientID, ref clients);

		// Sync info to remote peers
		int[] data = SerializeClients(clients);
		Rpc(MethodName.RPC_SyncClientList, data);
		// Rpc(MethodName.RPC_SpawnPlayerForClient, clientID);
	}

	protected void UnregisterClient(int clientID)
	{
		Debug.Assert(Multiplayer.IsServer(), "Illegal network operation: Attempted to register a client on remote.");

		if (!clients.ContainsKey(clientID))
		{
			throw new Exception("Attempted to remove client that is not in the client list. Client ID : " + clientID.ToString());
		}

		// Remove client from list
		UnregisterLocalClient(clientID, ref clients);

		// Sync info to remote peers
		int[] data = SerializeClients(clients);
		Rpc(MethodName.RPC_SyncClientList, data);
	}

	private int[] SerializeClients(Dictionary<int, Client> serialize)
	{
		// TODO : We might not need to send the first int as length of array, this is C# after all!

		int count = serialize.Count;
		int[] data = new int[count + 1];

		int[] keys = serialize.Keys.ToArray<int>();

		data[0] = count;
		for (int i = 0; i < keys.Length; i++)
		{
			data[i + 1] = keys[i];
		}

		return data;
	}


	/*********************************************************************************************/
	/** Client Registration (client) */

	// [N, CLIENT_0_ID, CLIENT_1_ID, ... CLIENT_N_ID]
	private void UpdateClients(int[] serverClientData)
	{
		int clientCount = serverClientData[0];
		HashSet<int> remoteClientIDs = new HashSet<int>(clientCount);

		for (int i = 1; i < clientCount + 1; i++)
		{
			int newClientID = serverClientData[i];
			remoteClientIDs.Add(newClientID);

			// If this client is not current in the local client list,
			if (!clients.ContainsKey(newClientID))
			{
				// Add to local list
				RegisterLocalClient(newClientID, ref clients);
			}
		}

		// Generate list of all local clients.
		int[] localClientIDs = new int[clients.Count];
		clients.Keys.CopyTo(localClientIDs, 0);

		// Remove any local clients that have been removed from the servers list,
		for (int i = 0; i < localClientIDs.Length; i++)
		{
			// If the client has been removed from the server list,
			if (!remoteClientIDs.Contains(localClientIDs[i]))
			{
				// Remove the client from local list 
				UnregisterLocalClient(localClientIDs[i], ref clients);
			}
		}
	}

	protected virtual Client RegisterLocalClient(int clientID, ref Dictionary<int, Client> clientDictonary)
	{
		Debug.Assert(!clientDictonary.ContainsKey(clientID), String.Format("Unable to regsiter local client {0}. Client already exists.", clientID));

		Client newClient = new Client(clientID);

		newClient.OnRegisterClient();
		clientDictonary.Add(clientID, newClient);

		return newClient;
	}

	protected virtual bool UnregisterLocalClient(int clientID, ref Dictionary<int, Client> clientDictonary)
	{
		Debug.Assert(clientDictonary.ContainsKey(clientID), String.Format("Unable to unregsiter local client {0}. Client does not exist.", clientID));

		clientDictonary[clientID].OnUnregisterClient();
		return clientDictonary.Remove(clientID);
	}


	protected virtual void SpawnPlayerForClient(int clientID)
	{
		Debug.Assert(clients.ContainsKey(clientID), String.Format("Unable to spawn player for client {0}. Client does not exist.", clientID));

		Node clientPlayerNode = playerScene.Instantiate();
		clientPlayerNode.Name = String.Format("client ({0})", clientID);
		AddChild(clientPlayerNode);
		clientPlayerNode.SetMultiplayerAuthority(clientID);

		clients[clientID].SetPlayer(clientPlayerNode);
	}

	protected virtual void DespawnPlayerForClient(int clientID)
	{
		Debug.Assert(clients.ContainsKey(clientID), String.Format("Unable to despawn player for client {0}. Client does not exist.", clientID));
		Debug.Assert(clients[clientID].GetPlayer() != null, String.Format("Unable to despawn player for client {0}. Player node does not exist.", clientID));

		clients[clientID].GetPlayer().QueueFree();
	}
}
