# Week 5 — Pong on a host

Based on `Week05_In-Class-Assignment.pdf` and this repository's Week03/Week04 Pong. The accompanying Week05 lecture notes were not supplied. **Implementation and source review are complete; Week05 Unity gameplay, timing measurements, and partner LAN tests have not been run.** All expected results below are test criteria, not recorded observations.

## Implementation

- `ConnectionHud.OnGUI`: Start Host appears only when both transport states are `Stopped`. It starts the server, then the local client on `127.0.0.1` using the transport's configured port. Stop Host appears only when both states are `Started`; it stops the client before the server. Separate server/client controls remain available.
- `ConnectionLogger.ClientState`: prints `[Host] This process is the server AND a client. RTT is 0 ms.` when the client reaches `Started` and `ServerManager.Started` is true. This assignment-required message describes local host operation; it is not a measured RTT result.
- `PaddleController`: both start callbacks call `Register()`, which already guarded `All.Add(this)` with `if (!All.Contains(this))`. The guard is retained. OnStopClient retains registry entries while the server half is still initialized, so stopping only the local client does not erase server paddle tracking. Logs now include ObjectId; the HUD always displays `PaddleController.All.Count`.
- `NetworkBall.ServerPlayersChanged`: when a player leaves while the server is still running, resets the ball and transfers serve to a remaining player if the serving side is empty. A spectator leaving does not reset a rally. This closes a gap in the Week04 baseline required by Step 6.
- Week04 server-authoritative paddle movement, input validation, RPC ownership and score authority are retained. No Mirror or Netcode APIs are introduced.

## Setup check

| Item | Source review / remaining check |
| --- | --- |
| Unity | `Pong/ProjectSettings/ProjectVersion.txt`: 6000.3.2f1 |
| FishNet | Existing official 4.7.3 tag; repository documents that upstream version metadata says 4.7.2. Exact equivalence to course Asset Store 4.7.3R remains unverified. |
| Multiplayer Play Mode | Package 1.6.2 is present. |
| Start On Headless | Off in `Assets/Scenes/Pong.unity`. |
| Run In Background | On in `Assets/Scenes/Pong.unity`. |
| Tick Rate | 30 in `Assets/Scenes/Pong.unity`. Restore to 30 after the 10-tick experiment. |
| Port | Existing transport uses 7770; HUD reads the configured port for both host and guest. |
| Server tag | Remove it from guest Play Mode instances. Current tracked PlayerTags is empty. |
| Library | Ignored; not tracked. |
| Homework 1 baseline | Historical tests are in Week03; a fresh Week05 compile and rally are still required. |

Open the existing `Pong/Assets/Scenes/Pong.unity`. Do not run **Build Connect Scenes**, which resets scenes. Use W/S or arrow keys to move and Space to serve. LEFT serves first. The third connected client is a spectator.

## Steps 0–8 — procedure and evidence

### Step 0 — compile the existing Pong

Open `Pong` in Unity 6000.3.2f1. Start Server in the main editor; start a client in another instance. Check no compile/runtime errors, one ball, HUD score, serve and feed. Complete a rally, then stop everything.

Status: **not run for Week05**. Historical Week03/Week04 results do not establish that the modified Week05 project compiles.

### Step 1 — two buttons, one host

In the main editor only, Start Server then Start Client at 127.0.0.1. Expect a local LEFT paddle and one registered paddle. The server and client share one process. Check server/client connection logs, local ClientId, and actual HUD RTT. Stop both.

Status: **not run**. If no local client starts, check port, loopback address and Start On Headless. If no paddle spawns, inspect PlayerSpawner and the prefab list.

### Step 2 — one-click host

Click Start Host. Expect server/client logs plus the `[Host]` line, one LEFT paddle, and All.Count = 1. Click Stop Host and wait for both states to become Stopped. Start Server alone: expect no local owned paddle and no keyboard-controlled player. Stop it and Start Host again.

Status: **implemented; runtime test pending**. The host condition is checked in the client-state callback, after the server is running.

### Step 3 — both callbacks, one paddle

Both OnStartServer and OnStartClient call the same guarded Register method. On a host the same NetworkObject runs both callbacks; it must enter All only once. The server assigns a side and the owner client sets Local. Log messages include the same ObjectId, allowing the two halves to be matched.

Expected order: **OnStartServer first (server half), then OnStartClient (client half)**. This is a source/framework expectation, not a captured Console observation.

| Required observation | Actual evidence |
| --- | --- |
| First callback and half | Pending: paste server/client log lines in order |
| ObjectId in both lines | Pending |
| Host alone, All.Count | Pending; expected 1 |
| Host + guest, All.Count on host | Pending; expected 2 |

### Step 4 — guest joins

