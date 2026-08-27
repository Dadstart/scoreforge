#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Runs ScoreForge.Api as a single host (Blazor WASM + minimal API) for LAN/internet access.

.DESCRIPTION
  Binds to all interfaces (0.0.0.0) so port forwarding to this machine works.
  Router should map WAN 880 -> this machine:80 and WAN 8443 -> this machine:443.

  On Windows, listening on 80 and 443 usually requires an elevated shell unless you use URL ACL
  or map the router to non-privileged ports instead (change -Urls and router rules accordingly).

  Use ASPNETCORE_ENVIRONMENT=Production so appsettings.Production.json applies (public HTTPS port 8443 for redirects, CORS).

.PARAMETER Urls
  Kestrel bind URLs. Default: http and https on all interfaces on ports 80 and 443.

.PARAMETER Environment
  ASPNETCORE_ENVIRONMENT value. Default: Production.
#>

[CmdletBinding()]
param(
    [string]$Urls = "http://0.0.0.0:80;https://0.0.0.0:443",
    [string]$Environment = "Production",
    [string]$ApiProject = ".\src\ScoreForge.Api\ScoreForge.Api.csproj"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$projectPath = if ([IO.Path]::IsPathRooted($ApiProject)) { $ApiProject } else { Join-Path $repoRoot $ApiProject }

if (-not (Test-Path -LiteralPath $projectPath))
    { throw "Project not found: $projectPath" }

$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:ASPNETCORE_URLS = $Urls

Write-Host "ASPNETCORE_ENVIRONMENT=$Environment"
Write-Host "ASPNETCORE_URLS=$Urls"
Write-Host "Starting API (hosts Blazor client + API)..."
Write-Host "Browse: http://score.dadstart.com:880 and https://score.dadstart.com:8443 (after DNS + port forward + TLS)."
Write-Host ""

Set-Location -LiteralPath $repoRoot
& dotnet run --no-launch-profile --project $projectPath
