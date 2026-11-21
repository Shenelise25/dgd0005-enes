// Replacement for the missing course HUD. Panels and Flash keep its documented API.
using System;
using System.Collections.Generic;
using System.Reflection;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.UTP;
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
    private string _joinCode = "";
    private string _relayCode = "";
    private bool _relayBusy;
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
    private async void HostViaRelay()
    {
        if (_relayBusy) return;
        _relayBusy = true;
        try
        {
            string code = await RelayLink.Host(manager);
            if (this == null) return;
            if (!manager.ServerManager.StartConnection()) throw new InvalidOperationException("Server could not start.");
            if (!manager.ClientManager.StartConnection()) throw new InvalidOperationException("Host client could not start.");
            _relayCode = code;
            Debug.Log("[Relay] Join code: " + code);
        }
        catch (Exception error)
        {
            if (this == null) return;
            _relayCode = "";
            manager.ClientManager.StopConnection();
            manager.ServerManager.StopConnection(true);
            Debug.LogError("[Relay] Host failed: " + error.Message);
            Flash("Relay host failed: " + error.Message);
        }
        finally { _relayBusy = false; }
    }
    private async void JoinViaRelay()
    {
        if (_relayBusy) return;
        _relayBusy = true;
        try
        {
            await RelayLink.Join(manager, _joinCode);
            if (this == null) return;
            if (!manager.ClientManager.StartConnection()) throw new InvalidOperationException("Client could not start.");
        }
        catch (Exception error)
        {
            if (this == null) return;
            Debug.LogError("[Relay] Join failed: " + error.Message);
            Flash("Relay join failed: " + error.Message);
        }
        finally { _relayBusy = false; }
    }
    private void DirectConnection()
    {
        var transport = manager.TransportManager.GetTransport<UnityTransport>();
        if (transport != null) transport.SetConnectionData(address,7770,"0.0.0.0");
    }
    private void OnGUI()
    {
        if (manager == null) return;
        GUILayout.BeginArea(new Rect(12,12,330,Screen.height-24), GUI.skin.box);
        GUILayout.Label("PONG | DGD0005 Week 06");
        address = GUILayout.TextField(address);
        var transport = manager.TransportManager.Transport;
        LocalConnectionState serverState = transport.GetConnectionState(true);
        LocalConnectionState clientState = transport.GetConnectionState(false);
        ushort port = transport.GetPort();
        GUI.enabled = !_relayBusy;
        GUILayout.BeginHorizontal();
        if (serverState == LocalConnectionState.Stopped && GUILayout.Button("Start Server"))
        { DirectConnection(); manager.ServerManager.StartConnection(); }
        else if (serverState == LocalConnectionState.Started && GUILayout.Button("Stop Server"))
            manager.ServerManager.StopConnection(true);
        if (serverState == LocalConnectionState.Stopped && clientState == LocalConnectionState.Stopped
            && GUILayout.Button("Start Host"))
        {
            DirectConnection();
            manager.ServerManager.StartConnection();
            manager.ClientManager.StartConnection("127.0.0.1", port);
        }
        if (serverState == LocalConnectionState.Started && clientState == LocalConnectionState.Started
            && GUILayout.Button("Stop Host"))
        {
            manager.ClientManager.StopConnection();
            manager.ServerManager.StopConnection(true);
            _relayCode = "";
        }
        GUILayout.EndHorizontal();
        if (clientState == LocalConnectionState.Stopped && GUILayout.Button("Start Client"))
        { if (serverState == LocalConnectionState.Stopped) DirectConnection(); manager.ClientManager.StartConnection(address, port); }
        else if (clientState == LocalConnectionState.Started && GUILayout.Button("Stop Client"))
            manager.ClientManager.StopConnection();
        if (serverState == LocalConnectionState.Stopped && clientState == LocalConnectionState.Stopped)
        {
            if (GUILayout.Button("Host via Relay")) HostViaRelay();
            _joinCode = GUILayout.TextField(_joinCode,6).ToUpperInvariant();
            if (GUILayout.Button("Join via Relay")) JoinViaRelay();
        }
        GUI.enabled = true;
        if (_relayBusy) GUILayout.Label("Connecting to Relay...");
        if (!string.IsNullOrEmpty(_relayCode) && serverState == LocalConnectionState.Started)
            GUILayout.Label("JOIN CODE: " + _relayCode, new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold });
        if (serverState == LocalConnectionState.Stopped && !_relayBusy) _relayCode = "";
        GUILayout.Label($"Server: {serverState} | Client: {clientState} | Port: {port}");
        GUILayout.Label($"RTT: {manager.TimeManager.RoundTripTime} ms | Tick: {manager.TimeManager.Tick}\nTick rate: {manager.TimeManager.TickRate} | Clients: {manager.ServerManager.Clients.Count}");
        if (manager.IsClientStarted)
            GUILayout.Label($"Client: my id = {manager.ClientManager.Connection.ClientId}");
        GUILayout.Label($"PaddleController.All.Count = {PaddleController.All.Count}");
        GUILayout.Label(rates);
        GUILayout.Label("W/S or arrows: move | Space: serve");
        GUILayout.Label("C: spam | X: speed hack | T: teleport | G: score");
        if (PaddleController.Local != null) GUILayout.Label("You: " + PaddleController.SideName(PaddleController.Local.Side.Value));
        foreach (var panel in Panels) GUILayout.Label(panel());
        flashes.RemoveAll(f => f.until < Time.unscaledTime);
        foreach (var flash in flashes) GUILayout.Label(flash.text);
        GUILayout.EndArea();
    }
    private void OnDestroy() { if (traffic != null) traffic.OnNetworkTraffic -= Traffic; flashes.Clear(); }
}
