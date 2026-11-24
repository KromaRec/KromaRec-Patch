using System;
using System.Linq;
using System.Reflection;

namespace RecNetPlugin;

internal static class ProbeReadback
{
    internal static void Run(Type auth, ConstructorInfo constructor, Action<string> report)
    {
        const string first = "kr_probe_first";
        const string second = "kr_probe_second";
        object New(string value) => constructor.Invoke(new object[] { value });
        var properties = auth.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(p => p.GetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();

        foreach (var reader in properties.Where(p => p.PropertyType == typeof(string)))
        {
            try
            {
                var follows = Equals(reader.GetValue(New(first)), first) && Equals(reader.GetValue(New(second)), second);
                report($"CTOR_STRING reader={reader.Name} follows={follows}");
            }
            catch (Exception e) { report($"CTOR_STRING reader={reader.Name} error={e.GetBaseException().GetType().Name}"); }
        }
        foreach (var writer in properties.Where(p => p.SetMethod?.IsPublic == true))
        {
            var type = writer.PropertyType;
            object a = first, b = second;
            if (type.IsEnum && Enum.IsDefined(type, "None") && Enum.IsDefined(type, "Custom"))
            {
                a = Enum.Parse(type, "None");
                b = Enum.Parse(type, "Custom");
                if (Equals(a, b)) continue;
            }
            else if (type != typeof(string)) continue;
            foreach (var reader in properties.Where(p => p.PropertyType == type))
            {
                try
                {
                    var sample = New("kr_probe_ctor");
                    writer.SetValue(sample, a);
                    var before = reader.GetValue(sample);
                    writer.SetValue(sample, b);
                    var after = reader.GetValue(sample);
                    report($"READBACK writer={writer.Name} reader={reader.Name} follows={Equals(before, a) && Equals(after, b)}");
                }
                catch (Exception e) { report($"READBACK writer={writer.Name} reader={reader.Name} error={e.GetBaseException().GetType().Name}"); }
            }
        }
    }
}
