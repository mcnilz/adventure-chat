$ErrorActionPreference = 'Stop'
$project = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$review = Join-Path $PSScriptRoot ('review-' + [Guid]::NewGuid().ToString('N'))
$application = Join-Path $review 'application'
New-Item -ItemType Directory -Path $review | Out-Null
Copy-Item -LiteralPath (Join-Path $project 'src/AdventureChat.Server/bin/Debug/net10.0') -Destination $application -Recurse
Copy-Item -LiteralPath (Join-Path $project 'src/AdventureChat.Server/wwwroot') -Destination (Join-Path $application 'wwwroot') -Recurse
$world = Join-Path $review 'world'
Copy-Item -LiteralPath (Join-Path $project 'worlds/reference') -Destination $world -Recurse
# Only this disposable review copy starts directly on the new tour.
$manifest = Join-Path $world 'world.yaml'
$text = [System.IO.File]::ReadAllText($manifest).Replace('startRoom: lounge', 'startRoom: tour-map')
[System.IO.File]::WriteAllText($manifest, $text, [System.Text.UTF8Encoding]::new($false))
$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$url = "http://127.0.0.1:$port"
$quotedDll = '"' + (Join-Path $application 'AdventureChat.Server.dll') + '"'
$quotedWorld = '"' + $world + '"'
$quotedWebRoot = '"' + (Join-Path $application 'wwwroot') + '"'
$arguments = @($quotedDll, '--urls', $url, '--authoring', '--World:Root', $quotedWorld, '--Content:WebRoot', $quotedWebRoot)
$process = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList $arguments -WorkingDirectory $application -WindowStyle Hidden -RedirectStandardOutput (Join-Path $review 'server.stdout.log') -RedirectStandardError (Join-Path $review 'server.stderr.log') -PassThru
$ready = $false
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    try { if ((Invoke-WebRequest -Uri $url -UseBasicParsing).StatusCode -eq 200) { $ready = $true; break } } catch { Start-Sleep -Milliseconds 400 }
}
if (-not $ready) { throw "Review server did not become ready; inspect $review" }
$session = @{ url=$url; pid=$process.Id; application=$application; world=$world; startRoom='tour-map'; sourceWorldUnchanged=$true }
$session | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'review-session.json') -Encoding UTF8
Write-Output ($session | ConvertTo-Json -Compress)
