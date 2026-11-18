[CmdletBinding()]
param([Parameter(Mandatory)][string]$LoaderPath)
$ErrorActionPreference = 'Stop'
$root = Join-Path ([IO.Path]::GetTempPath()) ('kr-build-check-' + [guid]::NewGuid().ToString('N'))
$build = Join-Path (Split-Path -Parent $PSScriptRoot) 'build.ps1'
$pwsh = (Get-Process -Id $PID).Path
function Expect-Failure([string]$Game, [string]$Marker, [switch]$ReferencesOnly) {
    $arguments = @('-NoProfile', '-File', $build, '-GamePath', $Game, '-VerifyOnly', '-ReportPath', (Join-Path $root 'metadata-report.txt'))
    if ($ReferencesOnly) { $arguments += '-ReferencesOnly' }
    $output = @(& $pwsh @arguments 2>&1) -join [Environment]::NewLine
    if ($LASTEXITCODE -eq 0 -or !$output.Contains($Marker)) {
        throw "Expected failure $Marker was not observed."
    }
    Write-Output "PASS: $Marker"
}
try {
    Expect-Failure (Join-Path $root 'absent') 'KRBUILD_GAME'
    $game = Join-Path $root 'game'
    New-Item -ItemType Directory -Path $game -Force | Out-Null
    Expect-Failure $game 'KRBUILD_LOADER'
    $core = Join-Path $game 'BepInEx/core'
    New-Item -ItemType Directory -Path $core -Force | Out-Null
    Get-ChildItem -LiteralPath $LoaderPath -File -Filter '*.dll' | Copy-Item -Destination $core
    Expect-Failure $game 'KRBUILD_GAME'
    Expect-Failure $game 'KRBUILD_INTEROP' -ReferencesOnly
    $interop = Join-Path $game 'BepInEx/interop'
    New-Item -ItemType Directory -Path $interop -Force | Out-Null
    foreach ($file in @('Assembly-CSharp.dll', 'PhotonRealtime.dll', 'Photon3Unity3D.dll', 'UnityEngine.CoreModule.dll')) {
        Set-Content -LiteralPath (Join-Path $interop $file) -Value 'synthetic invalid image'
    }
    Expect-Failure $game 'KRBUILD_METADATA' -ReferencesOnly
    $report = Get-Content -LiteralPath (Join-Path $root 'metadata-report.txt') -Raw
    if (!$report.Contains('KRBUILD_METADATA_READ') -or $report.Contains($root)) {
        throw 'The sanitized failure report is missing or contains a local path.'
    }
    $repo = Split-Path -Parent $PSScriptRoot
    $diagnose = Join-Path $repo 'scripts/Test-NativeReadiness.ps1'
    $config = Join-Path $root 'test.cfg'
    Copy-Item -LiteralPath (Join-Path $repo 'config/net.kromarec.patch.cfg') -Destination $config
    $log = Join-Path $game 'BepInEx/LogOutput.log'
    @('[Info   :KromaRec Patch (20230414)] RecNet discovery: HTTP 200.',
      '[Info   :KromaRec Patch (20230414)] RecNet oauth: HTTP 401. sensitive-fixture',
      '[Info   :Other] RecNet discovery: HTTP 500.') | Set-Content -LiteralPath $log
    $summary = (& $diagnose -GamePath $game -ConfigPath $config) -join "`n"
    foreach ($expected in @('CONFIG_SYNTAX=PASS','HTTP stage=discovery status=200','MAIN_BUILD=BLOCKED','MISSING RecRoom.exe')) {
        if (!$summary.Contains($expected)) { throw "Readiness report omitted $expected" }
    }
    if ($summary -match 'sensitive-fixture|status=401|status=500' -or $summary.Contains($root)) {
        throw 'Readiness report contained non-allowlisted output.'
    }
    (Get-Content -LiteralPath $config -Raw).Replace('https://ns.rec.test','https://sensitive-fixture@ns.rec.test') |
        Set-Content -LiteralPath $config
    $summary = (& $diagnose -GamePath $game -ConfigPath $config) -join "`n"
    if (!$summary.Contains('CONFIG_SYNTAX=FAIL') -or $summary.Contains('sensitive-fixture')) {
        throw 'Invalid backend configuration was accepted or disclosed.'
    }
    Write-Output 'PASS: configuration validation and sanitized readiness report.'
    Write-Output 'PASS: build failure classification. Main plugin compilation remains UNVERIFIED.'
} finally {
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
