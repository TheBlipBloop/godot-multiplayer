using Godot;

// TODO : Modular architecture for the NetworkManager class? I feel this should not be its own node
// TODO : Documentation
// TODO : Godot C# standards
//
// TODO : Investigate the effect of join time (and by extension relative placement of pings and time syncs)
// TODO : Randomize ping / sync frequencies

/// <summary>
/// Node responsible for tracking network time across clients / server
/// </summary>
public partial class NetworkTime : Node
{
    /*********************************************************************************************/
    /** Network Time */

    [Export]
    protected float syncFrequency = 1f;

    private float nextSyncTime = 0;

    private float localToServerTimeOffset;

    // Copy of network time exported for debugging purposes.
    [Export]
    private float debug_NetworkTime;

    [Export]
    private Label debug_timeLabel;

    /*********************************************************************************************/
    /** Ping */

    // Number of past ping samples to reference when calcualting smoothed ping.
    const int PING_SMOOTHING_SAMPLES = 6;

    // Weights applied to ping samples (chronologically). Will be normalized to sum to 1.0 at runtime.
    readonly float[] PING_SMOOTHING_WEIGHTS = new float[]
    {
        0.35f,
        0.25f,
        0.20f,
        0.1f,
        0.05f,
        0.05f,
    };

    // Round trip time from client to server in seconds
    protected float ping;

    // Round trip time smoothed using an average of the last @PING_SMOOTHING_SAMPLES samples.
    protected float smoothedPing;

    // Circular array of recent ping samples. Used when calculating @smoothedPing.
    private float[] pingSamples = new float[PING_SMOOTHING_SAMPLES];

    private int nextSampleIndex = 0;

    private float localPingTime;

    /*********************************************************************************************/
    /** Engine Methods */

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ResetPingSamples();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (
            !Multiplayer.HasMultiplayerPeer()
            || Multiplayer.MultiplayerPeer.GetConnectionStatus()
                != MultiplayerPeer.ConnectionStatus.Connected
        )
        {
            return;
        }

        if (
            Multiplayer.HasMultiplayerPeer()
            && !Multiplayer.IsServer()
            && GetLocalTime() > nextSyncTime
        )
        {
            localPingTime = GetLocalTime();
            RpcId(1, MethodName.Command_Ping);
            nextSyncTime = GetLocalTime() + 1f / syncFrequency;
        }

        if (
            Multiplayer.HasMultiplayerPeer()
            && Multiplayer.IsServer()
            && GetLocalTime() > nextSyncTime
        )
        {
            GD.Print("Syncing time from server");
            SyncNetworkTime();
            nextSyncTime = GetLocalTime() + 1f / syncFrequency;
        }

        ProcessDebugging();
    }

    /*********************************************************************************************/
    /** Time */

    protected void SyncNetworkTime()
    {
        this.EnsureServer();

        Rpc(MethodName.RPC_SyncNetworkTime, GetLocalTime());
    }

    public float GetNetworkTime()
    {
        if (Multiplayer.MultiplayerPeer != null && Multiplayer.IsServer())
        {
            return GetLocalTime();
        }
        else
        {
            float delayFromServer = smoothedPing / 2f;
            return GetLocalTime() + localToServerTimeOffset + delayFromServer;
        }
    }

    public float GetLocalTime()
    {
        return (float)Time.GetTicksMsec() / 1000f;
    }

    /*********************************************************************************************/
    /** Ping */

    protected void ResetPingSamples()
    {
        for (int i = 0; i < pingSamples.Length; i++)
        {
            pingSamples[i] = 0;
        }
    }

    protected void RecordPingSample(float newPingSample)
    {
        pingSamples[nextSampleIndex] = newPingSample;
        nextSampleIndex = (nextSampleIndex + 1) % PING_SMOOTHING_SAMPLES;
    }

    protected float AveragePingSamples()
    {
        // TODO : Clean up
        float sum = 0f;
        float sumWeight = 0f;

        int index = nextSampleIndex - 1;
        if (index < 0)
        {
            index = PING_SMOOTHING_SAMPLES - 1;
        }

        int temporalDistance = 0;

        while (temporalDistance != pingSamples.Length)
        {
            float weight = PING_SMOOTHING_WEIGHTS[temporalDistance];

            string output =
                weight.ToString() + " * " + (pingSamples[index] * 1000).ToString() + "ms";
            GD.Print(output);

            sum += pingSamples[index] * weight;
            if (pingSamples[index] > 0)
            {
                sumWeight += weight;
            }

            // Wrap around!
            index = index - 1;
            if (index < 0)
            {
                index = pingSamples.Length - 1;
            }
            temporalDistance += 1;
        }

        return sum / sumWeight;
    }

    /*********************************************************************************************/
    /** Server */

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered
    )]
    private void RPC_SyncNetworkTime(float newNetworkTime)
    {
        localToServerTimeOffset = newNetworkTime - GetLocalTime();
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered
    )]
    private void RPC_Pong()
    {
        ping = GetLocalTime() - localPingTime;

        RecordPingSample(ping);
        smoothedPing = AveragePingSamples();

        float pingMs = 1000f * ping;

        GD.Print("P:" + pingMs.ToString());
        GD.Print("S:" + (1000f * smoothedPing).ToString());
    }

    /*********************************************************************************************/
    /** Client */

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered
    )]
    private void Command_Ping()
    {
        GD.Print("Ping");
        int clientId = Multiplayer.GetRemoteSenderId();
        RpcId(clientId, MethodName.RPC_Pong);
    }

    /*********************************************************************************************/
    /** Debugging */

    private void ProcessDebugging()
    {
        debug_NetworkTime = GetNetworkTime();
        debug_timeLabel.Text = GetNetworkTime().ToString();
    }
}
