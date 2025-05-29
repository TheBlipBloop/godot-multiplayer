using System;
using Godot;

public partial class Menu : CanvasLayer
{
    protected LineEdit _passwordInput = null;

    protected LineEdit _ipInput = null;

    private String ip
    {
        get { return _ipInput.Text; }
    }

    private String password
    {
        get { return _passwordInput.Text; }
    }

    protected Lobby LobbyInstance
    {
        get { return Lobby.GetLobbyInstance(); }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _passwordInput = GetNode<LineEdit>("%password");
        _ipInput = GetNode<LineEdit>("%ip");
    }

    /*********************************************************************************************/
    /** Button Signals */

    public void _on_host_button_down()
    {
        Error e = Lobby.GetLobbyInstance().StartServer(ip, password);
        GD.Print(password);
        if (e != Error.Ok)
        {
            GD.Print(e.ToString());
        }
    }

    public void _on_join_button_down()
    {
        Error e = Lobby.GetLobbyInstance().ConnectClient(ip, password);
        GD.Print(password);
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
