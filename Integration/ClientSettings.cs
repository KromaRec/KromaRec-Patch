using System;
using System.Net;
using System.Net.Sockets;

namespace RecNetPlugin;

internal sealed record ClientSettings(Uri Discovery, Uri Match, string NameServer, int NamePort,
                                      string Region, string AppId)
{
    internal static ClientSettings Parse(string backend, string match, string host, int port,
                                         string region, string appId)
    {
        var discovery = ParseOrigin(backend, "Backend.BaseUrl");
        var matchOrigin = ParseOrigin(match, "Backend.MatchUrl");
        if (Uri.CheckHostName(host) != UriHostNameType.Dns &&
            !(IPAddress.TryParse(host, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork &&
              host == ip.ToString()))
            throw new ArgumentException("Photon.NameServerHost must be a DNS name or IPv4 address.");
        if (port is < 1 or > 65535)
            throw new ArgumentException("Photon.NameServerPort must be between 1 and 65535.");
        if (region.Length is < 2 or > 16 || !Array.TrueForAll(region.ToCharArray(),
                c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'))
            throw new ArgumentException("Photon.Region must contain 2–16 lowercase ASCII letters, digits or hyphens.");
        if (!Guid.TryParseExact(appId, "D", out var id) || id == Guid.Empty || id.ToString("D") != appId)
            throw new ArgumentException("Photon.RealtimeAppId must be a nonzero lowercase UUID.");
        return new(discovery, matchOrigin, host, port, region, appId);
    }

    private static Uri ParseOrigin(string value, string key)
    {
        if (!TryParseOrigin(value, out var uri))
            throw new ArgumentException($"{key} must be an HTTPS origin without credentials, path, query or fragment.");
        return uri;
    }

    internal static bool TryParseOrigin(string value, out Uri uri) =>
        Uri.TryCreate(value, UriKind.Absolute, out uri) && uri.Scheme == Uri.UriSchemeHttps &&
        uri.Port is >= 1 and <= 65535 && uri.UserInfo.Length == 0 && uri.PathAndQuery == "/" &&
        uri.Fragment.Length == 0 && !value.Contains('@') && !value.Contains('\\');

    internal string RewriteDiscovery(string original)
    {
        if (!Uri.TryCreate(original, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Host, "ns.rec.net", StringComparison.OrdinalIgnoreCase) ||
            uri.UserInfo.Length != 0 || uri.Scheme is not ("https" or "http"))
            return null;

        return Discovery.GetLeftPart(UriPartial.Authority) + uri.PathAndQuery;
    }

    internal static bool SameOrigin(Uri request, Uri origin) =>
        request.Scheme == Uri.UriSchemeHttps && request.UserInfo.Length == 0 &&
        request.Port == origin.Port && string.Equals(request.IdnHost, origin.IdnHost, StringComparison.OrdinalIgnoreCase);

    internal bool IsMatchOrigin(Uri uri) => SameOrigin(uri, Match);

    internal bool IsKromaRecAuth(string appId, string endpoint) =>
        string.Equals(appId, AppId, StringComparison.OrdinalIgnoreCase) || IsNameServer(endpoint);

    internal bool IsNameServer(string endpoint) =>
        string.Equals(endpoint, $"{NameServer}:{NamePort}", StringComparison.OrdinalIgnoreCase);
}
