# My verification results

Original test date: October 14, 2026. Unity 6000.3.2f1, Windows development build, Tugboat/UDP, and loopback connections on one computer.

## Completed checks

- My Unity C# compilation and Windows player build completed successfully.
- I connected a separate server and LEFT, RIGHT, and SPECTATOR processes through the real FishNet transport.
- The two players received different sides, and the third player received the spectator role.
- Paddle position changes from the owning client appeared on the server and the other clients.
- The server and each connected client had one ball copy. Only the server simulated the authoritative ball.
- The server rejected RIGHT's initial out-of-turn serve request. The ball moved when the correct player served.
- Goals and scores reached the server, both players, and the spectator.
- Both players received YOU SERVE when it was their turn. The spectator did not receive this private notification.
- The event feed stayed limited to six entries.
- RIGHT disconnected normally through ClientManager.StopConnection. The server logged the disconnection, and the reconnecting client received the available RIGHT role.
- The reconnecting client received the current non-zero score and six-event history.
- I found no runtime exceptions in the five process logs from this test.

The automated checker reported **11/11 checks passed**. Summary: [test-summary.json](test-summary.json).

In an additional test, a new connection also received the current score after I force-closed the application. The old connection was still considered connected until its timeout, so the new client joined as a spectator at that point. With the normal Stop Client flow, the side became available immediately.

## How I rerun the checks

1. In Unity, I run `PongReplicateSetup.BuildPlayer` or create a Windows development build at `Pong/Builds/Windows/Pong.exe`.
2. From the repository root, I run `python tests/run_smoke.py`. The processes close after about 40 seconds, and the logs are saved under `tests/logs/`.
3. I run `python tests/check_smoke.py`. Every check should be true, with exit code 0.

The probe supplies automatic test input only when I launch with `-smoke`. This automated test did not check manual keyboard input or window focus.

## Visual check

I checked the court, walls, center line, and connection interface in the Windows game window. I placed the camera view beside the connection panel so the panel would not cover the paddle. I left the black screenshot from a hidden or minimized window out of the submission files.

## Follow-up status

At the time of the October 14 report, the two-computer partner demo, 10/30/60 tick measurements, and prediction review were still pending. My later results are recorded in the [Week 03 README](README.md):

- I tested with my sibling on two physical computers, taking turns hosting. Both connection directions, serving, rallies, goals, and reconnecting with the current score worked.
- My recorded one-laptop tick measurements were approximately 510 B/s at 10 ticks, 1,370 B/s at 30 ticks, and 2,800 B/s at 60 ticks for server outgoing traffic.
- I marked all three of my predictions as right.

A direct comparison with the original course Tools files and the Asset Store 4.7.3R package is still pending because those files were not supplied.
