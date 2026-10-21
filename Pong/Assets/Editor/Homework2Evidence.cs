using UnityEditor;
using UnityEditor.SceneManagement;

// Opens the existing Console for the assignment's manual server evidence.
public static class Homework2Evidence
{
    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Homework2Evidence", false)) return;
            SessionState.SetBool("Homework2Evidence", false);
            FishNet.InstanceFinder.ServerManager.StartConnection();
        };
    }
    [MenuItem("Pong/Homework 2/Start evidence server")]
    public static void StartServer()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Pong.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Console");
        SessionState.SetBool("Homework2Evidence", true);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }
}
