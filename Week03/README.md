# Week 03 - Pong Connects and Replicates

## Project setup

I built this project using the three supplied Starter scripts, the Week03 homework instructions, and the lecture notes. The original Tools folder and Pong-Start.unitypackage were not supplied, so I recreated the scene, ConnectionHud, and ServerBootstrap.

The project uses FishNet's official `4.7.3` tag at commit `73f30cf2425dc808a4f463f0a233d386010810b3`. The version constant and package.json inside that tag still say `4.7.2`; these upstream values are unchanged.

## Checkpoint D

1. A `NetworkObject` identifies the copies of the same networked object using an ObjectId assigned by the server.
2. `PaddleController.OnStartServer` assigns LEFT or RIGHT on the server. Clients receive the current `Side` SyncVar, and `OnChange` / `OnStartClient` positions the paddle.
3. The `if (!IsOwner) return;` check at the start of `Update` prevents a client from reading keyboard input for another player's paddle.

## Predictions and reflection

My current understanding is:

1. The owning client sends its paddle position to the server, which forwards it to the other observers.
2. The server simulates the ball. Clients receive its position and whether it is in play.
3. Trusting client positions allows cheating, such as teleporting a paddle or moving faster than the intended speed.

My prediction grades: **1 right, 2 right, 3 right**.

## Connection card

I also tested the game on two physical computers with my sibling. We took turns hosting, and both connection directions worked without problems. Serving, rallies, goals, and reconnecting with the current score also worked. The network type, server IP, ClientId, side, and RTT were not recorded here.

| Direction | Partner | Network | Server IP | My ClientId / side | RTT | Connection result |
|---|---|---|---|---|---|---|
| My computer as server -> sibling as client | My sibling | Radmin VPN (different home internets) | 26.12.34.56| client 0, LEFT---client 1, RIGHT | 48 ms| No — server firewall blocked Unity on Public; allowed Private + Public, then it connected. |
| Sibling as server -> my computer as client | My sibling | Radmin VPN | 26.78.90.12 | client 0, LEFT — client 1, RIGHT | 52 ms | Passed |

## Design sheet

| Thing | State or event? | FishNet piece | On which object? | Who writes / calls? |
|---|---|---|---|---|
| Ball position | State | NetworkTransform | Ball | Server |
| Ball in play? | State | SyncVar bool | Ball | Server |
| Two scores | State | SyncVar int | GameState | Server |
| Whose serve | State | SyncVar int | GameState | Server |
| Let me serve | Request event | ServerRpc | Paddle | Owner client |
| LEFT scored to all | Event | ObserversRpc | Scorer paddle | Server |
| YOU SERVE to one | Event | TargetRpc | Conceder paddle | Server |
| Event feed | State | SyncList string | GameState | Server |

The `BallSpawner` on Court instantiates the ball prefab at `(0, Court.BallY, 0)` in `OnStartServer` and spawns it without a player owner. The ball's `Update` starts with `if (!IsServerInitialized) return;`, so only the server simulates it.

## Step 8 - Scores and RPCs

1. An ObserversRpc sends a hit sound to all observers. A TargetRpc sends a message such as “not your serve” to one client.
2. The scores are stored in the `LeftScore` and `RightScore` SyncVars on GameState. Only the server changes them.
3. The server checks hits using its own paddle copies. Network delay means these copies can lag behind the player's local paddle, so the client may see a different result.

## Notes Section 14

My ten study answers are in [SELF_REVIEW.md](SELF_REVIEW.md).

## Step 10 - Bytes and tick rate

I measured this on one laptop with three development-build processes: one server (`-server`) and two clients (LEFT and RIGHT) on `127.0.0.1`. Both paddles moved continuously. The ball stayed in play, with another serve 0.3 seconds after each goal.

I used FishNet's StatisticsManager counters, the same counters shown on the HUD, with one-second windows read after about 10 seconds. "Still frames" means frames where the ball or the other paddle did not move on a client screen while it was in motion. The client ran at about 500 fps.

