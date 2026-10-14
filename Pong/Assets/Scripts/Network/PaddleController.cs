using System.Collections.Generic;
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
    }
    public override void OnStartClient()
    {
        Register();
        if (IsOwner) { Local = this; Debug.Log($"[Client] I am client {OwnerId}, side {SideName(Side.Value)}"); }
        Place();
    }
    public override void OnStopServer() => All.Remove(this);
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
        float direction = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                        - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        transform.position += Vector3.forward * direction * speed * Time.deltaTime;
        Court.ClampPaddle(transform);
    }
}
