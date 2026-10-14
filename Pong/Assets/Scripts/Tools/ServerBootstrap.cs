// Replacement for the course Tools file, which was not included in Starter.zip.
using System;
using System.Linq;
using FishNet;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.Playmode;
#endif

public class ServerBootstrap : MonoBehaviour
{
    private void Start()
    {
        Application.runInBackground = true;
        bool server = Application.isBatchMode || Environment.GetCommandLineArgs().Contains("-server");
#if UNITY_EDITOR
        server |= CurrentPlayer.ReadOnlyTags().Contains("Server");
#endif
        if (server) InstanceFinder.ServerManager.StartConnection();
        else if (Environment.GetCommandLineArgs().Contains("-client"))
            InstanceFinder.ClientManager.StartConnection("127.0.0.1", 7770);
    }
}
