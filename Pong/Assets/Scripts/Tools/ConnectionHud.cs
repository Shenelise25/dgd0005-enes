// Replacement for the missing course HUD. Panels and Flash keep its documented API.
using System;
using System.Collections.Generic;
using System.Reflection;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Statistic;
using FishNet.Editing.NetworkProfiler;
using UnityEngine;

public class ConnectionHud : MonoBehaviour
{
    public static readonly List<Func<string>> Panels = new List<Func<string>>();
    private static readonly List<(string text, float until)> flashes = new List<(string, float)>();
    private NetworkManager manager;
    private NetworkTrafficStatistics traffic;
    private string address = "127.0.0.1";
    private double serverIn, serverOut, clientIn, clientOut;
    private string rates = "Waiting for traffic";
    private float sampleStart;
    // The 4.7.3 tag exposes the traffic event but its direction fields are internal.
    private static readonly FieldInfo inbound = typeof(BidirectionalNetworkTraffic).GetField("InboundTraffic", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo outbound = typeof(BidirectionalNetworkTraffic).GetField("OutboundTraffic", BindingFlags.Instance | BindingFlags.NonPublic);
    public static void Flash(string message)
    {
        flashes.Add((message, Time.unscaledTime + 4f));
        Debug.Log("[Client] Flash: " + message);
    }
    private void Start()
    {
        manager = InstanceFinder.NetworkManager;
        sampleStart = Time.unscaledTime;
        if (manager.StatisticsManager.TryGetNetworkTrafficStatistics(out traffic)) traffic.OnNetworkTraffic += Traffic;
    }
    private void LateUpdate()
    {
        // Keep the connection panel beside the court instead of covering the left paddle.
        Camera camera = Camera.main;
        if (camera == null || Screen.height == 0) return;
        float left = Mathf.Min(350f / Screen.width, .45f);
        camera.rect = new Rect(left,0,1-left,1);
        float courtAspect = Screen.width * (1-left) / Screen.height;
        camera.orthographicSize = Mathf.Max(6f,9f/courtAspect);
    }
    private static ulong Bytes(BidirectionalNetworkTraffic data, FieldInfo direction)
    {
        object value = direction?.GetValue(data);
        return value == null ? 0UL : (ulong)value.GetType().GetField("Bytes").GetValue(value);
    }
    private void Traffic(uint tick, BidirectionalNetworkTraffic server, BidirectionalNetworkTraffic client)
    {
        serverIn += Bytes(server, inbound); serverOut += Bytes(server, outbound);
        clientIn += Bytes(client, inbound); clientOut += Bytes(client, outbound);
        float elapsed = Time.unscaledTime - sampleStart;
        if (elapsed < 1f) return;
        rates = $"Server in/out: {serverIn/elapsed:F0} / {serverOut/elapsed:F0} B/s\nClient in/out: {clientIn/elapsed:F0} / {clientOut/elapsed:F0} B/s";
        serverIn = serverOut = clientIn = clientOut = 0;
        sampleStart = Time.unscaledTime;
    }
    private void OnGUI()
    {
        if (manager == null) return;
        GUILayout.BeginArea(new Rect(12,12,330,Screen.height-24), GUI.skin.box);
        GUILayout.Label("PONG | DGD0005 Week 04");
        address = GUILayout.TextField(address);
        if (GUILayout.Button(manager.IsServerStarted ? "Stop Server" : "Start Server"))
        { if (manager.IsServerStarted) manager.ServerManager.StopConnection(true); else manager.ServerManager.StartConnection(); }
        if (GUILayout.Button(manager.IsClientStarted ? "Stop Client" : "Start Client"))
        { if (manager.IsClientStarted) manager.ClientManager.StopConnection(); else manager.ClientManager.StartConnection(address,7770); }
        GUILayout.Label($"RTT: {manager.TimeManager.RoundTripTime} ms | Tick: {manager.TimeManager.Tick}\nTick rate: {manager.TimeManager.TickRate} | Clients: {manager.ServerManager.Clients.Count}");
        GUILayout.Label(rates);
        GUILayout.Label("W/S or arrows: move | Space: serve");
        GUILayout.Label("C: spam | X: speed hack | T: teleport");
        if (PaddleController.Local != null) GUILayout.Label("You: " + PaddleController.SideName(PaddleController.Local.Side.Value));
        foreach (var panel in Panels) GUILayout.Label(panel());
        flashes.RemoveAll(f => f.until < Time.unscaledTime);
        foreach (var flash in flashes) GUILayout.Label(flash.text);
        GUILayout.EndArea();
    }
    private void OnDestroy() { if (traffic != null) traffic.OnNetworkTraffic -= Traffic; flashes.Clear(); }
}
