using System;
using System.IO;
using BepInEx.Configuration;
using RecNetPlugin;

bool validate = args.Length == 2 && args[1] == "--validate";
var temporary = Path.GetTempFileName();
try
{
    File.Copy(args[0], temporary, true);
    var config = new ConfigFile(temporary, false) { SaveOnConfigSet = false };
    if (!validate && (!config.Bind("General", "Enabled", false).Value || config.Bind("General", "Debug", true).Value))
        throw new Exception("General flags were not parsed from the example.");
    var settings = ClientSettings.Parse(
        config.Bind("Backend", "BaseUrl", "").Value,
        config.Bind("Backend", "MatchUrl", "").Value,
        config.Bind("Photon", "NameServerHost", "").Value,
        config.Bind("Photon", "NameServerPort", 0).Value,
        config.Bind("Photon", "Region", "").Value,
        config.Bind("Photon", "RealtimeAppId", "").Value);
    if (!validate && (settings.NamePort != 5058 || settings.Region != "us"))
        throw new Exception("Photon defaults differ from the example.");
    if (validate)
        Console.WriteLine("CONFIG_VALID");
    else Console.WriteLine("PASS: BepInEx 6.0.0-pre.2 ConfigFile parsed the shipped example.");
}
catch (Exception) when (validate)
{
    Console.WriteLine("CONFIG_INVALID");
    return 1;
}
finally
{
    File.Delete(temporary);
}
return 0;
