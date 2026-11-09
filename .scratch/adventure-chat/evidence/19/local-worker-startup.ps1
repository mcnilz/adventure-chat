param([Parameter(Mandatory)][ValidateSet('Disable','Restore')][string]$Phase)
$ErrorActionPreference = 'Stop'
$configPath = 'C:\Users\MartinSchnabel\.codex\config.toml'
$backupPath = 'C:\Users\MartinSchnabel\.codex\.config.ticket19-before-worker.toml'
$statePath = Join-Path $PSScriptRoot 'local-worker-startup-state.json'
$marker = '# ticket19 temporary local worker startup'
$utf8 = [Text.UTF8Encoding]::new($false)

if ($Phase -eq 'Disable') {
    if (Test-Path -LiteralPath $backupPath) { throw 'Existing backup preserved; restore it first.' }
    $originalBytes = [IO.File]::ReadAllBytes($configPath)
    $originalText = [IO.File]::ReadAllText($configPath)
    $section = [regex]::Match($originalText, '(?ms)^\[mcp_servers\.open-design\](?<newline>\r?\n)(?<body>.*?)(?=^\[|\z)')
    if (!$section.Success) { throw 'Expected design connection section not found.' }
    if ([regex]::IsMatch($section.Groups['body'].Value, '(?m)^\s*enabled\s*=')) {
        throw 'Explicit enabled setting preserved; inspect before changing.'
    }
    $newline = $section.Groups['newline'].Value
    $insertion = 'enabled = false ' + $marker + $newline
    $position = $section.Groups['body'].Index
    $modifiedText = $originalText.Insert($position, $insertion)
    [IO.File]::WriteAllBytes($backupPath, $originalBytes)
    [IO.File]::WriteAllText($configPath, $modifiedText, $utf8)
    $state = @{
        originalHash = (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash
        modifiedHash = (Get-FileHash -LiteralPath $configPath -Algorithm SHA256).Hash
        insertedLine = $insertion
        active = $true
    }
    [IO.File]::WriteAllText($statePath, ($state | ConvertTo-Json), $utf8)
    'Only the design MCP entry is temporarily disabled for new worker startup.'
    exit
}

if (!(Test-Path -LiteralPath $backupPath)) { throw 'No startup backup exists; nothing overwritten.' }
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
$currentHash = (Get-FileHash -LiteralPath $configPath -Algorithm SHA256).Hash
if ($currentHash -eq $state.modifiedHash) {
    [IO.File]::WriteAllBytes($configPath, [IO.File]::ReadAllBytes($backupPath))
    if ((Get-FileHash -LiteralPath $configPath -Algorithm SHA256).Hash -ne $state.originalHash) {
        throw 'Restore verification failed; backup retained.'
    }
} else {
    $currentText = [IO.File]::ReadAllText($configPath)
    if (!$currentText.Contains($state.insertedLine)) {
        throw 'Concurrent design entry change detected; backup retained for inspection.'
    }
    [IO.File]::WriteAllText($configPath, $currentText.Replace($state.insertedLine, ''), $utf8)
}
$state.active = $false
$state | Add-Member -NotePropertyName restored -NotePropertyValue $true -Force
[IO.File]::WriteAllText($statePath, ($state | ConvertTo-Json), $utf8)
Remove-Item -LiteralPath $backupPath
'Design MCP entry restored; unrelated configuration preserved.'
