using Godot;
using GodotNetworking;
using System;

public partial class Menu : Control
{

	[Export]
	protected Lobby lobby;

	[Export]
	protected TextEdit textEdit;

	[Export]
	protected string password;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		password = textEdit.Text;
		lobby.SetPassword(password);
	}

	/*********************************************************************************************/
	/** Button Signals */

	public void _on_host_button_down()
	{
		Error e = lobby.Host("127.0.0.1");
		if (e != Error.Ok)
		{
			GD.Print(e.ToString());
		}
	}

	public void _on_join_button_down()
	{
		Error e = lobby.Connect("127.0.0.1");
		if (e != Error.Ok)
		{
			GD.Print(e.ToString());
		}
	}

	public void _on_exit_button_down()
	{
		Exit();
	}

	public void _on_password_text_changed()
	{
		password = textEdit.Text;
		lobby.SetPassword(password);
	}

	public void _on_disconnect_button_down()
	{
		lobby.Disconnect();
	}

	/*********************************************************************************************/
	/** Menu */

	private void Exit()
	{
		GetTree().Quit(0);
	}
}
