using System.CommandLine;
using System.CommandLine.Parsing;
using Godot;

public partial class GDParser : SceneTree {

    /// <summary>
    /// Main parser; Run on _ready in entrypoint, with target containing user
    /// accessible commands.
    /// </summary>
    /// <param name="target"></param>
    /// <returns>Returns an error if one has occurred, else Error.Ok </returns>
    public static Error run(Node target) {

        Command root = AssembleCommand(target);
        ParseResult pr = getArgs(root);

        // Handle default command
        if (pr.Tokens.Count == 0 && target.HasMethod("defaultCmd")) {
            // TODO: Add support for default command parameters
            target.Callv("defaultCmd", []);
            return Error.Ok;
        }

        // Command not found
        if (!target.HasMethod(pr.CommandResult.Command.Name)) {
            return Error.DoesNotExist;
        }

        // Assemble command params
        Godot.Collections.Array parameters = new Godot.Collections.Array();
        for (int i = 0; i < pr.Tokens.Count; i++) {
            Token item = pr.Tokens[i];
            if (i == 0) {
                continue;
            }
            parameters.Add(item.Value);
        }

        // Check for invalid parameter count
        if (target.GetMethodArgumentCount(pr.CommandResult.Command.Name) !=
            parameters.Count) {
            return Error.InvalidParameter;
        }
        // TODO: Handle command type checking

        // Call command
        target.Callv(pr.CommandResult.Command.Name, parameters);
        return Error.Ok;
    }

    /// <summary>
    /// takes all non-node methods (user defined methods when target inherets
    /// from node) and assembles them into callable comands.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private static Command AssembleCommand(Node target) {
        // TODO: Allow users to inheret command classes from Node subclasses
        // (eg: node2D) without also including additional methods in the command
        // TODO: Can the assembled command be cached to avoid being rebuilt
        // every consecutive run?
        Command root = new Command(nameof(target));
        Node baseNode = new Node();

        foreach (Godot.Collections.Dictionary command in target
                     .GetMethodList()) {
            if (!baseNode.GetMethodList().Contains(command)) {
                string name = (string)command["name"];
                root.AddCommand(new Command(name));
            }
        }
        baseNode.QueueFree();
        return root;
    }

    /// <summary>
    /// Parses current command arguments against assembled command
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private static ParseResult getArgs(Command root) {
        string[] args = OS.GetCmdlineUserArgs();
        Parser p = new Parser(root);
        return p.Parse(args);
    }
}
