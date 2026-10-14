using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GameState : NetworkBehaviour
{
    public static GameState Instance { get; private set; }
    public readonly SyncVar<int> LeftScore = new SyncVar<int>();
    public readonly SyncVar<int> RightScore = new SyncVar<int>();
    public readonly SyncVar<int> ServingSide = new SyncVar<int>(0);
    private Func<string> panel;
    private void Awake() => Instance = this;
    private void Start() { panel = Panel; ConnectionHud.Panels.Add(panel); }
    private string Panel() => $"LEFT {LeftScore.Value} : {RightScore.Value} RIGHT\nServe: {PaddleController.SideName(ServingSide.Value)}";
    private void OnDestroy() { if (panel != null) ConnectionHud.Panels.Remove(panel); if (Instance == this) Instance = null; }
    public void Goal(int scorerSide)
    {
        if (!IsServerInitialized || (scorerSide != 0 && scorerSide != 1)) return;
        if (scorerSide == 0) LeftScore.Value++; else RightScore.Value++;
        ServingSide.Value = 1-scorerSide;
    }
}
