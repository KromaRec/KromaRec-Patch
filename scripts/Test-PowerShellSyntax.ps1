[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$files = @(Get-Item (Join-Path $root 'build.ps1')) +
    @(Get-ChildItem -LiteralPath (Join-Path $root "tests") -Filter "*.ps1" -File) +
    @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1' -File)
foreach ($file in $files) {
    $parseErrors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile(
        $file.FullName, [ref]$null, [ref]$parseErrors)
    if ($parseErrors.Count) {
        throw "KRBUILD_SCRIPT_SYNTAX: $($file.Name) contains $($parseErrors.Count) parse errors."
    }
}
Write-Output "PASS: PowerShell syntax for $($files.Count) scripts."
