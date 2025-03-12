using Godot;
using System;

/// <summary>
/// Clients are objects that represent an authenticated connection to the server.
/// By default, each client spawns a player node which they have network authority over.
/// </summary>
[Serializable]
public partial class Client : RefCounted, IBlittable
{
	/*********************************************************************************************/
	/** Client Variables */

	// Unique network ID for this client
	[Export]
	protected int networkID;

	// Server time when this client was connected.
	// TODO : Must have unified server time for this to make sense, which is silly but fun!
	[Export]
	protected float connectTime;

	// Player node belonging to this client.
	protected Node playerInstance;

	/*********************************************************************************************/
	/** Constructor */

	public Client(int _networkID)
	{
		networkID = _networkID;
	}

	/*********************************************************************************************/
	/** Client Registration */

	public virtual void OnRegisterClient(Node root, PackedScene playerPrefab)
	{
		// Spawn in your player character, etc
		playerInstance = playerPrefab.Instantiate<Node>();
		playerInstance.SetMultiplayerAuthority(networkID);

		playerInstance.Name = String.Format("client_{0}", networkID);

		root.AddChild(playerInstance);
	}

	public virtual void OnUnregisterClient()
	{
		// Destroy your player character, etc
		playerInstance.QueueFree();
	}

	/*********************************************************************************************/
	/** Getters / Setters */

	public int GetNetworkID()
	{
		return networkID;
	}

	public Node GetPlayer()
	{
		return playerInstance;
	}

	/*********************************************************************************************/
	/** Blittable */

	public Object[] ToBlittable()
	{
		Object[] objects = new Object[2];

		objects[1] = networkID;

		return objects;
	}

	public Object fromBlittable(Object[] data)
	{
		return new Client(networkID);
	}
}
