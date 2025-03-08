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

	public float debug_reg_time;

	/*********************************************************************************************/
	/** Constructor */

	public Client(int _networkID)
	{
		networkID = _networkID;
	}

	/*********************************************************************************************/
	/** Client Registration */

	public virtual void OnRegisterClient()
	{
		// Spawn in your player character, etc
		GD.Print("!!!reg client!!!");
		debug_reg_time = (float)Time.GetTicksMsec() / 1000f;
	}

	public float DEBUG_GetLifetime()
	{
		return ((float)Time.GetTicksMsec() / 1000f) - debug_reg_time;
	}

	public virtual void OnUnregisterClient()
	{
		// Destroy your player character, etc
		GD.Print("!!!unreg client!!!");
	}

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
