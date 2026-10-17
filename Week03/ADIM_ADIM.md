# My step-by-step guide

At each stage, I read the script that changes, try it in Unity, and check which computer runs each part of the code.

1. **Open the project:** I add this repository's `Pong` folder through Unity Hub → Add project from disk, select Unity 6000.3.2f1, and wait for the packages to load.
2. **Understand the local game:** I open `Assets/Scenes/Pong-Start` and press Play. W/S moves the left paddle, and the arrow keys move the right paddle. `Court` builds the court, and `LocalBall` moves the ball.
3. **Open the networked scene:** I stop Play Mode and open `Assets/Scenes/Pong`. I check the NetworkManager, Hud, Court, and GameState objects.
4. **Start the server:** I press Play and select Start Server. The server keeps the authoritative game state.
5. **Connect the clients:** I open separate windows with Multiplayer Play Mode. If I give Player 2 the `Server` tag, ServerBootstrap starts the server automatically in that window, so I do not start another server. I enter `127.0.0.1` and select Start Client in the main window and the other two client windows.
6. **Check the roles:** The first player is the blue LEFT paddle, the second is the orange RIGHT paddle, and the third is an invisible SPECTATOR. W/S or the arrow keys only move my own paddle because of the `IsOwner` check.
7. **Compare movement:** I move my paddle and watch the other window. `NetworkTransform` sends the position from the owner to the server and then to the other clients.
8. **Serve:** LEFT serves first by pressing Space. RIGHT and the spectator cannot start the first serve. The client sends a request, and the server checks whose turn it is.
9. **Check a goal:** When the ball crosses the goal line, the score changes in every window. Everyone receives the goal notification, but only the player who conceded receives YOU SERVE. The ball waits in the center, colored gray, until the next serve.
10. **Reconnect:** I use Stop Client to disconnect one client and then connect again. The score should stay the same, and the last six events should appear. Stopping the server ends the match.
11. **Use a second physical computer:** For a hotspot test, I connect both computers to the same hotspot and enter the server's local IPv4 address in the other computer's HUD. I try both hosting directions and record the actual results in the connection card.
12. **Compare tick rates:** I stop Play Mode and set NetworkManager/TimeManager Tick Rate to 10. I start the game, wait 10 seconds during a rally, and read Server out B/s. I repeat at 30 and 60 with the same player count and conditions, then return the setting to 30.
13. **Prepare the submission:** I review my predictions, write the ownership paragraph for my game idea, and complete any entries still marked Pending or Not measured in the README. For the two-computer demo, I show serving, rallies, goals, and reconnection.

## How I explain the five scripts

- `ConnectionLogger` writes connection and disconnection messages to the Console.
- `PaddleController` manages the player's role and paddle movement; it also contains the three RPCs.
- `NetworkBall` moves the ball only on the server.
- `BallSpawner` creates one networked ball when the server starts.
- `GameState` stores the shared scores, serve turn, and last six events.

## My demo checklist

1. Connect two computers and show both paddles moving on both screens.
2. Show a serve, a rally, and a goal. Everyone should receive the score and goal notification, while only one player receives YOU SERVE.
3. Disconnect a player and reconnect with the score preserved.
4. Explain `IsOwner`, `SyncVar`, `ServerRpc`, `ObserversRpc`, and `TargetRpc`.
