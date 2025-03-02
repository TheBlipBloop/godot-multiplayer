# Multiplayer Game Framework within Godot.
Let's make some tools around multiplayer development in Godot!

## Network Manager
We are going to need a Node to handle the following:
1) Hosting Servers
2) Joining Servers
3) Host Client-Server
3) Keeping track of connected players
4) 

### Feature Creep
4) Kicking / Banning players
5) Password Protection / connection validation

## Server
Maintain and synchronize variables to clients as needed. 

Send data to ALL clients.

Send data to a SINGLE client.

### FEATURE CREEP
* Players could express a level of interest (0-255) in other players that determins what players need updates.

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

Bit (un)packing utils