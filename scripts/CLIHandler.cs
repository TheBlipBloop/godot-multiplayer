using Godot;
using System;

namespace GodotNetworking
{
	/// <summary>
	/// Parses command line arguments for the networking system
	/// </summary>
	public static class CLIHandler
	{
		private const string ARG_START_SERVER = "server";

		private const string ARG_BIND_IP = "ip=";

		private const string ARG_PORT_OVERRIDE = "port=";

		public static void ProcessCommandLineArguments(string[] args)
		{
			int port = Lobby.GetLobbyInstance().GetPort();

			ParseArgumentInt(args, ARG_PORT_OVERRIDE, out port);

			if (HasArgument(args, ARG_START_SERVER))
			{
				StartServer(port);
			}
		}

		private static void StartServer(int port)
		{
			Lobby lobby = Lobby.GetLobbyInstance();
			// lobby.Host()
			// TODO 
		}

		private static bool HasArgument(string[] args, string query)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (query.Equals(args[i]))
				{
					return true;
				}
			}

			return false;
		}


		private static bool ParseArgumentInt(string[] args, string query, out int value)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith(query))
				{
					string[] split = args[i].Split('=');
					value = (int)split[1].ToInt();

					return true;
				}
			}

			value = 0;
			return false;
		}
	}
}