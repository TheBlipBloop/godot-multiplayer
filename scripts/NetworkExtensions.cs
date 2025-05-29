#define DEBUG_NETWORKING

using Godot;

public static class NetworkExtensions
{
    // Throws an error if this node is not running on the server.
    public static void EnsureServer(this Node node)
    {
#if DEBUG_NETWORKING

        if (!IsConnected(node))
        {
            return;
        }

        bool isServer = node.Multiplayer.IsServer();

        if (!isServer)
        {
            throw new System.Exception("Attempted to run a server function on a non-server node.");
        }
#endif
    }

    // Throws an error if this node is not running as a client connected to a server.
    public static void EnsureClient(this Node node)
    {
#if DEBUG_NETWORKING

        if (!IsConnected(node))
        {
            return;
        }

        bool isServer = node.Multiplayer.IsServer();

        if (isServer)
        {
            throw new System.Exception("Attempted to run a client function on a non-client node.");
        }
#endif
    }

    private static bool IsConnected(Node node)
    {
        return node.Multiplayer.MultiplayerPeer != null
            && node.Multiplayer.MultiplayerPeer.GetConnectionStatus()
                == MultiplayerPeer.ConnectionStatus.Connected;
    }
}
