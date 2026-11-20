using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RecNetPlugin;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class InteropVerifier
{
    internal static int Run(string game)
    {
        var interop = Path.Combine(game, "BepInEx", "interop");
        if (!Directory.Exists(interop))
        {
            Console.WriteLine("KRBUILD_METADATA_INPUT Missing BepInEx/interop. Launch the game with the pinned loader first.");
            return 2;
        }
        var assemblies = new List<AssemblyDefinition>();
        try
        {
            foreach (var path in Directory.GetFiles(interop, "*.dll").OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                try { assemblies.Add(AssemblyDefinition.ReadAssembly(path)); }
                catch (Exception error) when (error is BadImageFormatException or IOException or ArgumentException)
                {
                    Console.WriteLine($"KRBUILD_METADATA_READ type={error.GetType().Name}; regenerate interop with the pinned loader.");
                    return 3;
                }
            }
            var types = assemblies.SelectMany(assembly => Flatten(assembly.MainModule.Types)).ToLookup(type => type.FullName);
            var failures = new List<string>();
            void Require(bool condition, string member)
            {
                Console.WriteLine($"{(condition ? "PASS" : "MISSING")} {member}");
                if (!condition) failures.Add(member);
            }

            bool Method(string type, string name, string returns, bool? isStatic, params string[] parameters) =>
                types[type].SelectMany(t => t.Methods).Count(method =>
                    method.Name == name && !method.HasGenericParameters && (returns == null || method.ReturnType.FullName == returns) &&
                    (isStatic == null || method.IsStatic == isStatic) && !method.IsAbstract &&
                    method.Parameters.Select(param => param.ParameterType.FullName).SequenceEqual(parameters)) == 1;

            bool WritableMember(string type, string name, string result) =>
                types[type].Any(t => t.Properties.Any(property =>
                    property.Name == name && property.PropertyType.FullName == result && property.GetMethod?.IsPublic == true &&
                    property.SetMethod?.IsPublic == true && !property.GetMethod.IsStatic &&
                    !property.SetMethod.IsStatic && property.Parameters.Count == 0) || t.Fields.Any(field =>
                    field.Name == name && field.FieldType.FullName == result && field.IsPublic && !field.IsStatic && !field.IsInitOnly && !field.IsLiteral));

            bool Event(string type, string name) =>
                types[type].SelectMany(t => t.Events).Any(e => e.Name == name && e.AddMethod?.IsPublic == true && e.AddMethod.IsStatic);

            Console.WriteLine($"Discovered future Photon settings factory: {Method("HPEENKELKDJ", "MGKINLFMJLB", "Photon.Realtime.AppSettings", false)}");
            Require(Method("BestHTTP.HTTPManager", "SendRequestImpl", null, true, "BestHTTP.HTTPRequest"), "BestHTTP.SendRequestImpl");
            foreach (var (member, type) in new[] { ("MaxRedirects", "System.Int32"), ("DisableCache", "System.Boolean"),
                         ("Uri", "Il2CppSystem.Uri"), ("Callback", "BestHTTP.OnRequestFinishedDelegate") })
                Require(WritableMember("BestHTTP.HTTPRequest", member, type), "BestHTTP.HTTPRequest." + member);
            Require(Method("BestHTTP.OnRequestFinishedDelegate", "Invoke", "System.Void", false,
                "BestHTTP.HTTPRequest", "BestHTTP.HTTPResponse"), "BestHTTP.OnRequestFinishedDelegate.Invoke");

            void DescribeOptionalEvent(string owner, string name)
            {
                var owners = types[owner].ToArray();
                Console.WriteLine($"OPTIONAL {owner}.{name} event={Event(owner, name)} owners={owners.Length} " +
                    $"add_methods={owners.SelectMany(t => t.Methods).Count(m => m.Name == "add_" + name)} " +
                    $"event_rows={owners.SelectMany(t => t.Events).Count(e => e.Name == name)} " +
                    $"fields={owners.SelectMany(t => t.Fields).Count(f => f.Name == name)} " +
                    $"properties={owners.SelectMany(t => t.Properties).Count(p => p.Name == name)}");
            }
            DescribeOptionalEvent("UnityEngine.SceneManagement.SceneManager", "sceneLoaded");
            DescribeOptionalEvent("UnityEngine.Application", "logMessageReceived");

            foreach (var candidate in types.SelectMany(group => group).Where(type =>
                         type.Module.Assembly.Name.Name == "PhotonRealtime" && type.BaseType?.FullName == PhotonBindings.BasePeerName))
            {
                Console.WriteLine($"PEER_CANDIDATE {candidate.FullName}");
                foreach (var method in candidate.Methods.Where(method => !method.IsStatic &&
                             method.ReturnType.FullName == "System.Boolean" && method.Parameters.Count >= 5 &&
                             method.Parameters[0].ParameterType.FullName == "System.String" &&
                             method.Parameters[1].ParameterType.FullName == "System.String"))
                    Console.WriteLine($"AUTH_CANDIDATE {method.FullName}");
            }

            var peers = types[PhotonBindings.PeerName].Where(t => t.Module.Assembly.Name.Name == "PhotonRealtime" &&
                t.BaseType?.FullName == PhotonBindings.BasePeerName).ToArray();
            bool AuthMethod(string name, params string[] parameters)
            {
                var candidates = peers.SelectMany(peer => peer.Methods).Where(method => method.Name == name).ToArray();
                foreach (var method in candidates)
                    Console.WriteLine($"AUTH_METHOD {method.FullName} static={method.IsStatic} abstract={method.IsAbstract}");
                return candidates.Count(method => !method.IsStatic && !method.IsAbstract && !method.HasGenericParameters &&
                    method.ReturnType.FullName == "System.Boolean" &&
                    method.Parameters.Select(parameter => parameter.ParameterType.FullName).SequenceEqual(parameters)) == 1;
            }

            var regular = AuthMethod(PhotonBindings.RegularName, "System.String", "System.String",
                PhotonBindings.AuthName, "System.String", "System.Boolean");
            var once = AuthMethod(PhotonBindings.OnceName, "System.String", "System.String",
                PhotonBindings.AuthName, "System.String", PhotonBindings.EncryptionModeName, PhotonBindings.ProtocolName);
            Require(peers.Length == 1, "Photon peer");
            Require(regular, "20230414 Photon Authenticate signature");
            Require(once, "20230414 Photon AuthenticateOnce signature");
            var realtimePath = Path.Combine(interop, "PhotonRealtime.dll");
            if (File.Exists(realtimePath))
            {
                using var stream = File.OpenRead(realtimePath);
                using var sha = SHA256.Create();
                Console.WriteLine($"PhotonRealtime SHA256={Convert.ToHexString(sha.ComputeHash(stream))}");
            }

            var authTypes = types[PhotonBindings.AuthName].Where(t => t.Module.Assembly.Name.Name == "PhotonRealtime" &&
                t.IsClass && !t.IsEnum).ToArray();
            Require(authTypes.Length == 1, "Photon auth object type");
            Require(types[PhotonBindings.EncryptionModeName].Count(t =>
                             t.Module.Assembly.Name.Name == "PhotonRealtime" && t.IsEnum) == 1, "Photon encryption enum");
            Require(types[PhotonBindings.ProtocolName].Any(t => t.IsEnum), "Photon protocol enum");
            string WrapperFields(MethodDefinition accessor) => accessor.HasBody
                ? string.Join(",", accessor.Body.Instructions
                    .Where(instruction => instruction.OpCode == OpCodes.Ldsfld &&
                        instruction.Operand is FieldReference field && field.Name.StartsWith("NativeFieldInfoPtr_", StringComparison.Ordinal))
                    .Select(instruction => ((FieldReference)instruction.Operand).Name).Distinct())
                : "";

            foreach (var auth in authTypes)
            {
                Console.WriteLine($"AUTH_TYPE {auth.FullName} base={auth.BaseType?.FullName}");
                foreach (var field in auth.Fields.Where(field => !field.IsStatic))
                    Console.WriteLine($"AUTH_FIELD {field.Name} type={field.FieldType.FullName}");
                foreach (var constructor in auth.Methods.Where(method => method.IsConstructor && !method.IsStatic))
                    Console.WriteLine($"AUTH_CONSTRUCTOR {constructor.FullName}");
                foreach (var property in auth.Properties.Where(p => p.GetMethod?.IsPublic == true &&
                             !p.GetMethod.IsStatic && p.SetMethod?.IsPublic == true && !p.SetMethod.IsStatic &&
                             p.Parameters.Count == 0))
                {
                    Console.WriteLine($"AUTH_PROPERTY {property.Name} type={property.PropertyType.FullName} " +
                        $"getter_fields=[{WrapperFields(property.GetMethod)}] setter_fields=[{WrapperFields(property.SetMethod)}]");
                    foreach (var field in types[property.PropertyType.FullName]
                                 .Where(type => type.IsEnum && type.Module == auth.Module)
                                 .SelectMany(type => type.Fields).Where(field => field.IsLiteral && field.HasConstant))
                        Console.WriteLine($"AUTH_ENUM {property.PropertyType.FullName} {field.Name}={field.Constant}");
                }
            }
            foreach (var constant in types.SelectMany(group => group).Where(type =>
                         type.Module.Assembly.Name.Name is "PhotonRealtime" or "Photon3Unity3D")
                     .SelectMany(type => type.Fields).Where(field => field.IsLiteral && field.HasConstant &&
                         field.FieldType.FullName is "System.Byte" or "System.Int16" or "System.Int32"))
            {
                var value = Convert.ToInt64(constant.Constant);
                if (value is 216 or 217 or 221 or 225 or 230 or 231)
                    Console.WriteLine($"PHOTON_CONSTANT {constant.DeclaringType.FullName}.{constant.Name}={value}");
            }
            Console.WriteLine($"HARMONY_TARGET regular={regular && peers.Length == 1} once={once && peers.Length == 1}");
            Console.WriteLine("NATIVE_ROLES=UNVERIFIED ENCRYPTION=UNVERIFIED HANDOFF=UNVERIFIED");
            Console.WriteLine("Authentication member semantics require native evidence. KromaRec Photon auth is gated.");
            foreach (var failure in failures) Console.WriteLine($"REQUIRED_FAILURE {failure}");
            var valid = failures.Count == 0;
            Console.WriteLine("Interop metadata check only. Confirm Harmony binding and callback execution in the game.");
            Console.WriteLine($"KRBUILD_METADATA={(valid ? "PASS" : "FAIL")} scope=managed-signatures-only");
            return valid ? 0 : 3;
        }
        finally
        {
            foreach (var assembly in assemblies)
                assembly.Dispose();
        }
    }

    private static IEnumerable<TypeDefinition> Flatten(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;
            foreach (var nested in Flatten(type.NestedTypes))
                yield return nested;
        }
    }
}
