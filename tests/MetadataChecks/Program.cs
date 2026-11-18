using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

var root = Path.Combine(Path.GetTempPath(), "kr-metadata-" + Guid.NewGuid().ToString("N"));
var interop = Path.Combine(root, "BepInEx", "interop");
Directory.CreateDirectory(interop);
try
{
    Check(InteropVerifier.Run(Path.Combine(root, "absent")) == 2, "missing interop directory");
    using var assembly = AssemblyDefinition.CreateAssembly(
        new AssemblyNameDefinition("PhotonRealtime", new Version(1, 0)), "PhotonRealtime", ModuleKind.Dll);
    var module = assembly.MainModule;
    TypeDefinition Type(string ns, string name, TypeReference parent = null)
    {
        var value = new TypeDefinition(ns, name, TypeAttributes.Public, parent ?? module.TypeSystem.Object);
        module.Types.Add(value);
        return value;
    }
    MethodDefinition Method(TypeDefinition owner, string name, TypeReference returns, bool isStatic,
                            params TypeReference[] parameters)
    {
        var method = new MethodDefinition(name, MethodAttributes.Public |
            (isStatic ? MethodAttributes.Static : (MethodAttributes)0), returns);
        foreach (var parameter in parameters)
            method.Parameters.Add(new ParameterDefinition(parameter));
        owner.Methods.Add(method);
        if (returns.FullName == "System.Boolean") method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        if (returns.FullName != "System.Void" && returns.FullName != "System.Boolean")
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        return method;
    }
    TypeDefinition Enum(string ns, string name)
    {
        var type = Type(ns, name, module.ImportReference(typeof(System.Enum)));
        type.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName |
            FieldAttributes.RTSpecialName, module.TypeSystem.Int32));
        return type;
    }
    void Property(TypeDefinition owner, string name, TypeReference type)
    {
        var property = new PropertyDefinition(name, PropertyAttributes.None, type)
        {
            GetMethod = Method(owner, "get_" + name, type, false),
            SetMethod = Method(owner, "set_" + name, module.TypeSystem.Void, false, type)
        };
        owner.Properties.Add(property);
    }

    var peerBase = Type("ExitGames.Client.Photon", "PhotonPeer");
    var peer = Type("", "GICDPFOMCGN", peerBase);
    var auth = Type("", "LCBFGLBDFHG");
    var encryption = Enum("", "PPHMFCHPECA");
    var protocol = Enum("ExitGames.Client.Photon", "ConnectionProtocol");
    var regular = Method(peer, "IGAAMJJJFKB", module.TypeSystem.Boolean, false,
        module.TypeSystem.String, module.TypeSystem.String, auth, module.TypeSystem.String, module.TypeSystem.Boolean);
    Method(peer, "CADOCIIEJAD", module.TypeSystem.Boolean, false, module.TypeSystem.String,
        module.TypeSystem.String, auth, module.TypeSystem.String, encryption, protocol);
    Property(auth, "syntheticUser", module.TypeSystem.String);
    var customAuth = Enum("", "SyntheticCustomAuth");
    customAuth.Fields.Add(new FieldDefinition("Custom", FieldAttributes.Public | FieldAttributes.Static |
        FieldAttributes.Literal | FieldAttributes.HasDefault, customAuth) { Constant = 0 });
    Property(auth, "syntheticKind", customAuth);
    auth.Fields.Add(new FieldDefinition("syntheticAuth", FieldAttributes.Public, module.TypeSystem.String));
    var opcode = Type("Photon.Realtime", "OperationCode");
    opcode.Fields.Add(new FieldDefinition("Authenticate", FieldAttributes.Public | FieldAttributes.Static |
        FieldAttributes.Literal | FieldAttributes.HasDefault, module.TypeSystem.Byte) { Constant = (byte)230 });
    var request = Type("BestHTTP", "HTTPRequest");
    var response = Type("BestHTTP", "HTTPResponse");
    var callback = Type("BestHTTP", "OnRequestFinishedDelegate");
    Property(request, "MaxRedirects", module.TypeSystem.Int32);
    Property(request, "DisableCache", module.TypeSystem.Boolean);
    Property(request, "Uri", Type("Il2CppSystem", "Uri"));
    Property(request, "Callback", callback);
    Method(Type("BestHTTP", "HTTPManager"), "SendRequestImpl", module.TypeSystem.Void, true, request);
    Method(callback, "Invoke", module.TypeSystem.Void, false, request, response);
    var manager = Type("UnityEngine.SceneManagement", "SceneManager");
    manager.Events.Add(new EventDefinition("sceneLoaded", EventAttributes.None, module.TypeSystem.Object)
    {
        AddMethod = Method(manager, "add_sceneLoaded", module.TypeSystem.Void, true, module.TypeSystem.Object)
    });

    string report = "";
    int Run()
    {
        assembly.Write(Path.Combine(interop, "PhotonRealtime.dll"));
        var original = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            return InteropVerifier.Run(root);
        }
        finally { Console.SetOut(original); report = output.ToString(); }
    }
    Check(Run() == 0 && report.Contains("PHOTON_CONSTANT Photon.Realtime.OperationCode.Authenticate=230") &&
        report.Contains("AUTH_FIELD syntheticAuth") && report.Contains("AUTH_ENUM SyntheticCustomAuth Custom=0") && report.Contains("NATIVE_ROLES=UNVERIFIED"), "valid synthetic signatures and evidence boundaries");
    var wrongOverload = Method(peer, "IGAAMJJJFKB", module.TypeSystem.Boolean, false, module.TypeSystem.String);
    Check(Run() == 0 && report.Contains("IGAAMJJJFKB(System.String)"), "report additional overload without confusing the target");
    peer.Methods.Remove(wrongOverload);
    regular.IsStatic = true;
    Check(Run() == 3, "reject static auth");
    regular.IsStatic = false;
    regular.Parameters[2].ParameterType = module.TypeSystem.String;
    Check(Run() == 3, "reject wrong auth parameter");
    regular.Parameters[2].ParameterType = auth;
    peer.Name = "RenamedPeer";
    Check(Run() == 3 && report.Contains("PEER_CANDIDATE RenamedPeer"), "report renamed peer without accepting it");
    peer.Name = "GICDPFOMCGN";
    var getter = request.Properties.First(p => p.Name == "Uri").GetMethod;
    getter.IsStatic = true;
    Check(Run() == 3, "reject static request accessor");
    getter.IsStatic = false;
    Check(Run() == 0, "recover after invalid metadata");
    File.WriteAllText(Path.Combine(interop, "broken.dll"), "not metadata");
    Check(InteropVerifier.Run(root) == 3, "reject corrupt assembly");
    Console.WriteLine("PASS synthetic verifier fixtures. Native compatibility and Harmony dispatch remain UNVERIFIED.");
}
finally { Directory.Delete(root, true); }

static void Check(bool condition, string label)
{
    if (!condition) throw new Exception(label);
}
