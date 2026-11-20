$ErrorActionPreference = 'Stop'
$dir = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString())
$record = Join-Path $dir 'manifest.txt'
$script = Join-Path (Split-Path -Parent $PSScriptRoot) 'scripts/Test-DepotIdentity.ps1'
try {
    $rows = foreach ($name in @('RecRoom.exe','GameAssembly.dll','UnityPlayer.dll','RecRoom_Data/il2cpp_data/Metadata/global-metadata.dat')) {
        $path = Join-Path $dir $name
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
        [IO.File]::WriteAllText($path, 'fixture')
        "  7  1  $((Get-FileHash -LiteralPath $path -Algorithm SHA1).Hash)  0  $name"
    }
    $header = @('Content Manifest for Depot 471711 ', 'Manifest ID / date     : 6426603215211043630 / fixture')
    function Expect($expected, $lines) {
        $lines | Set-Content -LiteralPath $record
        if ((& $script -GamePath $dir -ManifestPath $record) -ne $expected) { throw "Expected $expected" }
    }
    Expect 'DEPOT_CORE_FILES_MATCH' ($header + $rows)
    Expect 'INVALID_RECORD' ($header + $rows + $rows[0])
    Expect 'INVALID_RECORD' ($header + $rows[0..2])
    Expect 'INVALID_RECORD' (@($header[0], 'Manifest ID / date : 7 / fixture') + $rows)
    [IO.File]::WriteAllText((Join-Path $dir 'GameAssembly.dll'), 'changed')
    Expect 'MISMATCH' ($header + $rows)
    Write-Output 'PASS: depot identity fixtures.'
} finally { if (Test-Path -LiteralPath $dir) { Remove-Item -LiteralPath $dir -Recurse -Force } }
