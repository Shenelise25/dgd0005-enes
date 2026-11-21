#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using FishNet;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Opt-in host/guest integration check; keyboard events exercise the actual cheat handlers.
public class Homework3Probe : MonoBehaviour
{
    private Keyboard keyboard;
    private float started = -1, nextSample;
    private int stage;
    private bool captured;
    private bool serveRetried;
    private static readonly FieldInfo lastServeTime = typeof(PaddleController).GetField("_lastServeTime", BindingFlags.Instance | BindingFlags.NonPublic);
    private void Start()
    {
        if (Environment.GetCommandLineArgs().Contains("-hw3-relay")) Invoke(nameof(ConnectRelay),.2f);
    }
    private void ConnectRelay()
    {
        string[] args = Environment.GetCommandLineArgs();
        var hud = FindFirstObjectByType<ConnectionHud>();
        int code = Array.IndexOf(args,"-hw3-relay-code");
        if (code >= 0 && code + 1 < args.Length)
            typeof(ConnectionHud).GetField("_joinCode",BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud,args[code+1]);
        hud.SendMessage(code >= 0 ? "JoinViaRelay" : "HostViaRelay");
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Environment.GetCommandLineArgs().Contains("-hw3-test"))
            new GameObject("Homework3Probe").AddComponent<Homework3Probe>();
    }
    private void Update()
    {
        bool relay = Environment.GetCommandLineArgs().Contains("-hw3-relay");
        if (Time.time > (relay ? 60 : 30)) { Application.Quit(); return; }
        var manager = InstanceFinder.NetworkManager;
        var paddle = PaddleController.Local;
        var state = GameState.Instance;
        if (manager == null || paddle == null || state == null) return;
        if (started < 0)
        {
            started = Time.time;
            keyboard = InputSystem.AddDevice<Keyboard>();
            // Keep this opt-in process test independent of desktop keyboard focus.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices.OfType<Keyboard>().ToArray())
                if (device != keyboard) InputSystem.DisableDevice(device);
        }
        float t = Time.time - started;
        bool host = manager.IsServerStarted;
        if (!host && !serveRetried && t > 4.2f)
        {
            paddle.RequestServe();
            serveRetried = true;
            Debug.Log("HW3_ACTION guest serve after cooldown");
        }
        InputSystem.QueueStateEvent(keyboard, host && t > 10 && t < 10.06f ? new KeyboardState(Key.W) : new KeyboardState());
        float[] times = host ? new[] { 3.5f, 5f, 6f, 7f, 8f, 9f } : new[] { 2f, 3f, 4f, 5f, 6f };
        if (stage < times.Length && t > times[stage])
        {
            if (host)
            {
                if (stage == 0) { paddle.RequestServe(); Debug.Log("HW3_ACTION host serve"); }
                if (stage == 1) Press(Key.C);
                if (stage == 2) Press(Key.T);
                if (stage == 3) Press(Key.G);
                if (stage == 4) Press(Key.X);
                if (stage == 5)
                {
                    typeof(PaddleController).GetField("speed",BindingFlags.Instance | BindingFlags.NonPublic).SetValue(paddle,40f);
                    Debug.Log("HW3_ACTION host speed=40");
                }
            }
            else
            {
                if (stage == 0) Press(Key.G);
                if (stage == 1) { paddle.RequestServe(); paddle.RequestServe(); Debug.Log("HW3_ACTION guest double serve"); }
                if (stage == 2) Press(Key.T);
                if (stage == 3) Press(Key.C);
                if (stage == 4) Press(Key.X);
            }
            stage++;
        }
        if (Time.time > nextSample)
        {
            nextSample = Time.time + .05f;
            foreach (var p in PaddleController.All)
                Debug.Log(FormattableString.Invariant($"HW3_SAMPLE t={t:F3} tick={manager.TimeManager.Tick} serveTime={(double)lastServeTime.GetValue(p):F6} host={host} id={p.OwnerId} side={p.Side.Value} x={p.transform.position.x:F3} z={p.transform.position.z:F3} score={state.LeftScore.Value} rejects={p.RejectedInputs} rtt={manager.TimeManager.RoundTripTime} ball={NetworkBall.Instance.InPlay.Value}"));
        }
        string[] args = Environment.GetCommandLineArgs();
        int shot = Array.IndexOf(args,"-hw3-screenshot");
        if (!host && !captured && t > 9 && shot >= 0 && shot + 1 < args.Length)
        {
            ScreenCapture.CaptureScreenshot(args[shot+1]);
            captured = true;
        }
        if (t > (host ? (relay ? 30 : 18) : 14)) Application.Quit();
    }
    private void Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
        Debug.Log("HW3_ACTION " + key);
    }
    private void OnDestroy() { if (keyboard != null) InputSystem.RemoveDevice(keyboard); }
}
#endif
