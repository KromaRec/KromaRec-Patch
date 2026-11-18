using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecNetPlugin;

[BepInPlugin(Id, "KromaRec Patch (20230414)", "0.3.1")]
public class Plugin : BasePlugin
{
    internal const string Id = "net.kromarec.patch";
    internal static new ManualLogSource Log;
    internal static ClientSettings Settings;
    internal static readonly PhotonTicketCache Tickets = new();
    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static ConfigEntry<bool> DisableTelemetry { get; private set; }

    public override void Load()
    {
        Log = base.Log;
        var enabled = Config.Bind("General", "Enabled", true, "Enable the KromaRec client integration.");
        Debug = Config.Bind("General", "Debug", false, "Log route categories, status codes and Photon stages.");
        var backend = Config.Bind("Backend", "BaseUrl", "https://ns.rec.test", "RecNet discovery HTTPS origin.");
        var match = Config.Bind("Backend", "MatchUrl", "https://match.rec.test", "Match service HTTPS origin.");
        var host = Config.Bind("Photon", "NameServerHost", "127.0.0.1", "Luxon NameServer DNS name or IPv4 address.");
        var port = Config.Bind("Photon", "NameServerPort", 5058, "Luxon NameServer UDP port.");
        var region = Config.Bind("Photon", "Region", "us", "Luxon region.");
        var app = Config.Bind("Photon", "RealtimeAppId", "7a6eafd9-8a49-47bd-98da-c743525a3e72", "KromaRec Realtime UUID.");
        DisableTelemetry = Config.Bind("Analytics", "DisableTelemetry", true,
            "Apply the upstream managed telemetry filters and try Unity's opt-out.");

        if (!enabled.Value)
        {
            Log.LogInfo("KromaRec client integration disabled.");
            return;
        }

        try
        {
            Settings = ClientSettings.Parse(backend.Value, match.Value, host.Value, port.Value, region.Value, app.Value);
        }
        catch (ArgumentException error)
        {
            Log.LogError(error.Message);
            return;
        }

        var harmony = new Harmony(Id);
        try
        {
            Patches.PhotonPatches.Install(harmony);
            if (Debug.Value)
                Log.LogInfo("Both Photon authentication guards installed.");
            Patches.SendRequestPatch.Install(harmony);
            if (Debug.Value)
                Log.LogInfo("BestHTTP integration hook installed.");
            Log.LogInfo("Required Harmony hook installation completed. Native authentication remains guarded.");
            SceneManager.sceneLoaded += (Action<Scene, LoadSceneMode>)OnSceneLoaded;
        }
        catch (Exception error)
        {
            harmony.UnpatchSelf();
            Log.LogError($"20230414 integration could not bind ({error.GetType().Name}). Check the build and regenerate interop.");
            return;
        }

        if (DisableTelemetry.Value)
        {
            foreach (var type in new[] { typeof(Patches.AmplitudePatch),
                         typeof(Patches.AmplitudePatch.BlockAnalyticsUploadPatch),
                         typeof(Patches.AmplitudePatch.BlockAnalyticsUploadPatch.ImplPatch),
                         typeof(Patches.BacktracePatch) })
            {
                try { harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception error)
                {
                    Log.LogWarning($"Optional telemetry hook {type.Name} failed ({error.GetType().Name}).");
                }
            }
            Patches.UnityTelemetryPatch.Apply();
        }

        if (Debug.Value)
            Patches.NativeDiagnostics.Install();
        Log.LogInfo("KromaRec 20230414 startup diagnostics initialized. KromaRec Photon authentication is pending verification.");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Debug.Value)
            Log.LogInfo("Unity sceneLoaded callback received.");
        if (DisableTelemetry.Value)
            Patches.UnityTelemetryPatch.Apply();
    }
}
