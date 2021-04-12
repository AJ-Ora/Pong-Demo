# Pong Demo
A demo project for a simple UDP Pong game.

# How to build
Client: Build the project as you would normally build a Windows/Mac/Linux executable.

Server: Build the project with "Server Build" enabled in Build Settings window.

# How to create a server
Double click on the fresh server build executable, and that's it!
The server will always try to open at UDP port 42069,
but tries higher port numbers if it's already taken.

# How to connect to a server
Double click on the fresh client build executable,
and enter IP address and port number at top left.

If you don't see two paddles, a ball and a scoreboard,
you've probably entered wrong IP address and/or port,
or the server is behind a firewall.
