using Godot;
using System;

public partial class Entrypoint : Node {
    public partial class Cmds : Node {
        public void run(String ip, String password) {
            PackedScene lobby_scene =
                ResourceLoader.Load<PackedScene>("uid://cdjo6xqp3diue");
            // Lobby lobby_instance = (Lobby)lobby_scene.Instantiate();
            Error e = Lobby.GetLobbyInstance().StartServer(ip, password);
            if (e != Error.Ok) {
                GD.Print(e.ToString());
            }
        }
    }

    public override void _Ready() {
        Error result = GDParser.run(new Cmds());
        if (result != Error.Ok) {
            GD.Print(result);
        }
    }
}
