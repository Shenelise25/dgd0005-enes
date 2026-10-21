using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : NetworkBehaviour
{
    public readonly SyncVar<int> Side = new SyncVar<int>(2);
    public static readonly List<PaddleController> All = new List<PaddleController>();
    public static PaddleController Local { get; private set; }
    [SerializeField] private float speed = 8f;
    private const int Spectator = 2;
    private float _serverAxis;
    public override void OnStartNetwork() => TimeManager.OnTick += OnTick;
    public override void OnStopNetwork() => TimeManager.OnTick -= OnTick;
    private void Awake() => Side.OnChange += SideChanged;
    private void OnDestroy() { Side.OnChange -= SideChanged; All.Remove(this); if (Local == this) Local = null; }
    private void Register() { if (!All.Contains(this)) All.Add(this); }
    public override void OnStartServer()
    {
        Register();
        bool left = All.Exists(p => p != this && p.IsServerInitialized && p.Side.Value == 0);
        bool right = All.Exists(p => p != this && p.IsServerInitialized && p.Side.Value == 1);
        Side.Value = !left ? 0 : !right ? 1 : 2;
        Place();
        Debug.Log($"[Server] client {OwnerId}: {SideName(Side.Value)}");
        GameState.Announce($"Client {OwnerId} joined: {SideName(Side.Value)}");
    }
    public override void OnStartClient()
    {
        Register();
        if (IsOwner) { Local = this; Debug.Log($"[Client] I am client {OwnerId}, side {SideName(Side.Value)}"); }
        Place();
    }
    public override void OnStopServer()
    {
        GameState.Announce($"Client {OwnerId} left: {SideName(Side.Value)}");
        All.Remove(this);
    }
    public override void OnStopClient() { All.Remove(this); if (Local == this) Local = null; }
    private void SideChanged(int previous, int next, bool asServer) => Place();
    public static string SideName(int side) => side == 0 ? "LEFT" : side == 1 ? "RIGHT" : "SPECTATOR";
    private void Place()
    {
        GetComponent<Renderer>().enabled = Side.Value != 2;
        if (Side.Value == 2) return;
        transform.position = new Vector3(Side.Value == 0 ? -Court.PaddleX : Court.PaddleX, .5f, transform.position.z);
        Court.Paint(gameObject, Side.Value == 0 ? new Color(.35f,.65f,1f) : new Color(1f,.60f,.25f));
    }
    private void Update()
    {
        if (!IsOwner) return;
        if (Side.Value == 2) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.spaceKey.wasPressedThisFrame) RequestServe();
    }
    private void OnTick()
    {
        if (IsOwner && Side.Value != Spectator)
        {
            Keyboard kb = Keyboard.current;
            float axis = kb == null ? 0f
                : (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            ServerSetAxis(axis); // Send zero too, so releasing a key stops the paddle.
        }
        if (IsServerInitialized)
        {
            transform.position += Vector3.forward * _serverAxis * speed * (float)TimeManager.TickDelta;
            Court.ClampPaddle(transform);
        }
    }
    [ServerRpc]
    private void ServerSetAxis(float axis)
    {
        if (float.IsNaN(axis)) return;
        axis = Mathf.Clamp(axis, -1f, 1f);
        if (Side.Value == Spectator) return;
        _serverAxis = axis;
    }
    [ServerRpc]
    public void RequestServe()
    {
        var state = GameState.Instance;
        var ball = NetworkBall.Instance;
        if (state == null || ball == null || Side.Value == 2) return;
        if (Side.Value != state.ServingSide.Value || ball.InPlay.Value) return;
        ball.Serve(1-Side.Value);
        GameState.Announce($"{SideName(Side.Value)} served");
    }
    [ObserversRpc]
    public void AnnounceGoal(int left, int right)
        => ConnectionHud.Flash($"{SideName(Side.Value)} scored! {left} : {right}");
    [TargetRpc]
    public void YouServe(NetworkConnection target) => ConnectionHud.Flash("YOU SERVE");
}
