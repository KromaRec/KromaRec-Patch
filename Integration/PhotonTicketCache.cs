using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace RecNetPlugin;

internal sealed class PhotonTicketCache
{
    internal const string ConnectionInfo = "/player/connection-info";
    internal sealed record Credential(string Token, string Subject, long Expires)
    {
        // Photon Realtime serializes AuthGetParameters as string parameter 216.
        internal string Parameter216 => "token=" + Token;
    }

    private readonly object gate = new();
    private long generation;
    private Credential current;

    internal long Invalidate()
    {
        lock (gate)
        {
            current = null;
            return ++generation;
        }
    }

    internal bool Complete(long attempt, string response, ClientSettings settings, long now)
    {
        var credential = Read(response, settings, now);
        lock (gate)
        {
            if (attempt != generation || credential == null)
                return false;
            current = credential;
            return true;
        }
    }

    internal Credential Get(long now)
    {
        lock (gate)
            return current != null && current.Expires > now + 5 ? current : null;
    }

    private static Credential Read(string response, ClientSettings settings, long now)
    {
        if (string.IsNullOrEmpty(response) || response.Length > 32768)
            return null;

        try
        {
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;
            if (!root.GetProperty("success").GetBoolean())
                return null;

            var value = root.GetProperty("value");
            if (value.GetProperty("photonRealtimeAppId").GetString() != settings.AppId ||
                value.GetProperty("photonRegion").GetString() != settings.Region)
                return null;

            var token = value.GetProperty("photonAuthToken").GetString();
            if (string.IsNullOrEmpty(token) || token.Length > 8192)
                return null;

            var parts = token.Split('.');
            if (parts.Length != 3 || parts.Any(part => part.Length == 0 ||
                part.Any(c => c is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_'))))
                return null;

            using var header = JsonDocument.Parse(Decode(parts[0]));
            using var payload = JsonDocument.Parse(Decode(parts[1]));
            var claims = payload.RootElement;
            var expires = claims.GetProperty("exp").GetInt64();
            var subject = claims.GetProperty("sub").GetString();
            if (header.RootElement.GetProperty("alg").GetString() != "HS256" ||
                claims.GetProperty("aud").GetString() != settings.AppId ||
                claims.GetProperty("kromarec_use").GetString() != "photon-realtime" ||
                claims.GetProperty("rn.env").GetString() != "prod" ||
                expires <= now + 5 ||
                !long.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var player) ||
                player <= 0 || player.ToString(CultureInfo.InvariantCulture) != subject)
                return null;

            // The HTTPS peer issued this value. Luxon delegates signature and admission checks to KromaRec.
            return new(token, subject, expires);
        }
        catch (Exception error) when (error is JsonException or FormatException or InvalidOperationException or
                                     System.Collections.Generic.KeyNotFoundException)
        {
            return null;
        }
    }

    private static byte[] Decode(string part) =>
        Convert.FromBase64String(part.Replace('-', '+').Replace('_', '/').PadRight((part.Length + 3) / 4 * 4, '='));
}