Activate one Multiplayer Play Mode instance and remove its Server tag. Main editor: Start Host. Guest: Start Client on 127.0.0.1. Expect the next client to take RIGHT, host LEFT to serve first, and the same changing score in both windows. Use HUD ClientId rather than assuming it will always be 1 after reconnects.

Status: **not run**. Guest RTT must be measured; do not invent an above-zero sample. Only owners read keyboard input; the server moves both paddles.

### Step 5 — host advantage and tick experiment

Play five rallies as host + guest. Add a third connection and observe as spectator. Compare serve delay and ball/paddle correction. At 30 ticks, record guest RTT. Change TimeManager Tick Rate to 10, play another rally and describe what the guest actually sees. Stop sessions before changing settings; restore 30 and repeat a short check.

| Observation | Actual result |
| --- | --- |
| Guest RTT at 30 ticks | Not measured |
| Five rallies: host vs guest ball contact | Not observed |
| Host Space vs guest Space delay | Not observed |
| Spectator view | Not observed |
| Guest RTT and ball motion at 10 ticks | Not measured / not observed |
| Tick Rate restored to 30 | Source is 30; verify after the experiment |

Expected explanation: the host's RPC executes locally on the server. A remote guest's input must reach the server, and resulting movement must return. Lower tick rates increase the interval between updates (about 100 ms at 10 versus 33.3 ms at 30). Corrections/interpolation may become more visible. A local test may not visibly show a ball crossing a paddle, so record actual behavior.

**Week04 difference:** this project already uses server-authoritative paddles. Guests also wait for authoritative paddle movement; the host has no network round trip for its own input. The PDF's client-authoritative baseline should not be described as the current implementation.

### Step 6 — host leaves / guest leaves

Mid-rally, Stop Host. Check guest disconnection, removal of network-spawned objects and loss of live replicated game state. This is an orderly stop; notification can arrive promptly. A timeout is expected particularly after abrupt host/network loss and is not guaranteed for Stop Host. The HUD shell or scene objects can remain; the important check is that the old live match no longer continues.

Restart host and reconnect guest. Verify a fresh match at 0 : 0; record actual ClientIds. IDs may be reused after restart, so a different numeric ID is not a required invariant.

Then test guest Stop Client. Host must stay connected, RIGHT must despawn, ball must reset, and serve must be available to the remaining LEFT player. Score is preserved for the still-running match. Also disconnect a spectator: the rally should continue.

Status: **not run**. Player departure now explicitly resets the ball and handles an empty serving side. Host restart, scene SyncVar reset and static-list cleanup still require Unity verification.

### Step 7 — connection cards (both roles)

Use two real computers on a reachable LAN. Find the host's LAN IPv4 address (Windows: `ipconfig`; Linux: `ip -4 addr`), not 127.0.0.1. Host Start Host, guest enters that LAN address and starts client. Play three rallies, then swap roles. Read guest RTT; host RTT does not measure the remote connection.

| Field | I host | I join |
| --- | --- | --- |
| Partner's name | To be filled after test | To be filled after test |
| Host's LAN address | To be filled | To be filled |
| My ClientId | To be measured | To be measured |
| My RTT (ms) | To be measured | To be measured |
| Guest RTT (ms) | To be measured | To be measured |
| Connected first try? | Not tested | Not tested |
| Fix used, if any | Not tested | Not tested |
| Three rallies and movement on both machines | Not tested | Not tested |

Troubleshooting: confirm host `[Server] Started`, correct LAN address and matching port first. For cross-machine failures check the host's firewall/application rule and network isolation; try wired LAN or a shared hotspot if campus Wi-Fi isolates clients. Do not disable the firewall globally.

Earlier Week03 documentation mentions a sibling two-computer test, but it provides no Week05 partner card or RTT measurements. Those historical results are not reused as Week05 evidence.

### Step 8 — same scripts, three ways

1. **Start Server only:** ConnectionHud, ConnectionLogger and ServerBootstrap exist; server callbacks spawn ball/paddles, simulate ball/paddles, validate RPCs and write score/serve/feed. Without a local client no local owner reads the keyboard; OnStartClient does not run in this process.
2. **Start Host:** server and client callbacks run on the same network object. This does not mean every method runs twice: Update still runs once per component per frame, and OnStartNetwork registers OnTick once. Guarding Register prevents duplicate list entries. The owner half reads input; the server half simulates the match.
3. **Start Client:** the guest receives replicated state and sends permitted RPCs. It knows the address, port, its ClientId/side and RTT. Ordinary gameplay does not tell it that the server also has a local player; it has no authority over server scores or ball movement.

## Self-review

### 1. Which two calls start a host? Why is RTT 0?

`manager.ServerManager.StartConnection();` followed by `manager.ClientManager.StartConnection("127.0.0.1", port);`, where port comes from the configured transport. The local client connects to its own process and has no remote-machine network round trip. The assignment models/reports that local host path as 0 ms. The HUD still displays the runtime's actual RoundTripTime; the log is not a measurement of the guest's connection.

