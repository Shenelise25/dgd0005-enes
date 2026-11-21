using System;
using System.Linq;
using System.Threading.Tasks;
using FishNet.Managing;
using FishNet.Transporting.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;

public static class RelayLink
{
    private static async Task Initialize()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions();
            options.SetProfile("pong-" + System.Diagnostics.Process.GetCurrentProcess().Id);
            await UnityServices.InitializeAsync(options);
        }
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public static async Task<string> Host(NetworkManager manager)
    {
        await Initialize();
        var transport = GetTransport(manager);
        var allocation = await RelayService.Instance.CreateAllocationAsync(1);
        string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        var endpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        transport.SetRelayServerData(endpoint.Host, (ushort)endpoint.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, isSecure: true);
        return code;
    }

    public static async Task Join(NetworkManager manager, string code)
    {
        code = code.Trim().ToUpperInvariant();
        if (code.Length != 6 || code.Any(c => !((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))))
            throw new ArgumentException("Enter the six-character join code from the host.");
        await Initialize();
        var transport = GetTransport(manager);
        var allocation = await RelayService.Instance.JoinAllocationAsync(code);
        var endpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        transport.SetRelayServerData(endpoint.Host, (ushort)endpoint.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData,
            allocation.HostConnectionData, true);
    }

    private static UnityTransport GetTransport(NetworkManager manager)
    {
        var transport = manager.TransportManager.GetTransport<UnityTransport>();
        if (transport == null || manager.TransportManager.Transport != transport)
            throw new InvalidOperationException("Select UnityTransport on the NetworkManager's TransportManager.");
        return transport;
    }
}
