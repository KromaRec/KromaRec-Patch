using System;
using System.Text;
using System.Text.Json;
using RecNetPlugin;

const string App = "7a6eafd9-8a49-47bd-98da-c743525a3e72";
const long Now = 1700000000;
var settings = ClientSettings.Parse("https://ns.rec.test:8443", "https://match.rec.test",
    "127.0.0.1", 5058, "us", App);

Check(settings.RewriteDiscovery("https://NS.rec.net/v1/a%2Fb?x=%2B&x=b") ==
      "https://ns.rec.test:8443/v1/a%2Fb?x=%2B&x=b", "preserves path/query and configured port");
Check(settings.RewriteDiscovery("https://ns.rec.net.evil/") == null &&
      settings.RewriteDiscovery("https://a@ns.rec.net/") == null, "exact discovery host");
Check(settings.IsMatchOrigin(new Uri("https://match.rec.test/player/connection-info")) &&
      !settings.IsMatchOrigin(new Uri("https://match.rec.test:8443/player/connection-info")) &&
      !settings.IsMatchOrigin(new Uri("http://match.rec.test/player/connection-info")), "exact match HTTPS origin");
Check(settings.IsNameServer("127.0.0.1:5058") && !settings.IsNameServer("127.0.0.1:5055"), "NameServer vs Master");
Check(settings.IsKromaRecAuth(App, null) &&
      settings.IsKromaRecAuth(App, "198.51.100.1:5058") &&
      settings.IsKromaRecAuth("another-app", "127.0.0.1:5058") &&
      !settings.IsKromaRecAuth("another-app", "127.0.0.1:5056"), "unverified auth gate scope");

var types = typeof(GICDPFOMCGN).Assembly;
var (regular, once) = PhotonBindings.Resolve(types, typeof(ExitGames.Client.Photon.ConnectionProtocol));
Check(regular.Name == "IGAAMJJJFKB" && once.Name == "CADOCIIEJAD", "resolve both confirmed auth methods");
try
{
    PhotonBindings.Resolve(types, typeof(bool));
    throw new Exception("wrong Photon transport type accepted");
}
catch (MissingMemberException) { }
try
{
    PhotonBindings.Resolve(typeof(string).Assembly, typeof(ExitGames.Client.Photon.ConnectionProtocol));
    throw new Exception("missing obfuscated Photon types accepted");
}
catch (MissingMemberException) { }

const string Baseline = "UserId: kr_user, GetParameters: , Token available: False";
Check(ProbeLabels.ClassifyString(Baseline, "UserId: kr_marker, Token available: False", "kr_user", "kr_marker") == "UserId" &&
      ProbeLabels.ClassifyString(Baseline, "UserId: kr_user, GetParameters: kr_marker", "kr_user", "kr_marker") == "AuthGetParameters" &&
      ProbeLabels.ClassifyString(Baseline, "UserId: kr_user, Token available: True", "kr_user", "kr_marker") == "Token",
      "detect synthetic label changes");
Check(ProbeLabels.ClassifyString(Baseline, "LCBFGLBDFHG", "kr_user", "kr_marker") == "INCONCLUSIVE" &&
      ProbeLabels.ClassifyString(Baseline, "UserId: kr_marker_suffix", "kr_user", "kr_marker") == "INCONCLUSIVE" &&
      ProbeLabels.ClassifyString(Baseline, "UserId: kr_marker, Token available: True", "kr_user", "kr_marker") == "AMBIGUOUS" &&
      ProbeLabels.Value("AuthType: Custom, AuthType: None", "AuthType") == null, "refuse ambiguous labels");
foreach (var url in new[] { "http://ns.rec.test", "https://u@ns.rec.test", "https://ns.rec.test/path",
                            "https://ns.rec.test/?key=value", "https://ns.rec.test/#fragment" })
    Invalid(() => ClientSettings.Parse(url, "https://match.rec.test", "127.0.0.1", 5058, "us", App));
