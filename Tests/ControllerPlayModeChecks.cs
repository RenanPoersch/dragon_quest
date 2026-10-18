using UnityEditor;
using UnityEngine;

// Enter the real player loop in the disposable validation project, including a domain reload.
[InitializeOnLoad]
public static class ControllerPlayModeChecks
{
    private const string Pending = "DragonQuest.ControllerPlayModeChecks.Pending";
    static ControllerPlayModeChecks() { EditorApplication.playModeStateChanged += OnPlayMode; }
    public static void Run()
    {
        SessionState.SetBool(Pending, true);
        if (EditorApplication.isPlaying) OnPlayMode(PlayModeStateChange.EnteredPlayMode);
        else EditorApplication.EnterPlaymode();
    }
    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        if (!Application.isPlaying) { Debug.LogError("Controller checks did not enter Play Mode."); EditorApplication.Exit(1); return; }
        Debug.Log("CONTROLLER_PLAYMODE_CHECKS_STARTED");
        ControllerUnityChecks.Run();
    }
}
