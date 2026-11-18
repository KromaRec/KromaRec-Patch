#nullable enable

using System.Text.RegularExpressions;

namespace RecNetPlugin;

internal static class ProbeLabels
{
    internal static string? Value(string description, string label)
    {
        var matches = Regex.Matches(description,
            $@"\b{Regex.Escape(label)}\s*:\s*([A-Za-z0-9_]+)(?=[,\s\])}}]|$)",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        return matches.Count == 1 ? matches[0].Groups[1].Value : null;
    }

    internal static string ClassifyString(string baseline, string changed, string user, string marker)
    {
        var userId = Value(baseline, "UserId") == user && Value(changed, "UserId") == marker;
        var parameters = Value(baseline, "GetParameters") != marker &&
                         Value(changed, "GetParameters") == marker && Value(changed, "UserId") == user;
        var token = string.Equals(Value(baseline, "Token available"), "false", System.StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Value(changed, "Token available"), "true", System.StringComparison.OrdinalIgnoreCase);
        var matches = (userId ? 1 : 0) + (parameters ? 1 : 0) + (token ? 1 : 0);
        return matches switch
        {
            0 => "INCONCLUSIVE",
            1 => userId ? "UserId" : parameters ? "AuthGetParameters" : "Token",
            _ => "AMBIGUOUS"
        };
    }
}