Invalid(() => ClientSettings.Parse("https://ns.rec.test", "https://match.rec.test", "::1", 5058, "us", App));
Invalid(() => ClientSettings.Parse("https://ns.rec.test", "https://match.rec.test", "127.0.0.1", 0, "us", App));
Invalid(() => ClientSettings.Parse("https://ns.rec.test", "https://match.rec.test", "127.0.0.1", 5058, "US", "bad"));

var cache = new PhotonTicketCache();
var jwt = Token(Now + 900);
var first = cache.Invalidate();
Check(cache.Complete(first, Response(jwt), settings, Now), "valid ticket");
Check(cache.Get(Now).Parameter216 == "token=" + jwt && cache.Get(Now).Subject == "123", "custom auth parameter and subject");
Check(cache.Get(Now + 895) == null, "expiry margin");
var second = cache.Invalidate();
Check(!cache.Complete(first, Response(jwt), settings, Now) && cache.Get(Now) == null, "ignore stale login callback");
foreach (var body in new[] { "{", "{}", Response(Token(Now)), Response(Token(Now+900, "wrong")),
                             Response(Token(Now+900, sub: "00123")), Response(jwt, region: "eu"),
                             Response("a.b.c&token=bad"), Response(jwt, app: "wrong") })
    Check(!cache.Complete(second, body, settings, Now) && cache.Get(Now) == null, "reject bad ticket");
Check(cache.Complete(second, Response(jwt), settings, Now), "fresh callback accepted");
cache.Invalidate();
Check(cache.Get(Now) == null, "logout clears ticket");
// Simulate overlapping callbacks after a disconnect and an account switch.
var beforeReconnect = cache.Invalidate();
var afterReconnect = cache.Invalidate();
var reconnectJwt = Token(Now + 910, sub: "456");
Check(cache.Complete(afterReconnect, Response(reconnectJwt), settings, Now), "new connection-info accepted on reconnect");
Check(!cache.Complete(beforeReconnect, Response(jwt), settings, Now) && cache.Get(Now).Subject == "456",
      "late callback cannot replace the reconnect account");
Check(cache.Get(Now + 905) == null, "reconnect ticket also expires");
Check(!cache.Complete(cache.Invalidate(), Response(Token(long.MinValue)), settings, Now), "expired malformed lifetime");
var discovery = JsonSerializer.Serialize(new
{
    API = "https://api.rec.test", Auth = "https://auth.rec.test", Accounts = "https://accounts.rec.test",
    Matchmaking = "https://MATCH.rec.test:443/", Rooms = "https://rooms.rec.test", CDN = "https://cdn.rec.test",
    Notifications = "https://notify.rec.test", PlayerSettings = "https://playersettings.rec.test"
});
var trace = StartupTrace.Read(discovery, settings, out var outcome);
Check(trace != null && outcome == "service origins accepted", "discovery map and normalized MatchUrl");
Check(trace.Label(new Uri("https://api.rec.test/api/versioncheck/v4?v=20230414&marker=secret"), get: true, post: false) == "version" &&
      trace.Label(new Uri("https://cdn.rec.test/config/LoadingScreenTipData"), get: true, post: false) == "cdn-bootstrap" &&
      trace.Label(new Uri("https://accounts.rec.test/account/me"), get: true, post: false) == "account" &&
      trace.Label(new Uri("https://unrelated.test/account/me"), get: true, post: false) == null &&
      trace.Label(new Uri("https://accounts.rec.test/account/secret"), get: true, post: false) == null, "fixed labels and exact origins");
Check(trace.IsTokenEndpoint(new Uri("https://auth.rec.test/connect/token")) &&
      !trace.IsTokenEndpoint(new Uri("https://unrelated.test/connect/token")) &&
      !trace.IsTokenEndpoint(new Uri("http://auth.rec.test/connect/token")), "only the discovered HTTPS auth origin");