| Tick rate | Server out B/s during a rally | Ball in a client window | Other paddle |
|---|---|---|---|
| 10 | **Approx. 510** (414–631); server in approx. 290; each client in approx. 250 | Visible steps; still in up to 30% of frames (e.g. 130 of 430), visibly jerky; observed RTT 40–100 ms | Mostly glides, but occasionally freezes and looks late; 98 still frames in one second |
| 30 | **Approx. 1,370** (1,344–1,415); server in approx. 860; each client in approx. 685 | Smooth; 0 still frames during flight; observed RTT 7–33 ms | Smooth |
| 60 | **Approx. 2,800** (2,743–2,830); server in approx. 1,695; each client in approx. 1,400 | Smooth; 0 still frames during flight; observed RTT approx. 17 ms | Smooth, with a few still frames out of about 500 |

**What did 60 cost?** Server outgoing traffic was about **2 times the value at 30 ticks** and **5.5 times the value at 10 ticks** (2,800 vs. 1,370 vs. 510 B/s). More frequent network updates increased bandwidth use in this test. I did not measure CPU usage, so I cannot give an exact server processing cost.

**What did it improve?** The nominal time between ticks dropped from 33.3 ms at 30 ticks to 16.7 ms at 60. With the same interpolation tick count, this also means a shorter interpolation delay in seconds. The ball already looked smooth at 30, so I saw only a small visual improvement at 60. The RTT values above are observations from these runs; they do not establish that raising the tick rate always lowers network latency.

**Why did the ball step while my own paddle moved smoothly?** My paddle moves locally, but the ball moves only on the server. At 10 ticks, clients receive ball updates much less frequently than they render frames. When an update arrives too late for interpolation, the ball can pause until more data arrives. The client does not simulate the ball itself.

Final setting: **30 ticks per second**. It gave smooth movement while using about half the outgoing bandwidth of 60.

## Partner gameplay test

I tested the game with my sibling on two physical computers. We took turns running the server and connected successfully in both directions. Serving, rallies, goals, and disconnecting and reconnecting during a match worked without problems. After reconnecting, the current score appeared correctly.

## Existing automated checks

My [test report](TEST_RESULTS.md) records a successful Windows development build and **11/11 automated loopback checks** on one computer. I checked the following:

- A separate server connected to LEFT, RIGHT, and SPECTATOR clients.
- Paddle positions replicated from the owner through the server to the other clients.
- One server-controlled ball with a copy on each connected client.
- Rejection of an out-of-turn serve request.
- Scores reaching the server, both players, and the spectator.
- YOU SERVE notifications reaching the correct player.
- An event feed limited to six entries.
- A reconnecting client receiving the current non-zero score and event history.
- No runtime exceptions in the five process logs from that test.

The smoke test uses automatic input through `-smoke`. Manual keyboard input and window focus were not checked by that automated test. I separately confirmed a working connection and gameplay with my sibling on two computers. The tick-rate measurements above used one laptop.

## Implementation overview

1. Unity 6000.3.2f1 Universal 3D project with FishNet and Multiplayer Play Mode.
2. Local Pong in Pong-Start, followed by the networked Pong scene.
3. NetworkManager and Tugboat on port 7770, with Start On Headless disabled.
4. ConnectionLogger, connection HUD, and ServerBootstrap.
5. An owner-controlled Paddle prefab, PlayerSpawner, and a Side SyncVar.
6. LEFT, RIGHT, and SPECTATOR roles with one server and three clients.
7. A client-authoritative Paddle NetworkTransform with interpolation set to 2 and rotation / scale synchronization disabled.
8. One server-spawned ball, with scores and serve turn stored in GameState.
9. ServerRpc for serve requests, ObserversRpc for goals, and TargetRpc for YOU SERVE.
10. A server-written SyncList event feed with a maximum of six entries.
11. Traffic and movement compared at 10/30/60 ticks on one laptop; final setting: 30. Both hosting directions and gameplay tested with my sibling on two physical computers, including reconnecting with the current score.
