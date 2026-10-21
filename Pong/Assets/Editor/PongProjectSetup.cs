using System.IO;
using FishNet.Component.Spawning;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Managing.Timing;
using FishNet.Managing.Statistic;
using FishNet.Object;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PongProjectSetup
{
    public static void Set(Object target, string field, object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null) throw new System.Exception("Missing serialized field: " + field);
        if (value is bool b) property.boolValue = b;
        else if (value is int i) property.intValue = i;
        else if (value is Object o) property.objectReferenceValue = o;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
    private static void Camera()
    {
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0,20,0);
        camera.transform.rotation = Quaternion.Euler(90,0,0);
        camera.orthographic = true; camera.orthographicSize = 7;
        camera.backgroundColor = new Color(.04f,.06f,.10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.gameObject.AddComponent<AudioListener>();
    }
    [MenuItem("Pong/Build Connect Scenes (resets scenes)")]
    public static void Connect()
    {
        Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Assets/Prefabs");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Camera(); new GameObject("Court").AddComponent<Court>();
        for (int side = 0; side < 2; side++)
        {
            var paddle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paddle.name = side == 0 ? "Left Paddle" : "Right Paddle";
            paddle.transform.localScale = new Vector3(.5f,1,2);
            Object.DestroyImmediate(paddle.GetComponent<Collider>());
            Set(paddle.AddComponent<PaddleMovement>(),"side",side);
        }
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/Pong-Start.unity");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Camera(); new GameObject("Court").AddComponent<Court>();
        var manager = new GameObject("NetworkManager").AddComponent<NetworkManager>();
        // FishNet adds its required managers through Reset when added in the editor.
        if (!manager.TryGetComponent<ServerManager>(out var server)) server = manager.gameObject.AddComponent<ServerManager>();
        server.SetStartOnHeadless(false);
        if (!manager.TryGetComponent<TimeManager>(out var time)) time = manager.gameObject.AddComponent<TimeManager>();
        Set(time,"_tickRate",30);
        if (!manager.TryGetComponent<StatisticsManager>(out var stats)) stats = manager.gameObject.AddComponent<StatisticsManager>();
        Set(stats,"_networkTraffic._enableMode",1);
        Set(stats,"_networkTraffic._updateClient",true); Set(stats,"_networkTraffic._updateServer",true);
        manager.gameObject.AddComponent<ConnectionLogger>();
        manager.gameObject.AddComponent<ServerBootstrap>();
        new GameObject("Hud").AddComponent<ConnectionHud>();
        var source = GameObject.CreatePrimitive(PrimitiveType.Cube);
        source.name = "Paddle"; source.transform.localScale = new Vector3(.5f,1,2);
        Object.DestroyImmediate(source.GetComponent<Collider>());
        source.AddComponent<NetworkObject>(); source.AddComponent<PaddleController>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(source,"Assets/Prefabs/Paddle.prefab");
        Object.DestroyImmediate(source);
        manager.gameObject.AddComponent<PlayerSpawner>().SetPlayerPrefab(prefab.GetComponent<NetworkObject>());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/Pong.unity");
        EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene("Assets/Scenes/Pong.unity",true)};
        PlayerSettings.runInBackground = true; PlayerSettings.productName = "Pong";
        AssetDatabase.SaveAssets();
        Debug.Log("PONG_CONNECT_SCENES_READY");
    }
    [MenuItem("Pong/Add Paddle Replication")]
    public static void PaddleReplication()
    {
        var prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Paddle.prefab");
        var nt = prefab.GetComponent<NetworkTransform>();
        if (nt == null) nt = prefab.AddComponent<NetworkTransform>();
        ConfigureTransform(nt,false);
        PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/Paddle.prefab");
        PrefabUtility.UnloadPrefabContents(prefab);
        AssetDatabase.SaveAssets();
    }
    public static void ConfigureTransform(NetworkTransform nt, bool clientAuthority)
    {
        Set(nt,"_clientAuthoritative",clientAuthority); Set(nt,"_interpolation",2);
        Set(nt,"_synchronizeRotation",false); Set(nt,"_synchronizeScale",false);
        Set(nt,"_sendToOwner",!clientAuthority);
    }
}
