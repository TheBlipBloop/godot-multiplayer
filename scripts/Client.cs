using Godot;
using System;

[Serializable]
public class Client
{
	/*********************************************************************************************/
	/** Client Variables */

	// IP string, IPv4 / IPv6
	protected string ip;

	// Unique network ID for this client
	protected int networkID;

	// Set to true once this client is completely initialized (and authenticated).
	protected bool initialized;

	/*********************************************************************************************/
	/** Constructor */

	public Client(string _ip, int _networkID)
	{
		ip = _ip;
		networkID = _networkID;
		initialized = false;
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

	public bool IsInitialized()
	{
		return initialized;
	}
}
