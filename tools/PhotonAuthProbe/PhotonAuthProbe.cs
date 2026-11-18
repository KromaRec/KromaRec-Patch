using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace RecNetPlugin;

[BepInPlugin("net.kromarec.photon-auth-probe", "KromaRec Photon Auth Probe", "1.2.0")]
public sealed class PhotonAuthProbe : BasePlugin
{
    private const string User = "kr_probe_user";
    private const string Marker = "kr_probe_marker";
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

    private static readonly string[] Candidates =
    {
        "_MPGGEFPDICH_k__BackingField",
        "_LGKMOEHCOOP_k__BackingField",
        "_JEHPFMLLKBE_k__BackingField"
    };

    public override void Load()
    {
        Report("BEGIN");
        try
        {
            var assembly = Assembly.Load("PhotonRealtime");
            using (var stream = File.OpenRead(assembly.Location))
            using (var sha = SHA256.Create())
                Report($"PhotonRealtime SHA256={Convert.ToHexString(sha.ComputeHash(stream))}");

            var auth = assembly.GetType("LCBFGLBDFHG", throwOnError: true)!;
            var constructor = auth.GetConstructor(new[] { typeof(string) });
            if (constructor == null)
            {
                Report("ERROR stage=constructor result=MISSING_STRING_CONSTRUCTOR");
                return;
            }

            var baseline = NewSample(constructor).ToString() ?? string.Empty;
            var strings = auth.GetProperties(Members)
                .Where(property => Writable(property) && property.PropertyType == typeof(string))
                .Select(property => property.Name).Union(Candidates).OrderBy(name => name, StringComparer.Ordinal);
            foreach (var name in strings)
                TestString(auth, constructor, baseline, name);

            var enums = auth.GetProperties(Members)
                .Where(property => Writable(property) && property.PropertyType.IsEnum).ToArray();
            if (enums.Length == 0)
                Report("AUTH_TYPE result=NO_WRITABLE_ENUM_PROPERTY");
            foreach (var property in enums)
                TestEnum(constructor, property);
        }
        catch (Exception error)
        {
            Report($"ERROR stage=setup type={error.GetBaseException().GetType().Name}");
        }
        finally
        {
            Report("END");
        }
    }

    private void TestString(Type auth, ConstructorInfo constructor, string baseline, string name)
    {
        try
        {
            var property = auth.GetProperty(name, Members);
            if (property == null || !Writable(property) || property.PropertyType != typeof(string))
            {
                Report($"FIELD {name} result=MISSING_STRING_PROPERTY");
                return;
            }

            var sample = NewSample(constructor);
            property.SetValue(sample, Marker);
            var result = string.Equals(property.GetValue(sample) as string, Marker, StringComparison.Ordinal)
                ? ProbeLabels.ClassifyString(baseline, sample.ToString() ?? string.Empty, User, Marker)
                : "WRITE_NOT_CONFIRMED";
            Report($"FIELD {name} result={result}");
        }
        catch (Exception error)
        {
            Report($"FIELD {name} result=ERROR type={error.GetBaseException().GetType().Name}");
        }
    }

    private void TestEnum(ConstructorInfo constructor, PropertyInfo property)
    {
        try
        {
            var type = property.PropertyType;
            var names = Enum.GetNames(type);
            if (!names.Contains("Custom") || !names.Contains("None"))
            {
                Report($"AUTH_TYPE {property.Name} enum={type.FullName} constants={string.Join(",", names)} result=UNRESOLVED_CONSTANTS");
                return;
            }

            var custom = Enum.Parse(type, "Custom");
            var none = Enum.Parse(type, "None");
            var sample = NewSample(constructor);
            property.SetValue(sample, none);
            var noneLabel = AuthTypeLabel(sample);
            var noneRead = Equals(property.GetValue(sample), none);
            property.SetValue(sample, custom);
            var customLabel = AuthTypeLabel(sample);
            var evidence = !Equals(none, custom) && noneRead && Equals(property.GetValue(sample), custom) &&
                           noneLabel == "None" && customLabel == "Custom";
            Report($"AUTH_TYPE {property.Name} enum={type.FullName} result={(evidence ? "NAMED_LABEL_TRANSITION" : "INCONCLUSIVE")}");
        }
        catch (Exception error)
        {
            Report($"AUTH_TYPE {property.Name} result=ERROR type={error.GetBaseException().GetType().Name}");
        }
    }

    private static string? AuthTypeLabel(object sample)
    {
        var description = sample.ToString() ?? string.Empty;
        return ProbeLabels.Value(description, "AuthType") ?? ProbeLabels.Value(description, "AuthenticationType");
    }

    private static bool Writable(PropertyInfo property) =>
        property.GetMethod?.IsPublic == true && property.SetMethod?.IsPublic == true &&
        property.GetIndexParameters().Length == 0;

    private static object NewSample(ConstructorInfo constructor) => constructor.Invoke(new object[] { User });

    private void Report(string line) => Log.LogInfo($"[KR-PHOTON-PROBE] {line}");
}
