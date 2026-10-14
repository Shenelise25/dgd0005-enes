using FishNet.Component.Transforming;
using FishNet.Object;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PongReplicateSetup
{
    [MenuItem("Pong/Build Replication Scene")]
    public static void BuildScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Pong.unity");
        var source = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        source.name = "Ball"; source.transform.localScale = Vector3.one*.5f;
        Object.DestroyImmediate(source.GetComponent<Collider>());
        source.AddComponent<NetworkObject>(); source.AddComponent<NetworkBall>();
        PongProjectSetup.ConfigureTransform(source.AddComponent<NetworkTransform>(),false);
        var prefab = PrefabUtility.SaveAsPrefabAsset(source,"Assets/Prefabs/Ball.prefab");
        Object.DestroyImmediate(source);
        var court = Object.FindFirstObjectByType<Court>();
        PongProjectSetup.Set(court,"localBall",false);
        if (!court.TryGetComponent<NetworkObject>(out _)) court.gameObject.AddComponent<NetworkObject>();
        var spawner = court.GetComponent<BallSpawner>();
        if (spawner == null) spawner = court.gameObject.AddComponent<BallSpawner>();
        spawner.Prefab = prefab.GetComponent<NetworkObject>();
        if (Object.FindFirstObjectByType<GameState>() == null)
        {
            var state = new GameObject("GameState"); state.AddComponent<NetworkObject>(); state.AddComponent<GameState>();
        }
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PONG_REPLICATION_SCENE_READY");
    }
    public static void BuildPlayer()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources");
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/PongUnlit.mat") == null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Unlit")),"Assets/Resources/PongUnlit.mat");
        PlayerSettings.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);
        EditorSceneManager.OpenScene("Assets/Scenes/Pong.unity");
        // Persist unique scene identities before the build; batch creation has no editor delay loop.
        foreach (var nob in Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (nob.gameObject.name == "Court") nob.SetSceneId(1001);
            else if (nob.gameObject.name == "GameState") nob.SetSceneId(1002);
            EditorUtility.SetDirty(nob);
        }
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[]{"Assets/Scenes/Pong.unity"},
            locationPathName = "Builds/Windows/Pong.exe", target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Player build failed: " + result.summary.result);
        Debug.Log("PONG_PLAYER_BUILD_READY");
    }
}
