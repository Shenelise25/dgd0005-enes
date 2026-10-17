# DGD0005 - Enes - Week 03 Pong

I built this networked Pong project with Unity **6000.3.2f1**, the Universal 3D template, and FishNet's official **4.7.3** tag.

## Ownership in my game

Each player owns their paddle. Only the owner reads keyboard input, and the paddle's client-authoritative NetworkTransform sends its position to the server. The ball has no player owner: the server spawns it and controls its movement. The scores, serve turn, and last six events are shared state. Only the server changes these values, and SyncVars and a SyncList keep the clients updated.

## Starting the game

1. Open the `Pong` folder in Unity Hub with Unity 6000.3.2f1, open `Assets/Scenes/Pong.unity`, and press Play. In the server window, select **Start Server**.
2. In each player window, enter the server's IP address and select **Start Client**. For windows on the same computer, use `127.0.0.1` and port `7770`.
3. Move with **W/S or the arrow keys**. Press **Space** when it is your turn to serve. For a separate server in Multiplayer Play Mode, give Player 2 the `Server` tag.

## Project notes

- [My step-by-step guide](Week03/ADIM_ADIM.md)
- [Homework answers, design sheet, and experiment notes](Week03/README.md)
- [My Section 14 study answers](Week03/SELF_REVIEW.md)
- [My test results and verification status](Week03/TEST_RESULTS.md)

## Files

The five scripts in `Pong/Assets/Scripts/Network/` contain the networking logic for this assignment. `Starter/` contains the supplied local Pong scripts, with changes to Court.Paint for headless execution and shader availability in builds. The HUD and bootstrap in `Tools/` replace the course files that were not supplied.

`Assets/Editor/` contains helpers for creating scenes and prefabs and building a Windows development player. `Assets/Tests/PongSmokeProbe.cs` runs only in a development build with the `-smoke` argument. During normal gameplay, it does not move the paddles or serve automatically.

`Pong-Start.unity` is the local game, and `Pong.unity` is the networked scene. Open the existing scenes directly. The `Pong/Build Connect Scenes` menu resets the scenes to the initial connection stage.

## Versions and setup

- FishNet: [official 4.7.3 tag](https://github.com/FirstGearGames/FishNet/tree/4.7.3), commit `73f30cf2425dc808a4f463f0a233d386010810b3`. The tag's package.json and version constant still say 4.7.2; these upstream values are unchanged. A direct comparison with the course's Asset Store 4.7.3R package is still pending.
- Input System: **1.17.0**. The older version from the template did not compile with Unity 6000.3.2f1.
- Multiplayer Play Mode: **1.6.2**. Other exact package versions are listed in `Packages/packages-lock.json`.
- The original `Tools` folder and `Pong-Start.unitypackage` were not supplied, so I recreated the scene, HUD, and bootstrap.
- `Library`, `Temp`, `Logs`, and `Builds` are excluded from Git. The Windows build can be generated at `Pong/Builds/Windows/Pong.exe`.

## Testing status

My test report records a successful Windows development build and **11/11 automated loopback checks** on one computer. These checks cover connections, paddle replication, server-controlled ball movement, serve permissions, scores, the event feed, and reconnection with the current score.

My one-laptop tick-rate results were approximately **510 B/s at 10 ticks**, **1,370 B/s at 30**, and **2,800 B/s at 60** for server outgoing traffic. The ball looked jerky at 10 and smooth at 30 and 60. I chose **30 ticks per second** because the visible gain at 60 was small while outgoing traffic roughly doubled. I rated my three predictions as right. The full setup and results are in [the Week 03 README](Week03/README.md).

I also tested the game with my sibling on **two physical computers**. We took turns hosting, and both connection directions worked without problems. Serving, rallies, goals, and reconnecting worked, and the current score appeared correctly after reconnecting.

The following details are not documented:

- The two-computer test's network type, server IP addresses, ClientIds, sides, and RTT readings.
- Manual keyboard input and window-focus checks, which the automated smoke test did not cover.

The loopback checks, one-laptop tick-rate measurements, and two-computer gameplay test are recorded separately. See [the Week 03 README](Week03/README.md) for the results and remaining details.
