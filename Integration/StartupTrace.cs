using System;
using System.Text.Json;

namespace RecNetPlugin;

internal sealed record StartupTrace(Uri Api, Uri Auth, Uri Accounts, Uri Rooms, Uri Notifications, Uri Cdn)
{
    internal static StartupTrace Read(string body, ClientSettings settings, out string outcome)
    {
        outcome = "invalid service map";
        if (string.IsNullOrEmpty(body) || body.Length > 131072)
            return null;
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return null;
            // Backend 0.10.3 assigns a distinct HTTPS authority to every service.
            var origins = new System.Collections.Generic.HashSet<Uri> { settings.Discovery };
            var labels = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in root.EnumerateObject())
            {
                if (!labels.Add(entry.Name) || entry.Value.ValueKind != JsonValueKind.String ||
                    !ClientSettings.TryParseOrigin(entry.Value.GetString(), out var origin) ||
                    !origins.Add(origin))
                    return null;
            }
            var match = Origin(root, "Matchmaking");
            if (match == null || !settings.IsMatchOrigin(match))
            {
                outcome = "MatchUrl differs from discovery";
                return null;
            }

            var api = Origin(root, "API");
            var auth = Origin(root, "Auth");
            var accounts = Origin(root, "Accounts");
            var cdn = Origin(root, "CDN");
            var rooms = Origin(root, "Rooms");
            var notifications = Origin(root, "Notifications");
            if (api == null || auth == null || accounts == null || cdn == null ||
                notifications == null || rooms == null ||
                Origin(root, "PlayerSettings") == null)
                return null;
            outcome = "service origins accepted";
            return new(api, auth, accounts, rooms, notifications, cdn);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Uri Origin(JsonElement map, string key) =>
        map.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String &&
        ClientSettings.TryParseOrigin(value.GetString(), out var origin) ? origin : null;

    internal bool IsTokenEndpoint(Uri uri) =>
        ClientSettings.SameOrigin(uri, Auth) && uri.AbsolutePath == "/connect/token";

    internal string Label(Uri uri, bool get, bool post)
    {
        if (post && IsTokenEndpoint(uri))
            return "oauth";
        if (ClientSettings.SameOrigin(uri, Notifications))
        {
            if (post && uri.AbsolutePath == "/hub/v1/negotiate")
                return "notify-negotiate";
            if (get && uri.AbsolutePath == "/hub/v1")
                return "notify-hub";
        }
        if (ClientSettings.SameOrigin(uri, Rooms))
            return "rooms";
        if (post && ClientSettings.SameOrigin(uri, Api) &&
            uri.AbsolutePath is "/api/PlayerReporting/v1/deviceId" or "/PlayerReporting/v1/deviceId")
            return "device-id";
        if (!get)
            return null;
        if (ClientSettings.SameOrigin(uri, Api))
            return uri.AbsolutePath switch
            {
                "/api/versioncheck/v4" => "version",
                "/api/config/v2" => "initial-config",
                "/api/gameconfigs/v1/all" => "game-config",
                _ => null
            };
        if (ClientSettings.SameOrigin(uri, Accounts) && uri.AbsolutePath == "/account/me")
            return "account";
        if (ClientSettings.SameOrigin(uri, Cdn) && uri.AbsolutePath == "/config/LoadingScreenTipData")
            return "cdn-bootstrap";
        return null;
    }

    internal static string MatchLabel(string path)
    {
        return path switch
        {
            "/matchmake/none" => "startup-match",
            "/matchmake/dorm" => "dorm-match",
            "/invite" => "invite",
            "/player/login" => "session-login",
            "/player/exclusivelogin" => "session-exclusive-login",
            "/player/logout" => "session-logout",
            "/player/verifylock" => "session-verify-lock",
            "/player/heartbeat" => "session-heartbeat",
            _ => path.StartsWith("/matchmake/", StringComparison.Ordinal) ? "matchmake" : null
        };
    }

    internal static string ResultField(string label) => label switch
    {
        "version" => "VersionStatus",
        "matchmake" or "startup-match" or "dorm-match" or "session-exclusive-login" or "session-heartbeat" => "errorCode",
        _ => null
    };

    internal static bool TryResult(string payload, string field, out int code, out (long Room, long Instance)? ids)
    {
        code = 0;
        ids = null;
        if (string.IsNullOrEmpty(payload) || payload.Length > 32768)
            return false;
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(field, out var value) ||
                value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out code))
                return false;
            if (field == "errorCode" && code == 0 && root.TryGetProperty("roomInstance", out var instance) &&
                instance.ValueKind == JsonValueKind.Object &&
                TryPositiveId(instance, "roomId", out var room) && TryPositiveId(instance, "roomInstanceId", out var id))
                ids = (room, id);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryPositiveId(JsonElement value, string field, out long id)
    {
        id = 0;
        return value.TryGetProperty(field, out var property) && property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt64(out id) && id > 0;
    }

    internal static bool LooksLikeSceneFailure(string message) =>
        message != null && message.StartsWith("Scene ", StringComparison.OrdinalIgnoreCase) &&
        (message.Contains("couldn't be loaded", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("could not be loaded", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("failed to load", StringComparison.OrdinalIgnoreCase));
}
