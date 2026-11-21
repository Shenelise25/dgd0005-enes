# Homework 3 — The Host Cheats, and the Join Code

I continued my Homework 2 Pong project in Unity 6000.3.2f1 with FishNet 4.7.3.
This report separates the implemented behavior from tests I have actually recorded.

## Before starting

- Server-authoritative paddle NetworkTransform; 30 ms input limit; C, X and T retained.
- Start Host starts the server and its local client. Stop Host stops both.
- For Multiplayer Play Mode, use one guest instance without the `Server` tag.
- Week 6 lecture notes were not supplied, so the Relay implementation follows the package API and the homework requirements.

## Step 1 — Guest cheats

I added G as a direct `LeftScore.Value = 9` write, without an RPC.
A guest cannot publish a SyncVar write to the server: the server owns the shared score and sends updates to clients.
A local write cannot become an authoritative score change.

| Key on guest | Expected host result | Expected visible result |
| --- | --- | --- |
| C | Burst input rejected by the 30 ms limit | Normal paddle speed |
| X | Axis clamped to [-1, 1] | Normal paddle speed |
| T | No server position change | Guest-only jump, then server correction |
| G | No server score change | Host score unchanged |

Exactly 19 C rejections assumes no normal tick input was accepted in the same tick; otherwise all 20 burst requests can be refused.

## Step 2 — Serve rate limit

I keep a separate timestamp on each paddle. After validating the state, ball and side, I check one second before checking the serving rule.
I update the timestamp for every well-formed attempt, including a wrong-turn request. A refused burst sends `TOO FAST` only to `Owner` through a TargetRpc.
After one second the next request reaches the serving-rule check again; it can serve only on that player's turn with an idle ball.

## Step 3 — Host cheats

1. C and X pass through checked requests; T and G directly edit the authoritative objects when run on the host.
2. The guest can see the host's T teleport, G score edit and Inspector speed change after replication.
3. For ranked games with strangers I would choose a dedicated server, because a player-host controls the authoritative process and its rules.

| Host action | Expected result |
| --- | --- |
| C | Input rate limit still applies |
| X | Axis clamp still applies |
| T | Teleport reaches both windows |
| G | LEFT becomes 9 in both windows |
| Speed 8 → 40 | Host paddle moves five times faster, replicated to guest |

Guest screenshot after host G: not captured yet.

## Step 4 — Packages and transport

UnityTransport is the active transport on the NetworkManager, with direct connections on port 7770. Tugboat stays on the object unused.
The manifest installs Multiplayer Services and the homework's FishyUnityTransport Git package.
Unity Cloud project linking and enabling Relay must be completed in Project Settings → Services and the project's Relay dashboard.
The repository currently has no linked cloud project ID.

## Step 5 — RelayLink

- `CreateAllocationAsync(1)` returns an allocation containing Relay endpoint and connection data for one guest plus the host.
- `GetJoinCodeAsync(allocation.AllocationId)` returns the shareable join-code string for that allocation.
- I set Relay server data before `StartConnection` so the transport starts with the allocated endpoint, credentials and Relay protocol.

## Step 6 — Two buttons and a code

I added Host via Relay, a join-code field and Join via Relay. While a request is busy, connection buttons are disabled.
The host displays JOIN CODE while its server is running. Stop Host clears the code.
Initialization uses a distinct anonymous authentication profile per process so host and guest on one PC do not reuse the same player.
Relay errors appear in the Console and on the initiating HUD; failures release the busy flag.

## Step 7 — Partner on another network

1. Host and guest each connect to a public Relay endpoint, which forwards the game packets between them; authority remains on the host.
2. Both devices make outgoing connections, so neither player needs to configure router port forwarding.
3. The extra delay comes from the longer network route through Relay and its forwarding, compared with loopback on one computer.

| Connection card | Recorded value |
| --- | --- |
| Partner's name | Not supplied |
| Host / guest | Not tested across two networks |
| Two networks | Not recorded |
| Guest Relay RTT | Not measured |
| Guest loopback RTT | Not measured for this homework |
| First try / fix line | Not tested |
| Play to 3 and swap hosts | Pending real partner test |
| Guest screenshot with RTT and score | Not captured |

I must use the same cloud project for each host's code: my partner runs my project when I host, and I run theirs when they host.
A join code expires when its host stops.

## Vocabulary and pointed-at line

- **Listen server:** a player process running both the authoritative server and a local client.
- **Relay allocation:** reserved forwarding capacity and connection data for a host and its guests.
- Line I can explain: `TargetRefused(Owner, "TOO FAST");` targets only the requesting paddle's owning connection, leaving the other player's HUD unchanged.

## Verification

Verification results will be recorded after running the checks. Expected behavior above is not a claim of a completed manual test.

## Sources

- [FishyUnityTransport API and Relay setup](https://github.com/ooonush/FishyUnityTransport)
- [Unity Relay setup](https://docs.unity.com/ugs/en-us/manual/relay/manual/get-started)
