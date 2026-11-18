[CmdletBinding()]
param([Parameter(Mandatory)][string]$Destination)

$ErrorActionPreference = 'Stop'
$archive = Join-Path ([System.IO.Path]::GetTempPath()) ("kromarec-loader-" + [guid]::NewGuid().ToString("N") + ".zip")
try {
    try { Invoke-WebRequest -Uri 'https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.2/BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip' -OutFile $archive }
    catch { throw 'KRBUILD_LOADER_DOWNLOAD: Could not download BepInEx 6.0.0-pre.2. Check network access and retry.' }
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne '616ec7eb06cf11b2a0000e8fcef04d1b12bb58e84a2e0bdac9523234fc193ceb') {
        throw 'KRBUILD_LOADER_HASH: BepInEx archive hash mismatch.'
    }
    try { Expand-Archive -LiteralPath $archive -DestinationPath $Destination }
    catch { throw 'KRBUILD_LOADER_EXTRACT: Could not extract the verified archive. Check destination access and existing files.' }
} finally {
    if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive }
}
