using Godot;
using System;

[Serializable]
public partial class Client : RefCounted, IBlittable
{
	/*********************************************************************************************/
	/** Client Variables */

	// IP string, IPv4 / IPv6
	protected string ip;

	// Unique network ID for this client
	protected int networkID;

	// Server time when this client was connected.
	// TODO : Must have unified server time for this to make sense, which is silly but fun!
	protected float connectTime;

	/*********************************************************************************************/
	/** Constructor */

	public Client(string _ip, int _networkID)
	{
		ip = _ip;
		networkID = _networkID;
	}

	/*********************************************************************************************/
	/** Getters / Setters */

	public string GetIPString()
	{
		return ip;
	}

	public int GetNetworkID()
	{
		return networkID;
	}

	/*********************************************************************************************/
	/** Blittable */

	public Object[] ToBlittable()
	{
		Object[] objects = new Object[2];

		objects[0] = ip;
		objects[1] = networkID;

		return objects;
	}

	public Object fromBlittable(Object[] data)
	{
		ip = (string)data[0];
		networkID = (int)data[1];

		return new Client(ip, networkID);
	}

}
