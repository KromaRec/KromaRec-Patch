using System;
using System.Reflection;

namespace RecNetPlugin;

// Candidates from the supplied 20230414 report. Resolve verifies the installed metadata at startup.
internal static class PhotonBindings
{
    internal const string PeerName = "GICDPFOMCGN";
    internal const string AuthName = "LCBFGLBDFHG";
    internal const string EncryptionModeName = "PPHMFCHPECA";
    internal const string RegularName = "IGAAMJJJFKB";
    internal const string OnceName = "CADOCIIEJAD";
    internal const string ProtocolName = "ExitGames.Client.Photon.ConnectionProtocol";
    internal const string BasePeerName = "ExitGames.Client.Photon.PhotonPeer";

    internal static (MethodInfo Regular, MethodInfo Once) Resolve(Assembly realtime, Type protocol)
    {
        var peer = realtime.GetType(PeerName);
        var auth = realtime.GetType(AuthName);
        var encryption = realtime.GetType(EncryptionModeName);
        if (peer?.BaseType?.FullName != BasePeerName || auth == null || !auth.IsClass ||
            encryption?.IsEnum != true || protocol?.FullName != ProtocolName || !protocol.IsEnum)
            throw new MissingMemberException("20230414 Photon authentication types");

        return (Require(peer, RegularName, typeof(string), typeof(string), auth, typeof(string), typeof(bool)),
                Require(peer, OnceName, typeof(string), typeof(string), auth, typeof(string), encryption, protocol));
    }

    private static MethodInfo Require(Type peer, string methodName, params Type[] parameters)
    {
        var method = peer.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.DeclaredOnly, null, parameters, null);
        if (method == null || method.IsAbstract || method.IsGenericMethod || method.ReturnType != typeof(bool))
            throw new MissingMethodException(peer.FullName, methodName);
        return method;
    }
}