Check(trace.Label(new Uri("https://notify.rec.test/hub/v1/negotiate?private=value"), false, true) == "notify-negotiate" &&
      trace.Label(new Uri("https://rooms.rec.test/rooms/private-id"), true, false) == "rooms" &&
      trace.Label(new Uri("https://unrelated.test/hub/v1/negotiate"), false, true) == null &&
      StartupTrace.MatchLabel("/matchmake/dorm") == "dorm-match" &&
      StartupTrace.MatchLabel("/matchmake/none") == "startup-match" &&
      StartupTrace.MatchLabel("/invite") == "invite", "redacted backend 0.9 startup and social stages");
Check(StartupTrace.Read(discovery.Replace(":443/", ":444/"), settings, out outcome) == null &&
      outcome == "MatchUrl differs from discovery", "mismatched MatchUrl");
Check(StartupTrace.Read(discovery.Replace("https://cdn", "http://cdn"), settings, out outcome) == null &&
      StartupTrace.Read("[]", settings, out outcome) == null, "reject insecure or malformed discovery");
Check(StartupTrace.TryResult("{\"errorCode\":20,\"message\":\"ignored\"}", "errorCode", out var code, out var noIds) && code == 20 && noIds == null &&
      !StartupTrace.TryResult("{\"errorCode\":\"secret\"}", "errorCode", out _, out _), "numeric-only diagnostic payload");
Check(StartupTrace.TryResult("{\"errorCode\":0,\"roomInstance\":{\"roomId\":12,\"roomInstanceId\":34,\"photonRoomId\":\"private\"}}",
      "errorCode", out var matchResult, out var roomIds) && matchResult == 0 && roomIds == (12, 34) &&
      StartupTrace.TryResult("{\"errorCode\":3,\"roomInstance\":{\"roomId\":12,\"roomInstanceId\":34}}",
      "errorCode", out _, out var deniedIds) && deniedIds == null, "log only numeric room identifiers on success");
Check(StartupTrace.LooksLikeSceneFailure("Scene 'sensitive-path' couldn't be loaded") &&
      !StartupTrace.LooksLikeSceneFailure("Background request failed to load"), "scene failure classification");
Check(StartupTrace.MatchLabel("/player/verifylock") == "session-verify-lock" &&
      StartupTrace.MatchLabel("/player/heartbeat") == "session-heartbeat" &&
      StartupTrace.MatchLabel("/player/exclusivelogin") == "session-exclusive-login" &&
      StartupTrace.MatchLabel("/matchmake/room/private-id") == "matchmake" &&
      StartupTrace.MatchLabel("/player/sensitive-path") == null, "fixed reconnect diagnostic labels");
Check(StartupTrace.ResultField("session-verify-lock") == null &&
      StartupTrace.ResultField("session-heartbeat") == "errorCode" &&
      StartupTrace.ResultField("session-exclusive-login") == "errorCode", "reconnect diagnostic result fields");
Console.WriteLine("PASS: Photon binding discovery, diagnostic labels, config, routing, startup traces, and ticket expiry/reconnect.");

static void Check(bool pass, string test)
{
    if (!pass) throw new Exception(test);
}

static void Invalid(Action action)
{
    try { action(); }
    catch (ArgumentException) { return; }
    throw new Exception("invalid configuration accepted");
}

static string Base64(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
    .TrimEnd('=').Replace('+', '-').Replace('/', '_');

static string Token(long expires, string aud = App, string sub = "123") =>
    Base64("{\"alg\":\"HS256\"}") + "." + Base64(JsonSerializer.Serialize(new
        System.Collections.Generic.Dictionary<string, object>
        { ["sub"] = sub, ["aud"] = aud, ["exp"] = expires, ["rn.env"] = "prod", ["kromarec_use"] = "photon-realtime" }))
    + "." + Base64("test-signature");

static string Response(string jwt, string region = "us", string app = App) => JsonSerializer.Serialize(new
    { success = true, value = new { photonAuthToken = jwt, photonRealtimeAppId = app, photonRegion = region } });
