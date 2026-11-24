using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using RecNetPlugin;

internal static class Backend103Check
{
    // Only use with a disposable localhost backend allowing signup.
    internal static async Task Run(int port)
    {
        const string app = "7a6eafd9-8a49-47bd-98da-c743525a3e72";
        var settings = ClientSettings.Parse("https://ns.rec.test", "https://match.rec.test",
            "127.0.0.1", 5058, "us", app);
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false });
        client.BaseAddress = new Uri($"http://127.0.0.1:{port}");
        async Task<string> Fetch(string host, string path, HttpMethod method, string bearer = null,
                                 Dictionary<string, string> form = null)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Host = host + ".rec.test";
            if (bearer != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            if (form != null) request.Content = new FormUrlEncodedContent(form);
            using var reply = await client.SendAsync(request);
            if (reply.StatusCode != HttpStatusCode.OK) throw new Exception($"Backend {host} HTTP {(int)reply.StatusCode}");
            return await reply.Content.ReadAsStringAsync();
        }

        var map = await Fetch("ns", "/", HttpMethod.Get);
        var trace = StartupTrace.Read(map, settings, out var status);
        if (trace == null || !trace.IsTokenEndpoint(new Uri("https://auth.rec.test/connect/token")))
            throw new Exception(status);
        var signup = new Dictionary<string, string> {
            ["grant_type"] = "create_account", ["password"] = Guid.NewGuid().ToString("N"), ["ver"] = "20230414"
        };
        using var auth = JsonDocument.Parse(await Fetch("auth", "/connect/token", HttpMethod.Post, form: signup));
        var bearer = auth.RootElement.GetProperty("access_token").GetString();
        using var profile = JsonDocument.Parse(await Fetch("accounts", "/account/me", HttpMethod.Get, bearer));
        var player = profile.RootElement.GetProperty("accountId").GetInt64();
        var info = await Fetch("match", PhotonTicketCache.ConnectionInfo, HttpMethod.Get, bearer);
        var cache = new PhotonTicketCache();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!cache.Complete(cache.Invalidate(), info, settings, now) ||
            cache.Get(now)?.Subject != player.ToString(System.Globalization.CultureInfo.InvariantCulture))
            throw new Exception("Backend ticket failed client parsing or identity check");
        Console.WriteLine("PASS: real 0.10.3 directory, local signup, profile and connection-info accepted by managed client logic.");
    }
}
