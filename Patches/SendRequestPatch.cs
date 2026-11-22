using System;
using System.Runtime.InteropServices;
using BestHTTP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace RecNetPlugin.Patches;

internal static class SendRequestPatch
{
    private static volatile StartupTrace services;

    internal static void Install(Harmony harmony)
    {
        var target = AccessTools.DeclaredMethod(typeof(HTTPManager), "SendRequestImpl", new[] { typeof(HTTPRequest) });
        if (target == null || !target.IsStatic)
            throw new MissingMethodException("BestHTTP.SendRequestImpl(HTTPRequest)");
        harmony.Patch(target, prefix: new HarmonyMethod(typeof(SendRequestPatch), nameof(BeforeSend)));
        if (Plugin.Debug.Value)
            InstallCertificateReportTrace(harmony);
    }

    private static void ObserveDiscoveryTime(Il2CppSystem.Uri __0, bool __result)
    {
        if (!Plugin.Debug.Value) return;
        try
        {
            if (__0 != null && __0.IsAbsoluteUri &&
                string.Equals(__0.Host, Plugin.Settings.Discovery.Host, StringComparison.OrdinalIgnoreCase))
                Plugin.Log.LogInfo(__result ? "TLS discovery time=True" : "TLS discovery time=False");
        }
        catch (Exception)
        {
            Plugin.Log.LogWarning("TLS time result unavailable.");
        }
    }

    private static void ObserveDiscoveryRequest(Il2CppSystem.Uri __0, bool __result)
    {
        if (!Plugin.Debug.Value) return;
        try
        {
            if (__0 != null && __0.IsAbsoluteUri &&
                string.Equals(__0.Host, Plugin.Settings.Discovery.Host, StringComparison.OrdinalIgnoreCase))
                Plugin.Log.LogInfo(__result ? "TLS discovery request=True" : "TLS discovery request=False");
        }
        catch (Exception)
        {
            Plugin.Log.LogWarning("TLS request result unavailable.");
        }
    }

