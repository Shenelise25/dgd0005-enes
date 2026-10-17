# Section 14 lecture notes - my study answers

These are my notes for explaining the networking concepts in my own words during the demo.

1. **Why is there no shared "now"?** Information does not travel instantly through a cable. Distance, routing, and queues add delay. The lecture notes give an Istanbul–Frankfurt physical lower bound of 9.5 ms one way and 19 ms RTT, with a practical RTT of 35–50 ms. At 60 fps, one frame takes 16.67 ms. That makes the one-way physical limit about 0.57 frames and the RTT limit about 1.14 frames; practical RTT is about 2.1–3 frames. I need to keep RTT and one-way delay separate.

2. **Network topologies:**

| | Dedicated server | Listen server | Peer-to-peer |
|---|---|---|---|
| Trust | Independent server | Host player | Distributed trust |
| Cost | Server hosting | Player's computer | Players' computers |
| Latency | Travel to the server | Host advantage | Varies between peers |

In Step 5, the separate process with the Server tag acts as a dedicated server. If I run Start Server and Start Client in the same process for the two-computer setup, it becomes a listen server, or host. The server still controls the ball. A separate process running only Start Server keeps the dedicated-server role.

3. **UDP:** Waiting for an old position packet to be retransmitted can delay newer positions. FishNet adds reliability over UDP for messages that need it. The score SyncVar is reliable, while continuous position updates can use unreliable delivery.

4. **NetworkObject / NetworkBehaviour:** A NetworkObject gives an object its network identity, ownership, and spawn lifecycle. A NetworkBehaviour lets my script check network state and use SyncVars, RPCs, and network callbacks. The HUD is just a local interface, so MonoBehaviour is enough; it does not need a NetworkObject.

5. **Client 0's paddle with a separate server:**

| Process | IsOwner | IsServerInitialized | Callback | Reads keyboard input? |
|---|---|---|---|---|
| Server | false | true | OnStartServer | No |
| Client 0 | true | false | OnStartClient | Yes |
| Client 1 | false | false | OnStartClient | No |

On a host's own paddle, both server and client callbacks run. IsOwner and IsServerInitialized can both be true.

6. **What changed in Step 6?** I added NetworkTransform. The position goes from the owning client to the server and then to the other client. Client Authoritative is enabled on the paddle and disabled on the ball because the server simulates the ball.

7. **Why only one ball?** Court.Awake runs locally in every process. If Local Ball is enabled, every process creates its own ball. BallSpawner.OnStartServer runs only on the server, and the clients receive network copies from that single Spawn call. A normal client cannot use Spawn to create an authoritative network object; it needs to send a request to the server.

8. **State and events:**

| Information | Type | Tool |
|---|---|---|
| Score | State | SyncVar |
| LEFT scored | Event | ObserversRpc |
| Serve turn | State | SyncVar |
| YOU SERVE | Event | TargetRpc |
| Paddle hit sound | Event | ObserversRpc |
| Ball in play? | State | SyncVar |
| Ball position | Continuous state | NetworkTransform |

9. **Who decides whether a hit happened?** The server decides using its own paddle copies. Because of network delay, I might see the ball touch a paddle on a client while the server gets a different result. Writing to the InPlay SyncVar on a client does not change the server's authoritative state and does not replace a serve request.

10. **Traffic:** The example calculation is 8 × 30 × 40 = 9,600 B/s for one client, or 38,400 B/s for four clients. This only covers the assumed position payload, without protocol overhead, delta compression, or other messages. I expected 10 ticks to use less traffic and update the remote paddle and ball less often. My own paddle should feel the same because it moves locally every frame. My actual measurements are recorded in the [Week 03 README](README.md).
