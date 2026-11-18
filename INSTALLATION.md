# Installing KromaRec Patch

For member verification, install the standalone probe using [PHOTON_DIAGNOSTICS.md](PHOTON_DIAGNOSTICS.md). The preview patch below supports startup diagnostics while KromaRec Photon authentication is gated.

Complete [BUILDING.md](BUILDING.md) first and keep the game closed. Use the dedicated 20230414 copy you built against. Move any old RecFlare plugin or duplicate KromaRec Patch DLL to a folder outside `BepInEx\plugins` so only the candidate loads.

From the patch folder in PowerShell:

```powershell
$game = 'C:\Games\KromaRec-20230414'
$plugins = Join-Path $game 'BepInEx\plugins'
$configDir = Join-Path $game 'BepInEx\config'
New-Item -ItemType Directory -Force -Path $plugins, $configDir | Out-Null
Copy-Item -LiteralPath '.\bin\Release\net6.0\KromaRec.Patch.dll' -Destination $plugins -Force
$config = Join-Path $configDir 'net.kromarec.patch.cfg'
if (!(Test-Path -LiteralPath $config)) {
    Copy-Item -LiteralPath '.\config\net.kromarec.patch.cfg' -Destination $config
}
notepad.exe $config
```

Set `Debug = true` under `[General]` for acceptance. Set the actual `Backend.BaseUrl`, `Backend.MatchUrl` and Photon host, port, region and Realtime App ID from [CONFIGURATION.md](CONFIGURATION.md). Save and close the editor.

```powershell
Start-Process -FilePath (Join-Path $game 'RecRoom.exe') -WorkingDirectory $game -ArgumentList '+forcemode:screen'
```

Open the BepInEx log with the newer timestamp if both `LogOutput.log` and `LogOutput.txt` are present. Look for the plugin load line, both Photon guards, and the BestHTTP hook. Use [NATIVE_ACCEPTANCE.md](NATIVE_ACCEPTANCE.md) for the startup checks now and the multiplayer sequence after the authentication binding is implemented. Keep raw logs local and share the first failed stage and relevant sanitized lines.

Close the game and remove `KromaRec.Patch.dll` from `BepInEx\plugins` to uninstall the manual patch. The Phase 1 graphical installer targets patch 0.1.0. This guide installs the preview DLL directly.
