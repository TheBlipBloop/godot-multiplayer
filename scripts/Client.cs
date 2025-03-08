using Godot;
using System;

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

	// public virtual void OnRegisterClient()
	// {
	// 	GD.Print("reg client");
	// }

	// public virtual void OnUnregisterClient()
	// {
	// 	GD.Print("unreg client");
	// }

	/*********************************************************************************************/
	/** Getters / Setters */

	public void SetPlayer(Node newPlayerInstance)
	{
		playerInstance = newPlayerInstance;
	}

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
