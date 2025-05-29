# Multiplayer Game Framework within Godot

Let's make some tools around multiplayer development in Godot!

These should be relatively agnostic to the networking backend (Web vs ENET, etc)

## Network Manager

We are going to need a Node to handle the following:

1. Hosting Servers
2. Joining Servers
3. Host Client-Server
4. Keeping track of connected players

### Feature Creep

4. Kicking / Banning players
5. Password Protection / connection validation

## Server

Maintain and synchronize variables to clients as needed.

Send data to ALL clients.

Send data to a SINGLE client.

### FEATURE CREEP

- Players could express a level of interest (0-255) in other players that determins what players need updates.

## Clients

Represent a connection to a server. Not the players character but everything else.

I'm not sure if we actually need this

IsLocalPlayer()
GetClientID()

## Characters

UpdateServer()
UpdateLocal()
UpdateRemote()

## Misc

### Bit (un)packing utils

### More granular multiplayer sync

this could be a cool excuse to mod the engine a bit, I'd like a version of the multiplayer synchronizer that supports more granualar control over what is sent over network.

For exmaple, each "sync" propery would have its own send (to server AND from server) rates which could be changed at real time and synced across (e.g. sync 10Hz for cleints of lower interest to a given player VS 24hz for those of interest, etc)

# NOTES ON GODOT NETWORKING

- Pretty standard so far
- Names of nodes (e.g. exact paths) are used for syncing RPC targets
- RPCs can be sent to specific / all clients easily
- RPCs are hashed&checksumed excluding parameters
- Channels exist its unclear if there is a limit on how many we can have
- MultiplayerSynchronizer visiblity does infact refer to the nodes in game visiblity, not network visiblity
- ENET is common backend, but not usable on the web. TODO : Learn more about the common networking interfaces and how we can write tools across them
- There seems to be some limited community driven support for Steam based networking, we should see if we can support that too!
- MultiplayerSpawner exists. Need to look into how objects 'spawn' on the network. Looks like there may be some needed setup. TODO.
- Unclear to what extent godot networking has been deployed in The Real World, may have some funky bullshit :|
- Rumor has it that the MultiplayerSyncronizer is performing some sort of client-prediction server-validation? Unclear if thats the case looking at the source code. TODO
- Server → Client: @rpc("authority", "call_remote", "reliable")
- Server → Every peer: @rpc("authority", "call_local", "reliable")
- Client → Server: @rpc("any_peer", "call_remote", "reliable")
- Authority Client → Server: @rpc("authority", "call_remote", "reliable")
- The unique ID is not assigned sequntially, its just a random number (host is always 1!)
- Multiplayer State is determined by a parent root node of a scene, it might be possible to load a Client and Server scene under the same node?

# Arch A

## ?

- Shared interface for serializing objects for network transport

## Lobby

Handles low level connections and disconnections.

- Features
  - Hosting
  - Connection
  - Tracking connected clients
  - Authenticating players connections
  - Initializing players

## Client

Represents a single connected player in the lobby. Contains only game agnostic information

- Features
  - IP
  - Ping
  - IsLocalPlayer()

## Spawning players

- Every client has a player character node
- All nodes need to be spawned across all instances BEFORE transfering authority to the client
- So,
  SERVER : Registers client & syncs client lists
  CLIENT : Updates client list (clients will need to be bound to their respective player nodes)
  SERVER : Requests spawn
  CLIENT : spawns player instances with correct authorities
  SERVER ? Server sets authority of the player nodes
  oh god this sucks

  LETS MAKE IT THE CLIENTS PROBLEM!

## NetworkedNode

Node that establishes a connection between Client and the Scene.
? I'm not sure how godot handles things but these are componenty?

For now, just holds a reference to the resepective client.