### 2. The guest's paddle on host, guest and spectator

Assume the normal guest owner with no ownership transfer:

| Process | IsOwner | IsServerInitialized | Start callbacks on that paddle | Reads its keyboard? |
| --- | --- | --- | --- | --- |
| Host | false | true | OnStartNetwork once; OnStartServer and OnStartClient | No |
| Guest owning this paddle | true | false | OnStartNetwork; OnStartClient | Yes, owner guards in Update and OnTick |
| Separate spectator | false | false | OnStartNetwork; OnStartClient | No |

The host reads keys for its own LEFT paddle, not for the guest's RIGHT paddle. Its server OnTick still moves the guest's authoritative paddle from received input. The spectator owns its own third paddle object, but Side == 2 blocks movement/serve and hides it.

### 3. Trace both serves

Host: Space -> owner Update -> RequestServe ServerRpc, executed locally on server -> validate Side/ServingSide/InPlay -> NetworkBall.Serve -> authoritative velocity/InPlay -> ball moves in server Update -> replication to observers.

Guest: Space -> owner Update -> RequestServe ServerRpc sent to host -> same server validation -> NetworkBall.Serve -> server moves ball -> replication back to guest/spectator. The guest waits for outbound input plus the return update; about a round trip plus scheduling/interpolation, not an exact RTT stopwatch. Both calls are rejected if it is not the caller's turn or a ball is already in play.

### 4. Why does the guest paddle vanish when the host stops?

The server owns the network object's lifetime even when a guest owns its input/RPC rights. Host shutdown ends the authoritative session and the client cleans up its spawned objects. Guest shutdown only removes that connection and its owned objects; the host still has the authority, ball, score and remaining player. The departure handler resets the rally without stopping the host server.

### 5. State or event?

| Item | Classification and implementation |
| --- | --- |
| Score | State: LeftScore and RightScore SyncVars |
| "RIGHT scored!" | Event: AnnounceGoal ObserversRpc flash; it is also stored as text in feed history |
| Whose serve | State: ServingSide SyncVar |
| "YOU SERVE" | Targeted event: YouServe TargetRpc |
| Ball position | State: authoritative transform replicated by NetworkTransform |
| Feed | State: bounded SyncList of the last six event descriptions; additions are events, the stored history is state |

### 6. A guest assigns LeftScore.Value = 9

In this project's initialized, default server-write SyncVar, FishNet's CanNetworkSetValues check rejects an unauthorized standalone client's write. It does not change the server's authoritative score or broadcast a valid score update. The same line on the host can work because the object is also server-initialized in that process and has server write permission. This does not justify accepting arbitrary scores from guest RPC arguments. Before network initialization or with different SyncType permissions behavior can differ; neither is this question's active default setup.

### 7. Feed twice, goal sound twice, score log once

For a SyncList/SyncVar change callback that fires for both halves of a host, put presentation on the client half, e.g. `if (!asServer) PlayGoalSound();` (or return early when asServer is true). The server and client callback describe one authoritative change; playing sound in both would duplicate it on the host.

**Actual repository:** GameState has no Feed.OnChange sound handler and contains no goal audio. Goal feedback is an ObserversRpc flash. The score log is written by server-only `GameState.Goal()` -> `Announce()` once, not by two SyncVar callbacks. Feed is rendered as state by the HUD. The PDF's duplicate feed callback scenario is therefore a conceptual answer, not an observed bug in this code. If audio is added to AnnounceGoal, play it once in that RPC's receiving presentation path; do not also play it on a separate server feed callback.

## Validation and submission status

- Source/API review: the included FishNet transport supports GetConnectionState(bool) and GetPort(); ClientManager.StartConnection(string, ushort) and ServerManager.Started are present.
- `git diff --check`: run on the prepared change, no whitespace errors.
- Checked tracked Unity settings, package manifest, ownership guards, server RPCs and existing list guard.
- Unity compilation, Play Mode, actual HUD behavior, two-client/spectator gameplay and LAN checks: **not run**. This execution environment has no Unity Editor/player or C# compiler installed.
- Existing Windows-only smoke scripts and historical evidence are not Week05 runtime validation.
- No remote push performed. The PDF's push instruction is part of the assignment, not a separate request to publish changes to the user's GitHub repository.

After completing the manual evidence above, review the changes and commit from the repository root:

```sh
git add Pong/Assets/Scripts/Tools/ConnectionHud.cs Pong/Assets/Scripts/Network/ConnectionLogger.cs Pong/Assets/Scripts/Network/PaddleController.cs Pong/Assets/Scripts/Network/NetworkBall.cs Week05/README.md README.md
git commit -m "Week 5: Pong on a host + connection card"
git push
```

Before pushing, check that no Library directory is tracked. Homework 2's server-authoritative implementation is retained; test it from a guest when running with Start Host.