    private static void InstallCertificateReportTrace(Harmony harmony)
    {
        try
        {
            var target = AccessTools.DeclaredMethod(typeof(Org.BouncyCastle.Crypto.Tls.TimeValidVerifyer), "IsValid",
                new[] { typeof(Il2CppSystem.Uri), typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509.X509CertificateStructure>) });
            if (target == null || target.IsStatic || target.ReturnType != typeof(bool))
                throw new MissingMethodException();
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(SendRequestPatch), nameof(ObserveDiscoveryTime)));
            Plugin.Log.LogInfo("TLS time observer installed.");
        }
        catch (Exception error)
        {
            Plugin.Log.LogWarning($"TLS time observer unavailable ({error.GetType().Name}).");
        }
        try
        {
            var target = AccessTools.DeclaredMethod(typeof(EKAKCHPEMEJ).Assembly.GetType("EKAKCHPEMEJ+LJBCIDKNONH", true), "IsValid",
                new[] { typeof(Il2CppSystem.Uri), typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509.X509CertificateStructure>) });
            if (target == null || target.IsStatic || target.ReturnType != typeof(bool))
                throw new MissingMethodException();
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(SendRequestPatch), nameof(ObserveDiscoveryRequest)));
            Plugin.Log.LogInfo("TLS request observer installed.");
        }
        catch (Exception error)
        {
            Plugin.Log.LogWarning($"TLS request observer unavailable ({error.GetType().Name}).");
        }
        try
        {
            var target = AccessTools.DeclaredMethod(typeof(EKAKCHPEMEJ), "MEEELJJBJIP",
                new[] { typeof(IBPKDJLFBHM.MCFHHMMIEHP), typeof(string) });
            if (target == null || !target.IsStatic || target.ReturnType != typeof(void))
            {
                Plugin.Log.LogWarning("TLS report observer unavailable.");
                return;
            }
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(SendRequestPatch), nameof(ObserveCertificateReport)));
            Plugin.Log.LogInfo("TLS report observer installed.");
        }
        catch (Exception error)
        {
            Plugin.Log.LogWarning($"TLS report observer unavailable ({error.GetType().Name}).");
        }
    }

    private static void ObserveCertificateReport(IBPKDJLFBHM.MCFHHMMIEHP __0)
    {
        if (!Plugin.Debug.Value) return;
        switch ((int)__0)
        {
            case 102: Plugin.Log.LogInfo("TLS report code=102"); break;
            case 103: Plugin.Log.LogInfo("TLS report code=103"); break;
        }
    }

    private static string VerifierLabel(object verifier)
    {
        if (verifier is null) return "none";
        if (verifier is not Il2CppObjectBase wrapper) return "unknown";
        var klass = IL2CPP.il2cpp_object_get_class(wrapper.Pointer);
        if (klass == IntPtr.Zero) return "unknown";
        return Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(klass)) switch
        {
            "LJBCIDKNONH" => "LJBCIDKNONH",
            "TimeValidVerifyer" => "TimeValidVerifyer",
            "AlwaysValidVerifyer" => "AlwaysValidVerifyer",
            _ => "other"
        };
    }

    private static void BeforeSend(HTTPRequest __0)
    {
        if (__0?.Uri == null || !Uri.TryCreate(__0.Uri.AbsoluteUri, UriKind.Absolute, out var uri))
            return;
        var request = __0;
        var settings = Plugin.Settings;
        var rewritten = settings.RewriteDiscovery(uri.AbsoluteUri);
        var get = request.MethodType == HTTPMethods.Get;
        var post = request.MethodType == HTTPMethods.Post;
        var match = settings.IsMatchOrigin(uri);
        var discovery = get && uri.AbsolutePath == "/" &&
            (rewritten != null || ClientSettings.SameOrigin(uri, settings.Discovery));
        var connectionInfo = match && get && uri.AbsolutePath == PhotonTicketCache.ConnectionInfo;
        var sessionChange = match && post &&
            uri.AbsolutePath is "/player/logout" or "/player/login" or "/player/exclusivelogin";
        var currentServices = services;
        var oauth = post && currentServices?.IsTokenEndpoint(uri) == true;
        var label = discovery ? "discovery" : connectionInfo ? "connection-info" : oauth ? "oauth" :
            match && post ? StartupTrace.MatchLabel(uri.AbsolutePath) : currentServices?.Label(uri, get, post);
        var debug = Plugin.Debug.Value;

        if (rewritten != null)
            request.Uri = new Il2CppSystem.Uri(rewritten);
        if (rewritten != null || discovery || connectionInfo || sessionChange || oauth)
            request.MaxRedirects = 0;
        if (connectionInfo)
            request.DisableCache = true;
        var attempt = connectionInfo || sessionChange || oauth ? Plugin.Tickets.Invalidate() : 0;
        if (!connectionInfo && !discovery && !(debug && label != null))
            return;

        if (debug && discovery)
        {
            try
            {
                var custom = VerifierLabel(request.CustomCertificateVerifyer);
                var global = VerifierLabel(HTTPManager.DefaultCertificateVerifyer);
                Plugin.Log.LogInfo($"Discovery TLS verifiers: request={custom} default={global}");
            }
            catch (Exception error)
            {
                Plugin.Log.LogWarning($"Discovery TLS verifiers: unavailable ({error.GetType().Name})");
            }
        }
        if (debug)
            Plugin.Log.LogInfo($"RecNet {label}: request started.");
        try
        {
            var original = request.Callback;
            request.Callback = DelegateSupport.ConvertDelegate<OnRequestFinishedDelegate>(
                (Action<HTTPRequest, HTTPResponse>)((req, response) =>
                {
                    try
                    {
                        if (debug && discovery && response == null)
                        {
                            var error = req.Exception;
                            var klass = error == null || error.Pointer == IntPtr.Zero
                                ? IntPtr.Zero : IL2CPP.il2cpp_object_get_class(error.Pointer);
                            var name = klass == IntPtr.Zero ? null
                                : Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(klass));
                            var kind = name switch
                            {
                                null => "none-or-unavailable",
                                "TlsFatalAlert" or "TlsFatalAlertReceived" or "SocketException"
                                    or "IOException" or "AuthenticationException" => name,
                                _ => "other"
                            };
                            Plugin.Log.LogInfo($"RecNet discovery exception type={kind}");
                        }
                        if (response == null)
                            Plugin.Log.LogWarning($"RecNet {label}: no response, state={req.State}.");
                        else
                        {
                            if (debug || response.StatusCode >= 400)
                                Plugin.Log.LogInfo($"RecNet {label}: HTTP {response.StatusCode}.");
                            if (response.StatusCode == 200)
                            {
                                if (connectionInfo)
                                {
                                    if (!Plugin.Tickets.Complete(attempt, response.DataAsText, settings,
                                            DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
                                        Plugin.Log.LogWarning("Photon connection-info ticket rejected or superseded.");
                                    else if (debug)
                                        Plugin.Log.LogInfo("Photon connection-info ticket cached in memory.");
                                }
                                else if (discovery)
                                {
                                    services = StartupTrace.Read(response.DataAsText, settings, out var outcome);
                                    if (debug)
                                        Plugin.Log.LogInfo($"RecNet discovery: {outcome}.");
                                    else if (services == null)
                                        Plugin.Log.LogWarning($"RecNet discovery: {outcome}.");
                                }
                                else if (StartupTrace.ResultField(label) is string field &&
                                    StartupTrace.TryResult(response.DataAsText, field, out var code, out var ids))
                                {
                                    var room = ids is { } pair ? $" room={pair.Room} instance={pair.Instance}" : string.Empty;
                                    Plugin.Log.LogInfo($"RecNet {label}: result={code}{room}.");
                                }
                            }
                        }
                    }
                    catch (Exception error)
                    {
                        Plugin.Log.LogWarning($"RecNet {label}: response inspection failed ({error.GetType().Name}).");
                    }
                    finally
                    {
                        original?.Invoke(req, response);
                    }
                }));
        }
        catch (Exception error)
        {
            Plugin.Log.LogWarning($"RecNet {label}: could not attach callback ({error.GetType().Name}).");
        }
    }
}
