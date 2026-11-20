[CmdletBinding()]
param([Parameter(Mandatory)][string]$GamePath, [Parameter(Mandatory)][string]$ManifestPath)
$ErrorActionPreference = 'Stop'
# DepotDownloader 3.4.0's manifest-only text export. Only hash fixed local paths.
$required = @('RecRoom.exe','GameAssembly.dll','UnityPlayer.dll','RecRoom_Data/il2cpp_data/Metadata/global-metadata.dat')
$entries = @{}
try {
    $lines = Get-Content -LiteralPath $ManifestPath
    $depot = @($lines -match '^Content Manifest for Depot\s+')
    $manifest = @($lines -match '^Manifest ID / date\s*:')
    if ($depot.Count -ne 1 -or $manifest.Count -ne 1 -or
        $depot[0] -notmatch '^Content Manifest for Depot 471711\s*$' -or
        $manifest[0] -notmatch '^Manifest ID / date\s*:\s*6426603215211043630\s+/') {
        return 'INVALID_RECORD'
    }
    foreach ($line in $lines) {
        if ($line -match '^\s*([0-9]+)\s+[0-9]+\s+([a-f0-9]{40})\s+[a-f0-9]+\s+(.+?)\s*$') {
            $name = $Matches[3].Replace('\','/')
            if ($name -in $required) {
                if ($entries.ContainsKey($name)) { return 'INVALID_RECORD' }
                $entries[$name] = @($Matches[1], $Matches[2])
            }
        }
    }
    if ($entries.Count -ne $required.Count) { return 'INVALID_RECORD' }
    foreach ($name in $required) {
        $file = Get-Item -LiteralPath (Join-Path $GamePath $name)
        if ($file.Length.ToString() -ne $entries[$name][0] -or
            (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA1).Hash -ne $entries[$name][1]) {
            return 'MISMATCH'
        }
    }
    return 'DEPOT_CORE_FILES_MATCH'
} catch {
    return 'UNAVAILABLE'
}
