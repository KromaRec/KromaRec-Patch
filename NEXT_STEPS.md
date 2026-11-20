# First Windows build

Open x64 PowerShell in the patch checkout. Install SDK 6.0.428. Use an authorized copy of Steam app 471710, depot 471711, manifest 6426603215211043630.

```powershell
$ErrorActionPreference = 'Stop'
$game = 'C:\Games\KromaRec-20230414'
dotnet --version
.\scripts\Test-NativeReadiness.ps1 -GamePath $game -ConfigPath .\config\net.kromarec.patch.cfg
```

For an existing installation with generated interop, use the Steam-authorized DepotDownloader 3.4.0 manifest-only export. The identity check compares the record's depot and manifest IDs and the sizes and SHA-1 values of RecRoom.exe, GameAssembly.dll, UnityPlayer.dll and global-metadata.dat. Use only a trusted export. The result identifies those four files, not the entire installation. The observed GameAssembly SHA-256 from the previous run is not an independent reference.

```powershell
$dir = 'C:\Games\KromaRec-manifest'
$record = Join-Path $dir 'manifest_471711_6426603215211043630.txt'
# If you do not already have the trusted manifest export:
if (!(Test-Path -LiteralPath $record)) { .\scripts\Get-Game.ps1 -GamePath $game -ManifestDirectory $dir -ManifestOnly }
.\scripts\Test-NativeReadiness.ps1 -GamePath $game -ManifestPath $record -ConfigPath .\config\net.kromarec.patch.cfg
.\build.ps1 -GamePath $game -ManifestPath $record -VerifyOnly -ReportPath .\photon-interop.txt
.\build.ps1 -GamePath $game -ManifestPath $record
```

A supplied but unreadable, incorrect or mismatched record stops the build. Keep the export outside the game directory. If Steam cannot supply a record, metadata-only verification remains available without -ManifestPath, with depot identity unverified.

If the game is missing, obtain the pinned depot in an empty directory with the command below. For an existing depot, confirm its Steam provenance. On a clean depot install the pinned loader once.

```powershell
# Only for a new download:
.\scripts\Get-Game.ps1 -GamePath $game -ManifestDirectory 'C:\Games\KromaRec-manifest'
# Only for a clean depot without BepInEx:
.\scripts\Get-BuildLoader.ps1 -Destination $game
```

Launch that installation normally with its original security checks, allow BepInEx to finish generating interop, then close the game. Stop if the bootstrap or protected startup fails. From the same PowerShell session:

```powershell
.\build.ps1 -GamePath $game -VerifyOnly -ReportPath .\photon-interop.txt
.\build.ps1 -GamePath $game -Install
notepad.exe (Join-Path $game 'BepInEx\config\net.kromarec.patch.cfg')
```

Set the real discovery and Match HTTPS origins, NameServer host/port, region and Realtime UUID. For an acceptance run enable General.Debug. Launch normally, observe startup, close the game and collect:

```powershell
.\scripts\Test-NativeReadiness.ps1 -GamePath $game | Tee-Object -FilePath .\native-readiness.txt
```

Share the readiness and interop reports. If the build fails, also provide the first compiler code and member name with local paths removed. Run the isolated synthetic-value probe in PHOTON_DIAGNOSTICS.md separately from the main plugin. Native auth remains blocked pending serialization, encryption and handoff evidence.
