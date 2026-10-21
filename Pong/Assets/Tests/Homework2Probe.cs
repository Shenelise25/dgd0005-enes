#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using FishNet;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Opt-in real-transport test. Reflection calls the RPC wrapper, never RpcLogic.
public class Homework2Probe : MonoBehaviour
{
    private PaddleController local;
    private Keyboard keyboard;
    private float started, nextSample;
    private int stage;
    private bool detached;
    private static readonly MethodInfo setAxis = typeof(PaddleController).GetMethod("ServerSetAxis", BindingFlags.Instance | BindingFlags.NonPublic);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Environment.GetCommandLineArgs().Contains("-hw2-test"))
            new GameObject("Homework2Probe").AddComponent<Homework2Probe>();
    }
    private void Send(float axis) => setAxis.Invoke(local, new object[] { axis });
    private void Update()
    {
        var manager = InstanceFinder.NetworkManager;
        if (manager == null) return;
        if (local == null && PaddleController.Local != null)
        {
            local = PaddleController.Local;
            started = Time.time;
            keyboard = InputSystem.AddDevice<Keyboard>();
        }
        if (local != null)
        {
            float t = Time.time - started;
            // Exercise normal owner OnTick input first, including key release.
            InputSystem.QueueStateEvent(keyboard, t > .2f && t < .4f ? new KeyboardState(Key.S)
                : local.Side.Value == 1 && t > 2 && t < 4 ? new KeyboardState(Key.W) : new KeyboardState());
            if (local.Side.Value == 0)
            {
                // Quiet the normal sender only during isolated cheat measurements.
                if (t > 2 && !detached)
                {
                    var method = typeof(PaddleController).GetMethod("OnTick", BindingFlags.Instance | BindingFlags.NonPublic);
                    manager.TimeManager.OnTick -= (Action)Delegate.CreateDelegate(typeof(Action), local, method);
                    detached = true;
                }
                if (stage == 0 && t > 3) { for (int i=0;i<20;i++) Send(1); stage++; Debug.Log("HW2_ACTION C 20"); }
                if (stage == 1 && t > 3.2f) { Send(0); stage++; }
                if (stage == 2 && t > 4) { Send(50); stage++; Debug.Log("HW2_ACTION X 50"); }
                if (stage == 3 && t > 4.2f) { Send(0); stage++; }
                if (stage == 4 && t > 5)
                {
                    local.transform.position = NetworkBall.Instance.transform.position;
                    Debug.Log(FormattableString.Invariant($"HW2_ACTION T x={local.transform.position.x:F3}")); stage++;
                }
                if (stage == 5 && t > 5.3f) { Send(-1); stage++; }
                if (stage == 6 && t > 5.5f) { Send(0); stage++; }
                if (stage == 7 && t > 6) { Send(float.NaN); stage++; Debug.Log("HW2_ACTION NaN"); }
                if (stage == 8 && t > 7) { Send(float.NegativeInfinity); stage++; Debug.Log("HW2_ACTION negative infinity"); }
                if (stage == 9 && t > 7.2f) { Send(0); stage++; }
                if (stage == 10 && t > 8) { local.RequestServe(); stage++; Debug.Log("HW2_ACTION serve"); }
            }
            else if (local.Side.Value == 1 && stage == 0 && t > 1)
            { local.RequestServe(); stage++; Debug.Log("HW2_ACTION wrong serve"); }
            else if (local.Side.Value == 2 && stage == 0 && t > 4)
            { for (int i=0;i<20;i++) Send(50); local.RequestServe(); stage++; Debug.Log("HW2_ACTION spectator input"); }
        }
        if (Time.time >= nextSample)
        {
            nextSample = Time.time + .1f;
            var state = GameState.Instance;
            var ball = NetworkBall.Instance;
            foreach (var p in PaddleController.All)
                Debug.Log(FormattableString.Invariant($"HW2_SAMPLE t={Time.time:F3} tick={manager.TimeManager.Tick} server={manager.IsServerStarted} id={p.OwnerId} side={p.Side.Value} x={p.transform.position.x:F3} z={p.transform.position.z:F3} reject={p.RejectedInputs} rtt={manager.TimeManager.RoundTripTime} ball={(ball != null && ball.InPlay.Value)} score={(state == null ? -1 : state.LeftScore.Value + state.RightScore.Value)}"));
        }
        if (Time.time > 24) Application.Quit();
    }
    private void OnDestroy() { if (keyboard != null) InputSystem.RemoveDevice(keyboard); }
}
#endif
