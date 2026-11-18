# Building KromaRec Patch 0.3.1 development preview

Start with [the first Windows build checklist](NEXT_STEPS.md) for KromaRec 0.10.1. The read-only readiness command checks file availability, runs the metadata verifier when its inputs exist, and validates the chosen configuration with the loader parser and the plugin's settings validator. Before installation, pass `-ConfigPath .\config\net.kromarec.patch.cfg` to check the example. After installation omit it to check the deployed file. Configuration validity covers syntax and endpoint constraints. DNS, TLS and remote services require a live test.

`-KnownGameAssemblySha256` accepts an independently trusted hash of GameAssembly.dll from the target depot. A match identifies that file only. Confirm the Steam manifest separately. Without a reference the report gives the observed fingerprint and UNVERIFIED.

With the game closed, `.\build.ps1 -GamePath $game -Install` runs verification and compilation before copying the DLL. It preserves any existing configuration and refuses to replace a different installed DLL. Move the previous DLL outside the plugin tree before upgrading. Main build output is separate from readiness and runtime results.

Use .NET SDK 6.0.428, Rec Room Steam app 471710/depot 471711/manifest 6426603215211043630 and the [BepInEx 6.0.0-pre.2 Windows x64 IL2CPP distribution](https://github.com/BepInEx/BepInEx/releases/tag/v6.0.0-pre.2). Its CoreCLR runtime is .NET 6. The patch project references BepInEx.Core, BepInEx.Unity.IL2CPP, Il2CppInterop and HarmonyX from the installed loader's core folder to avoid compiling against a different bleeding-edge version.

## Windows x64

Open PowerShell in the extracted `KromaRec-Patch` folder. Install the **x64 SDK**, then confirm `dotnet --version` prints `6.0.428`.

If you need the depot, run the included Steam acquisition script. Sign in using the QR code shown by DepotDownloader. Use a new or empty game directory.

```powershell
$game = 'C:\Games\KromaRec-20230414'
.\scripts\Get-Game.ps1 -GamePath $game -ManifestDirectory 'C:\Games\KromaRec-manifest'
```

If you already have a clean copy from the exact Steam manifest, set `$game` to that folder and proceed with loader setup. On a clean copy, download and verify the pinned loader before extracting it. Run this bootstrap once per fresh game directory.

```powershell
.\scripts\Get-BuildLoader.ps1 -Destination $game
Start-Process -FilePath (Join-Path $game 'RecRoom.exe') -WorkingDirectory $game -ArgumentList '+forcemode:screen'
```

Wait for the loader to generate `BepInEx\interop`, then close the game. A fresh interop directory contains `Assembly-CSharp.dll`, `PhotonRealtime.dll`, `Photon3Unity3D.dll`, and `UnityEngine.CoreModule.dll`. If generation fails, inspect `BepInEx\LogOutput.log` or `BepInEx\LogOutput.txt`, whichever exists. If the interop came from a different game build or loader, move that folder aside and repeat this launch.

From the patch folder, run the metadata check and build. The build repeats the checks so the first command is useful when you want only a signature report.

```powershell
.\build.ps1 -GamePath $game -VerifyOnly
.\build.ps1 -GamePath $game
```

The script validates the expected files, checks both 20230414 Photon auth signatures and the active hook targets using Mono.Cecil, and builds `bin/Release/net6.0/KromaRec.Patch.dll`. A successful build prints its SHA-256. The file checks identify an incomplete game tree; use the Steam manifest to establish the build provenance. Metadata results establish signature matches. Hook binding and dispatch still have to be observed in the game. See [PHOTON_DIAGNOSTICS.md](PHOTON_DIAGNOSTICS.md) for the independent `-ProbeOnly` build.

To invoke the project directly after interop generation:

```powershell
dotnet build .\KromaRec-Patch.csproj -c Release "-p:GamePath=C:\Games\KromaRec-20230414"
```

The generated reference list includes assemblies present on disk. Missing common interop inputs fail preflight. Missing source types still produce compiler errors. Preserve any other working local edits when applying the source diff.

To run the platform-independent and actual BepInEx configuration checks, use the following commands. The configuration test needs only the BepInEx core folder.

```powershell
dotnet run --project tests/ProtocolChecks.csproj -c Release
dotnet run --project tests/BepInExConfig/BepInExConfigChecks.csproj -c Release '-p:LoaderPath=C:\Games\KromaRec-20230414\BepInEx\core' -- config/net.kromarec.patch.cfg
```

BepInEx archive SHA-256 is `616ec7eb06cf11b2a0000e8fcef04d1b12bb58e84a2e0bdac9523234fc193ceb`. Extract the official `BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip`.

The optional encrypted integration probe runs on a Linux test host against the extracted KromaRec 0.10.1 release. It uses the existing release test suite and a test-only Python dependency.

```sh
python3 -m venv .venv
. .venv/bin/activate
python -m pip install cryptography==50.0.0
python tools/encrypted_luxon_probe.py /path/to/extracted/KromaRec
python tools/encrypted_luxon_probe.py /path/to/extracted/KromaRec --glibc
```

## GitHub Actions

The Windows workflow runs the managed tests and builds the standalone probe on pushes and pull requests using .NET SDK 6.0.428. The probe artifact contains only the diagnostic DLL.

The main build runs on a manual request from the default branch with `build_plugin` selected. If you already have a private, authorized source that GitHub-hosted runners may access, configure the `windows-interop` environment with secret `KROMAREC_INTEROP_URL` and variable `KROMAREC_INTEROP_SHA256`. Use a short-lived HTTPS download URL for a hash-pinned ZIP containing `BepInEx/interop/` from the exact depot and loader. Restrict the environment to the default branch. The runner checks the hash and uploads only `KromaRec.Patch.dll` on success. The workflow summary reports when references are unavailable. Keep the full depot and generated references out of commits, Actions artifacts and releases.

Use [NATIVE-TESTING.md](NATIVE-TESTING.md) for the Windows acceptance sequence.

## Failure categories

| Message | Action |
| --- | --- |
| KRBUILD_GAME | Check GamePath and restore the exact depot files. |
| KRBUILD_LOADER | Install the pinned BepInEx core libraries. |
| KRBUILD_INTEROP | Generate genuine 20230414 interop and close the game. |
| KRBUILD_VERIFIER | Resolve the first metadata-tool compiler error. |
| KRBUILD_METADATA | Inspect the saved sanitized report for incompatible or unreadable metadata. |
| KRBUILD_COMPILER | Resolve the first main patch compiler diagnostic. |
| KRBUILD_OUTPUT | Check why the expected output DLL is absent after MSBuild. |
| KRBUILD_SUCCESS | Main plugin compilation completed. Native execution needs separate acceptance. |
| KRBUILD_PROBE_SUCCESS | Only the standalone diagnostic compiled. |

Use -ReportPath with -VerifyOnly to save the copyable report before sharing it. -ReferencesOnly supports a directory containing authorized generated interop and the pinned loader. It skips the executable/native game file preflight for compilation on CI. It still verifies managed signatures.

Run the additional offline checks with the installed loader.

~~~powershell
$core = 'C:\Games\KromaRec-20230414\BepInEx\core'
.\scripts\Test-PowerShellSyntax.ps1
dotnet run --project tests/MetadataChecks/MetadataChecks.csproj -c Release "-p:LoaderPath=$core"
.\tests\BuildReadiness.ps1 -LoaderPath $core
~~~

The synthetic fixtures test the checker and failure classification. Native authentication properties, encryption, Harmony dispatch and two-player gameplay require the genuine Windows installation.
