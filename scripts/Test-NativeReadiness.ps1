[CmdletBinding()]
param([Parameter(Mandatory)][string]$GamePath, [string]$ConfigPath, [string]$ManifestPath,
      [ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$KnownGameAssemblySha256)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$GamePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($GamePath)
if ($ManifestPath) { $ManifestPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ManifestPath) }
if ($ConfigPath) { $ConfigPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ConfigPath) }
$core = Join-Path $GamePath 'BepInEx/core'
if (!$ConfigPath) { $ConfigPath = Join-Path $GamePath 'BepInEx/config/net.kromarec.patch.cfg' }
function Get-MissingInputs($Directory, $Files) {
    foreach ($name in $Files) {
        if (!(Test-Path -LiteralPath (Join-Path $Directory $name) -PathType Leaf)) {
            "MISSING $name"
        }
    }
}
Push-Location -LiteralPath $root
try {
    $missing = @(Get-MissingInputs $GamePath @('RecRoom.exe','GameAssembly.dll','UnityPlayer.dll','RecRoom_Data/il2cpp_data/Metadata/global-metadata.dat'))
    $missing
    $game = $missing.Count -eq 0
    'TARGET app=471710 depot=471711 manifest=6426603215211043630'
    if ($game) {
        $hash = (Get-FileHash -LiteralPath (Join-Path $GamePath 'GameAssembly.dll')).Hash
        "GAME_ASSEMBLY_SHA256=$hash"
    }
    $identity = 'UNVERIFIED'
    if ($game -and $KnownGameAssemblySha256) {
        $identity = if ($hash -eq $KnownGameAssemblySha256) { 'REFERENCE_HASH_MATCH' } else { 'MISMATCH' }
    }
    if ($ManifestPath -and $identity -ne 'MISMATCH') {
        $identity = & (Join-Path $PSScriptRoot 'Test-DepotIdentity.ps1') -GamePath $GamePath -ManifestPath $ManifestPath
    }
    "GAME_BUILD_ID=$identity"

    $missing = @(Get-MissingInputs $core @('BepInEx.Core.dll','BepInEx.Unity.IL2CPP.dll','Il2CppInterop.Common.dll','Il2CppInterop.Runtime.dll','0Harmony.dll','Mono.Cecil.dll'))
    $missing
    $loader = $missing.Count -eq 0
    $missing = @(Get-MissingInputs $GamePath @('winhttp.dll','doorstop_config.ini','dotnet/coreclr.dll'))
    $missing
    $bootstrap = $missing.Count -eq 0
    $missing = @(Get-MissingInputs (Join-Path $GamePath 'BepInEx/interop') @('Assembly-CSharp.dll','PhotonRealtime.dll','Photon3Unity3D.dll','UnityEngine.CoreModule.dll'))
    $missing
    $interop = $missing.Count -eq 0
    $sdk = (Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue) -and ((& dotnet --version 2>$null) -eq '6.0.428')
    "GAME_FILES=$game LOADER_CORE=$loader LOADER_BOOTSTRAP=$bootstrap INTEROP_FILES=$interop SDK_6_0_428=$sdk"
    $metadata = 'BLOCKED'
    if ($game -and $loader -and $interop -and $sdk) {
        try {
            & (Join-Path $root 'build.ps1') -GamePath $GamePath -VerifyOnly *> $null
            $metadata = 'PASS'
        } catch { $metadata = 'FAIL' }
    }
    $config = 'BLOCKED'
    if (!(Test-Path -LiteralPath $ConfigPath -PathType Leaf)) { 'MISSING backend config' }
    elseif ($loader -and $sdk) {
        $check = 'tests/BepInExConfig/BepInExConfigChecks.csproj'
        & dotnet build $check -c Release "-p:LoaderPath=$core" *> $null
        if ($LASTEXITCODE -eq 0) {
            & dotnet tests/BepInExConfig/bin/Release/net6.0/BepInExConfigChecks.dll $ConfigPath --validate *> $null
            $config = if ($LASTEXITCODE -eq 0) { 'PASS' } else { 'FAIL' }
        }
    }
    "METADATA=$metadata CONFIG_SYNTAX=$config"
    if ($metadata -eq 'PASS' -and $identity -in @('UNVERIFIED','REFERENCE_HASH_MATCH','DEPOT_CORE_FILES_MATCH')) { 'MAIN_BUILD=READY_TO_ATTEMPT' } else { 'MAIN_BUILD=BLOCKED' }
    'NATIVE_AUTH=BLOCKED'
    $log = @('LogOutput.log','LogOutput.txt') | ForEach-Object {
        Get-Item -LiteralPath (Join-Path $GamePath "BepInEx/$_") -ErrorAction SilentlyContinue
    } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($log) {
        foreach ($line in [IO.File]::ReadLines($log.FullName)) {
            if ($line -cmatch '^\[[^\]]*:\s*KromaRec Patch \(20230414\)\]\s*(.*)$') {
                $message = $Matches[1]
                if ($message -cmatch '^RecNet (discovery|connection-info|oauth|notify-negotiate|notify-hub|rooms|invite|matchmake|startup-match|dorm-match|session-login|session-logout|account|version|cdn-bootstrap): HTTP ([1-5][0-9]{2})\.$') {
                    "HTTP stage=$($Matches[1]) status=$($Matches[2])"
                } elseif ($message -ceq 'Required Harmony hook installation completed. Native authentication remains guarded.') { 'LOG REQUIRED_HOOKS_INSTALLED' }
                elseif ($message -ceq 'Unity reported a scene-load failure.') { 'LOG SCENE_LOAD_FAILURE' }
                elseif ($message -ceq 'KromaRec Photon authentication blocked pending 20230414 auth member verification.') { 'LOG PHOTON_GUARD' }
            }
        }
    } else { 'LOG=UNAVAILABLE' }
} finally { Pop-Location }
