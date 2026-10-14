using FishNet.Object;
using UnityEngine;

public class BallSpawner : NetworkBehaviour
{
    public NetworkObject Prefab;
    public override void OnStartServer()
    {
        if (Prefab == null) { Debug.LogError("[Server] Ball prefab missing."); return; }
        NetworkObject ball = Instantiate(Prefab,new Vector3(0,Court.BallY,0),Quaternion.identity);
        Spawn(ball.gameObject);
    }
}
