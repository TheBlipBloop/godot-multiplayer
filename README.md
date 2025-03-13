# GODOT-MULTIPLAYER (name TBD)
Networking utilities for making multiplayer games in Godot 4.4. 

WIP.


# Roadmap (TODO)
- [X] Server hosting
- [X] Client connection
- [X] Client authentication with version
- [X] Client authentication with password
- [X] Player spawning with client authority
- [X] Client authority movement / sync demo
- [ ] Launch server via CLI args in headless mode
- [ ] Custom [Server] and [Client] attributes with safety checks
- [ ] Hybrid server-client hosting
- [ ] Track client latency
- [ ] Network time
- [ ] Demo with server authoritative movement + prediction / recon
- [ ] Methods for accessing client data from the lobby singleton
- [ ] Hosting / Connecting using UPNP
- [ ] Steam relay support
- [ ] Plug n play support for alternative (non ENET) multiplayer API's
- [ ] Conform to Godot naming conventions!
- [ ] Make sure lobby system plays nice with multi-scene setups
- [ ] Player Spawn Point class? 
- [ ] Cleanup client registration / registration code 
- [ ] A better name for this project
- [ ] Figure out how to turn this project into a plugin
- [ ] Unit testing!!!!!


# Things that would be fun to build to support this project
- [ ] Asset validation library
- [ ] C# Unit Testing Framework

# Known Bugs
So I don't forget to fix them!
- If a client joins the server, fails to authenticate, and then leaves the server before being kicked, an error is thrown by the lobby as it attempts to remove an non-existent peer.
