using System;
using Godot;

public partial class Menu : CanvasLayer
{
    protected LineEdit passwordInput = null;

    protected LineEdit ipInput = null;

    private String ip
    {
        get { return ipInput.Text; }
    }

    private String password
    {
        get { return passwordInput.Text; }
    }

    protected Lobby LobbyInstance
    {
        get { return Lobby.GetLobbyInstance(); }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        passwordInput = GetNode<LineEdit>("%password");
        ipInput = GetNode<LineEdit>("%ip");
    }

    /*********************************************************************************************/
    /** Button Signals */

    public void _on_host_button_down()
    {
        Error e = Lobby.GetLobbyInstance().StartServer(ipInput.Text);
        if (e != Error.Ok)
        {
            GD.Print(e.ToString());
        }
    }

    public void _on_join_button_down()
    {
        Error e = Lobby.GetLobbyInstance().ConnectClient(ipInput.Text);
        if (e != Error.Ok)
        {
            GD.Print(e.ToString());
        }
    }

    public void _on_exit_button_down()
    {
        Exit();
    }

    public void _on_disconnect_button_down()
    {
        LobbyInstance.Disconnect();
    }

    /*********************************************************************************************/
    /** Menu */

    private void Exit()
    {
        GetTree().Quit(0);
    }
}
