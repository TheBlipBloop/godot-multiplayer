using Godot;
using System;

public partial class DemoPlayer : RigidBody2D
{
	[Export]
	protected Sprite2D sprite;

	[Export]
	protected float speed = 128f;

	[ExportGroup("Synchronization (local)")]

	[Export]
	protected float positionSyncPerSecond = 10;

	private float nextSyncTime = 0;

	[ExportGroup("Synchronization (remote)")]

	protected Vector2 remotePosition;

	[Export]
	protected float snapToPositionDistance = 128f;

	[Export]
	protected Node2D remotePositionDebug;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GravityScale = 0.0f;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Multiplayer.HasMultiplayerPeer() && IsMultiplayerAuthority())
		{
			ProcessLocal((float)delta);
		}
		else
		{
			ProcessRemote((float)delta);
		}

	}

	protected virtual void ProcessRemote(float delta)
	{
		sprite.Modulate = new Color(0.5f, 0.5f, 0.5f, 1f);

		float distanceSq = remotePosition.DistanceSquaredTo(Position);

		if (distanceSq > snapToPositionDistance * snapToPositionDistance)
		{
			Position = remotePosition;
		}

		Position = Position.Lerp(remotePosition, delta * 8f);
		remotePositionDebug.GlobalPosition = remotePosition;
	}

	protected virtual void ProcessLocal(float delta)
	{
		if (Input.IsActionPressed("ui_left"))
		{
			LinearVelocity += Vector2.Left * delta * speed;
		}
		if (Input.IsActionPressed("ui_right"))
		{
			LinearVelocity += Vector2.Right * delta * speed;
		}
		if (Input.IsActionPressed("ui_up"))
		{
			LinearVelocity += Vector2.Up * delta * speed;
		}
		if (Input.IsActionPressed("ui_down"))
		{
			LinearVelocity += Vector2.Down * delta * speed;
		}

		float timeSeconds = (float)Time.GetTicksMsec() / 1000f;
		if (timeSeconds > nextSyncTime)
		{
			nextSyncTime = timeSeconds + (1f / positionSyncPerSecond);
			Command_SendPosition(Position);
		}

	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
	protected void RPC_SyncPosition(Vector2 newPosition)
	{
		if (IsMultiplayerAuthority())
		{
			return;
		}
		remotePosition = newPosition;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
	private void Command_SendPosition(Vector2 newPosition)
	{
		remotePosition = newPosition;
		Rpc(MethodName.RPC_SyncPosition, newPosition);
	}
}
