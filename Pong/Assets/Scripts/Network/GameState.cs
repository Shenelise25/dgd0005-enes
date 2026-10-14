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
    public readonly SyncList<string> Feed = new SyncList<string>();
    private Func<string> panel;
    private void Awake() => Instance = this;
    private void Start() { panel = Panel; ConnectionHud.Panels.Add(panel); }
    private string Panel() => $"LEFT {LeftScore.Value} : {RightScore.Value} RIGHT\nServe: {PaddleController.SideName(ServingSide.Value)}\n\n" + string.Join("\n",Feed);
    public static void Announce(string line)
    {
        if (Instance == null || !Instance.IsServerInitialized) return;
        Instance.Feed.Add(line);
        while (Instance.Feed.Count > 6) Instance.Feed.RemoveAt(0);
        Debug.Log("[Server] " + line);
    }
    private void OnDestroy() { if (panel != null) ConnectionHud.Panels.Remove(panel); if (Instance == this) Instance = null; }
    public void Goal(int scorerSide)
    {
        if (!IsServerInitialized || (scorerSide != 0 && scorerSide != 1)) return;
        if (scorerSide == 0) LeftScore.Value++; else RightScore.Value++;
        ServingSide.Value = 1-scorerSide;
        Announce($"{PaddleController.SideName(scorerSide)} scored! {LeftScore.Value} : {RightScore.Value}");
        var scorer = PaddleController.All.Find(p => p != null && p.IsServerInitialized && p.Side.Value == scorerSide);
        var conceder = PaddleController.All.Find(p => p != null && p.IsServerInitialized && p.Side.Value == ServingSide.Value);
        if (scorer != null) scorer.AnnounceGoal(LeftScore.Value,RightScore.Value);
        if (conceder != null && conceder.Owner.IsActive) conceder.YouServe(conceder.Owner);
    }
}
