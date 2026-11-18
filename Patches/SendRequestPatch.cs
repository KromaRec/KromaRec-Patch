using System;
using BestHTTP;
using HarmonyLib;
using Il2CppInterop.Runtime;

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
