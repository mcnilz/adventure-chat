$ErrorActionPreference = 'Stop'
$base = [System.IO.Path]::GetFullPath($PSScriptRoot)
$temporary = Join-Path $base ('restore-check-' + [Guid]::NewGuid().ToString('N'))
$project = [System.IO.Path]::GetFullPath((Join-Path $base '../../../..'))
function Hash($path) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return [Convert]::ToBase64String($sha.ComputeHash([System.IO.File]::ReadAllBytes($path))) }
    finally { $sha.Dispose() }
}
try {
    New-Item -ItemType Directory -Path (Join-Path $temporary 'scripts'), (Join-Path $temporary 'worlds') | Out-Null
    Copy-Item -LiteralPath (Join-Path $project 'scripts/build-reference-art.ps1') -Destination (Join-Path $temporary 'scripts')
    Copy-Item -LiteralPath (Join-Path $project 'worlds/reference') -Destination (Join-Path $temporary 'worlds') -Recurse
    $images = Join-Path $temporary 'worlds/reference/assets/images'
    $before = @{}
    Get-ChildItem -LiteralPath $images -File | ForEach-Object { $before[$_.Name] = Hash $_.FullName }
    & (Join-Path $temporary 'scripts/build-reference-art.ps1')
    foreach ($name in $before.Keys) {
        if ((Hash (Join-Path $images $name)) -ne $before[$name]) { throw "Restoration changed pixels or encoding: $name" }
    }
    Write-Output "Restoration verified: $($before.Count) runtime PNGs reproduced byte-for-byte in isolation."
} finally {
    $resolved = [System.IO.Path]::GetFullPath($temporary)
    if ($resolved.StartsWith($base + [System.IO.Path]::DirectorySeparatorChar) -and ([System.IO.Path]::GetFileName($resolved)).StartsWith('restore-check-')) {
        if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
    } else { throw 'Cleanup target outside the intended evidence directory; preserved.' }
}
