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

    // The number of times per second that the server will ping all clients.
    // Pinging will resynchronize network clock on clients.
    [Export]
    protected float PingAndSyncFrequency = 1f;

    private float _nextSyncTime = 0;

    private float _localToServerTimeOffset;

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

    // Circular array of recent ping samples. Used when calculating @smoothedPing.
    private float[] _pingSamples = new float[PING_SMOOTHING_SAMPLES];
    private int _nextSampleIndex = 0;

    // Round trip time from client to server in seconds
    protected float _ping = 0f;

    // Round trip time smoothed using an average of the last @PING_SMOOTHING_SAMPLES samples.
    protected float _smoothedPing = 0f;

    private float _localPingTime = 0f;

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

        if (!Multiplayer.IsServer() && GetLocalTime() > _nextSyncTime)
        {
            _localPingTime = GetLocalTime();
            RpcId(1, MethodName.Command_Ping);
            _nextSyncTime = GetLocalTime() + 1f / PingAndSyncFrequency;
        }
    }

    /*********************************************************************************************/
    /** Time */

    public float GetNetworkTime()
    {
        if (Multiplayer.MultiplayerPeer != null && Multiplayer.IsServer())
        {
            return GetLocalTime();
        }
        else
        {
            float delayFromServer = _smoothedPing / 2f;
            return GetLocalTime() + _localToServerTimeOffset + delayFromServer;
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
        for (int i = 0; i < _pingSamples.Length; i++)
        {
            _pingSamples[i] = 0;
        }
    }

    protected void RecordPingSample(float newPingSample)
    {
        _pingSamples[_nextSampleIndex] = newPingSample;
        _nextSampleIndex = (_nextSampleIndex + 1) % PING_SMOOTHING_SAMPLES;
    }

    protected float AveragePingSamples()
    {
        float sum = 0f;
        float totalWeight = 0f;

        int index = _nextSampleIndex - 1;
        if (index < 0)
        {
            index = PING_SMOOTHING_SAMPLES - 1;
        }

        int temporalDistance = 0;

        // Calculate a weighted average where the most recently recorded ping is
        // scaled by the first index of PING_SMOOTHING_SAMPLES, second most recent
        // by second index of PING_SMOOTHING_SAMPLES, etc.
        while (temporalDistance != _pingSamples.Length)
        {
            float samplePing = _pingSamples[index];
            bool hasValidSample = samplePing > 0;

            if (hasValidSample)
            {
                float weight = PING_SMOOTHING_WEIGHTS[temporalDistance];
                sum += samplePing * weight;
                totalWeight += weight;
            }

            // Wrap around @ index 0
            index = index - 1;
            if (index < 0)
            {
                index = _pingSamples.Length - 1;
            }
            temporalDistance += 1;
        }

        return sum / totalWeight;
    }

    // Returns the most recent successful RTT. Seconds.
    public float GetPing()
    {
        return _ping;
    }

    // Returns a weighted average of recent pings. Seconds.
    public float GetPingSmoothed()
    {
        return _smoothedPing;
    }

    /*********************************************************************************************/
    /** Server */

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = false,
        TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered
    )]
    private void RPC_Pong(float latestNetworkTime)
    {
        this.EnsureClient();

        // Calculate local time offsets (sync time)
        _localToServerTimeOffset = latestNetworkTime - GetLocalTime();

        // Calculate ping (normal & smoothed)
        _ping = GetLocalTime() - _localPingTime;

        RecordPingSample(_ping);
        _smoothedPing = AveragePingSamples();
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
        this.EnsureServer();
        int clientId = Multiplayer.GetRemoteSenderId();
        RpcId(clientId, MethodName.RPC_Pong, GetLocalTime());
    }
}
