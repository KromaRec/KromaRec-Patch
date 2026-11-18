using System;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace RecNetPlugin.Patches;

internal static class NativeDiagnostics
{
    internal static void Install()
    {
        try
        {
            Application.logMessageReceived += DelegateSupport.ConvertDelegate<Application.LogCallback>(
                (Action<string, string, LogType>)SceneError);
            Plugin.Log.LogInfo("Unity scene error diagnostics bound.");
        }
        catch (Exception error)
        {
            Plugin.Log.LogWarning($"Unity scene error diagnostics unavailable ({error.GetType().Name}).");
        }
    }

    private static void SceneError(string message, string stack, LogType type)
    {
        if (Plugin.Debug.Value && (type is LogType.Error or LogType.Exception) &&
            StartupTrace.LooksLikeSceneFailure(message))
            Plugin.Log.LogWarning("Unity reported a scene-load failure.");
    }
}
