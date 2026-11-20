[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$GamePath,
    [switch]$VerifyOnly,
    [switch]$ProbeOnly,
    [switch]$ReferencesOnly,
    [switch]$Install,
    [string]$ManifestPath,
    [string]$ReportPath
)
$ErrorActionPreference = 'Stop'
if ($Install -and ($VerifyOnly -or $ProbeOnly -or $ReferencesOnly)) {
    throw 'KRBUILD_INSTALL: Install requires a full build against the local game.'
}
if ($VerifyOnly -and $ProbeOnly) {
    throw 'Select either -VerifyOnly or -ProbeOnly.'
}
if (!(Test-Path -LiteralPath $GamePath -PathType Container)) {
    throw 'KRBUILD_GAME: GamePath directory is missing. Set -GamePath to the 20230414 installation.'
}
$game = (Resolve-Path -LiteralPath $GamePath).Path
if ($ManifestPath) {
    $identity = & (Join-Path $PSScriptRoot 'scripts/Test-DepotIdentity.ps1') -GamePath $game -ManifestPath $ManifestPath
    if ($identity -ne 'DEPOT_CORE_FILES_MATCH') { throw "KRBUILD_GAME_ID: $identity. Check the installation and the Steam manifest export." }
    Write-Output "GAME_BUILD_ID=$identity"
}
$loader = Join-Path $game 'BepInEx/core'
$interop = Join-Path $game 'BepInEx/interop'

$checker = Join-Path $PSScriptRoot 'tools/VerifyInterop/VerifyInterop.csproj'
$project = Join-Path $PSScriptRoot 'KromaRec-Patch.csproj'
Push-Location -LiteralPath $PSScriptRoot
try {
    if (!(Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue)) {
        throw 'KRBUILD_SDK: Install the Windows x64 .NET SDK 6.0.428 and reopen PowerShell.'
    }
    $sdk = & dotnet --version
    if ($LASTEXITCODE -ne 0 -or $sdk -ne '6.0.428') {
        throw 'KRBUILD_SDK: Install .NET SDK 6.0.428. This repository pins the SDK in global.json.'
    }

    foreach ($file in @('BepInEx.Core.dll', 'BepInEx.Unity.IL2CPP.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $loader $file) -PathType Leaf)) {
            throw "KRBUILD_LOADER: Missing core/$file. Install the pinned IL2CPP loader."
        }
    }
    if ($ProbeOnly) {
        $probe = Join-Path $PSScriptRoot 'tools/PhotonAuthProbe/PhotonAuthProbe.csproj'
        & dotnet build $probe -c Release "-p:LoaderPath=$loader"
        if ($LASTEXITCODE -ne 0) { throw 'KRBUILD_COMPILER: Photon auth diagnostic build failed. Review the first compiler diagnostic.' }
        Write-Output 'KRBUILD_PROBE_SUCCESS: Standalone diagnostic compiled. Native execution is NOT TESTED.'
        return
    }

    if (!$ReferencesOnly) {
        foreach ($file in @('RecRoom.exe', 'GameAssembly.dll', 'UnityPlayer.dll',
                           'RecRoom_Data/il2cpp_data/Metadata/global-metadata.dat')) {
            if (!(Test-Path -LiteralPath (Join-Path $game $file) -PathType Leaf)) {
                throw "KRBUILD_GAME: Missing game file: $file. Select the root of the Windows 20230414 depot."
            }
        }
    }
    foreach ($file in @('Mono.Cecil.dll', 'BepInEx.Core.dll', 'BepInEx.Unity.IL2CPP.dll',
                       'Il2CppInterop.Common.dll', 'Il2CppInterop.Runtime.dll', '0Harmony.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $loader $file) -PathType Leaf)) {
            throw "KRBUILD_LOADER: Missing BepInEx/core/$file. Extract the pinned 6.0.0-pre.2 Windows x64 IL2CPP loader."
        }
    }
    foreach ($file in @('Assembly-CSharp.dll', 'PhotonRealtime.dll', 'Photon3Unity3D.dll', 'UnityEngine.CoreModule.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $interop $file) -PathType Leaf)) {
            throw "KRBUILD_INTEROP: Missing BepInEx/interop/$file. Launch the pinned game with BepInEx, wait for interop generation, then close the game."
        }
    }

    & dotnet build $checker -c Release "-p:LoaderPath=$loader"
    if ($LASTEXITCODE -ne 0) {
        throw 'KRBUILD_VERIFIER: Could not compile the metadata checker. Review the first build diagnostic.'
    }
    $checkerDll = Join-Path $PSScriptRoot 'tools/VerifyInterop/bin/Release/net6.0/VerifyInterop.dll'
    $report = @(& dotnet $checkerDll $game)
    $metadataExit = $LASTEXITCODE
    if ($ReportPath) {
        $report | Set-Content -LiteralPath $ReportPath -Encoding UTF8
    }
    $report | Write-Output
    if ($metadataExit -ne 0) {
        throw 'KRBUILD_METADATA: Inspect each REQUIRED_FAILURE or KRBUILD_METADATA_READ in the report. OPTIONAL entries and UNVERIFIED runtime roles do not fail verification.'
    }
    if ($VerifyOnly) { return }

    & dotnet build $project -c Release "-p:GamePath=$game"
    if ($LASTEXITCODE -ne 0) { throw 'KRBUILD_COMPILER: KromaRec Patch build failed. Keep the first compiler diagnostic.' }
    $dll = Join-Path $PSScriptRoot 'bin/Release/net6.0/KromaRec.Patch.dll'
    if (!(Test-Path -LiteralPath $dll -PathType Leaf)) { throw 'KRBUILD_OUTPUT: Build finished without the expected KromaRec.Patch.dll.' }
    Write-Output "KRBUILD_SUCCESS: Built KromaRec.Patch.dll. SHA-256 $((Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash)"
    if ($Install) {
        $plugins = Join-Path $game 'BepInEx/plugins'
        $configDir = Join-Path $game 'BepInEx/config'
        [void][IO.Directory]::CreateDirectory($plugins)
        [void][IO.Directory]::CreateDirectory($configDir)
        $target = Join-Path $plugins 'KromaRec.Patch.dll'
        if ((Test-Path -LiteralPath $target) -and
            (Get-FileHash -LiteralPath $target).Hash -ne (Get-FileHash -LiteralPath $dll).Hash) {
            throw 'KRBUILD_INSTALL: Close the game and move the previous plugin outside BepInEx/plugins before reinstalling.'
        }
        $config = Join-Path $configDir 'net.kromarec.patch.cfg'
        if (!(Test-Path -LiteralPath $config)) {
            [IO.File]::Copy((Join-Path $PSScriptRoot 'config/net.kromarec.patch.cfg'), $config)
        }
        if (!(Test-Path -LiteralPath $target)) { [IO.File]::Copy($dll, $target) }
        Write-Output 'KRBUILD_INSTALLED: Main DLL installed. Review the installed configuration before launch.'
    }
} finally {
    Pop-Location
}
