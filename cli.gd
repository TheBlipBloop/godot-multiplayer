#!/usr/bin/env -S godot -s
extends Node
# https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html


class commands:
	func test (bob: String, testy: int) -> void:
		print(bob)
		print(testy+1)
	
	func start_server(ip: String, password: String):
		var lobby_scene: Lobby = preload("uid://cdjo6xqp3diue").instantiate() as Lobby
		var lobby_instance = lobby_scene.GetLobbyInstance()
		lobby_instance.SetPassword(password)
		var error = lobby_instance.Host(ip)
		if error != OK:
			print(error_string( error))


func _ready() -> void:
	var c = commands.new()
	var args: Dictionary = get_args()
	for command in c.get_method_list():
		if command["name"] == args["command"]:
			var resp = call_method(c, command, args)
			if resp != "":
				print(resp)
			break

func call_method(c: commands, command: Dictionary, args: Dictionary) -> String:
	var arg_num:int = c.get_method_argument_count(command["name"])
	var inp_num:int = args["args"].size()

	# Not enough arguments
	if arg_num > inp_num:
		return "Insufficient arguments provided"

	# Discard extra provided arguments
	var inputs: Array = []
	for i in range(inp_num):
		if i >= arg_num:
			break
		inputs.append( args["args"][i])
		
	# type check arguments
	for i in range(command["args"].size()):
		if ! is_instance_of( inputs[i], command["args"][i]["type"]):
			return "Type of \"" + inputs[i] + "\" was incorrect, expected " + type_string(command["args"][i]["type"])
	
	c.callv(command["name"], inputs)
	return ""


func get_args() -> Dictionary:
	var args : PackedStringArray = OS.get_cmdline_user_args()
	if args.size() <= 0:
		return {}
	var cmd = args[0]
	args.remove_at(0)

	var newArgs: Array = []
	for i in range(args.size()):
		if args[i].is_valid_int():
			newArgs.append(args[i].to_int())
			continue
		newArgs.append(args[i])
	
	return {"command": cmd, "args":newArgs}




