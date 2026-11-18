using System;
using System.Reflection;
using ExitGames.Client.Photon;
using HarmonyLib;

namespace RecNetPlugin.Patches;

internal static class PhotonPatches
{
    private static int reportedGuard;
    internal static void Install(Harmony harmony)
    {
        var (regular, once) = PhotonBindings.Resolve(Assembly.Load("PhotonRealtime"), typeof(ConnectionProtocol));
        if (!typeof(PhotonPeer).IsAssignableFrom(regular.DeclaringType))
            throw new MissingMemberException("20230414 Photon peer inheritance");

        var prefix = new HarmonyMethod(typeof(PhotonPatches), nameof(RequireVerifiedAuth));
        harmony.Patch(regular, prefix: prefix);
        harmony.Patch(once, prefix: prefix);
    }

    private static bool RequireVerifiedAuth(PhotonPeer __instance, string __0, ref bool __result)
    {
        var settings = Plugin.Settings;
        if (!settings.IsKromaRecAuth(__0, __instance.ServerAddress))
            return true;

        // Both methods are bound, but the auth object's native member roles are still unverified.
        if (Plugin.Debug.Value)
            Plugin.Log.LogInfo($"Photon auth-guard: encrypted={__instance.IsEncryptionAvailable}; admission=blocked.");
        if (System.Threading.Interlocked.Exchange(ref reportedGuard, 1) == 0)
        {
            Plugin.Log.LogWarning("KromaRec Photon authentication blocked pending 20230414 auth member verification.");
        }
        __result = false;
        return false;
    }
}
