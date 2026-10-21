#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Linq;
using FishNet;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Opt-in integration probe: absent unless launched with -smoke.
// Exercises the real transport, NetworkTransform, RPCs and SyncTypes.
public class PongSmokeProbe : MonoBehaviour
{
    private float nextLog;
    private bool wrongServeSent;
    private float lastServe;
    private bool disconnected;
    private bool captured;
    private Keyboard testKeyboard;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Environment.GetCommandLineArgs().Contains("-smoke")) new GameObject("SmokeProbe").AddComponent<PongSmokeProbe>();
    }
    private void Update()
    {
        var manager = InstanceFinder.NetworkManager;
        var state = GameState.Instance;
        var ball = NetworkBall.Instance;
        var local = PaddleController.Local;
        if (manager == null) return;
        var args = Environment.GetCommandLineArgs();
        if (args.Contains("-drop-client") && Time.time > 12)
        {
            if (!disconnected) { manager.ClientManager.StopConnection(); disconnected = true; Debug.Log("SMOKE_GRACEFUL_DISCONNECT"); }
            if (Time.time > 13) Application.Quit();
            return;
        }
        int captureArg = Array.IndexOf(args,"-capture");
        if (!captured && captureArg >= 0 && captureArg+1 < args.Length && Time.time > 10)
        { ScreenCapture.CaptureScreenshot(args[captureArg+1]); captured = true; }
        if (local != null && local.IsOwner && local.Side.Value < 2)
        {
            // Exercise owner input; the server is now the only movement writer.
            float testZ = ball != null && ball.transform.position.z < 0 ? 3.8f : -3.8f;
            if (testKeyboard == null) testKeyboard = InputSystem.AddDevice<Keyboard>();
            float distance = testZ - local.transform.position.z;
            InputSystem.QueueStateEvent(testKeyboard, Mathf.Abs(distance) < .2f ? new KeyboardState()
                : new KeyboardState(distance > 0 ? Key.W : Key.S));
            if (state != null && ball != null)
            {
                if (local.Side.Value == 1 && Time.time < 5 && !wrongServeSent)
                { local.RequestServe(); wrongServeSent = true; Debug.Log("SMOKE_WRONG_SERVE_SENT"); }
                if (Time.time > 6 && local.Side.Value == state.ServingSide.Value && !ball.InPlay.Value && Time.time-lastServe > 1)
                { local.RequestServe(); lastServe = Time.time; }
            }
        }
        if (Time.time >= nextLog && state != null)
        {
            nextLog = Time.time+1;
            string paddles = string.Join(";",PaddleController.All.Select(p=>$"{p.Side.Value}:{p.transform.position.z:F2}"));
            Debug.Log($"SMOKE t={Time.time:F1} server={manager.IsServerStarted} local={(local == null ? -1 : local.Side.Value)} score={state.LeftScore.Value}:{state.RightScore.Value} serve={state.ServingSide.Value} balls={FindObjectsByType<NetworkBall>(FindObjectsSortMode.None).Length} inplay={(ball != null && ball.InPlay.Value)} feed={state.Feed.Count} paddles={paddles}");
        }
        if (Time.time > 45) Application.Quit();
    }
    private void OnDestroy() { if (testKeyboard != null) InputSystem.RemoveDevice(testKeyboard); }
}
#endif
