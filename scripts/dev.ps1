#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [ValidateSet("Start", "Stop", "Restart", "Status")]
    [string]$Action = "Status",
    [string]$ApiProject = ".\src\ScoreForge.Api\ScoreForge.Api.csproj",
    [string]$WebDirectory = ".\src\ScoreForge.Web",
    [string]$ApiUrl = "https://127.0.0.1:7016",
    [string]$WebUrl = "http://127.0.0.1:5173",
    [int]$StartupTimeoutSeconds = 90
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

# Prefer the user-local preview.6 SDK when present (see global.json).
$localDotnet = Join-Path $env:LOCALAPPDATA "dotnet"
if (Test-Path -LiteralPath (Join-Path $localDotnet "dotnet.exe"))
    { $env:PATH = "$localDotnet;$env:PATH" }

function Resolve-RepoPath
{
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([IO.Path]::IsPathRooted($Path))
        { return (Resolve-Path -LiteralPath $Path).Path }

    return (Resolve-Path -LiteralPath (Join-Path $repoRoot $Path)).Path
}

function Invoke-RepoBuild
{
    Write-Host "Building solution: dotnet build"
    & dotnet build $repoRoot
    if ($LASTEXITCODE -ne 0)
        { throw "Build failed with exit code $LASTEXITCODE." }
}

function Start-ApiProcess
{
    $resolvedProjectPath = Resolve-RepoPath -Path $ApiProject
    $argumentList = "run --no-launch-profile --project `"$resolvedProjectPath`""
    $environment = @{
        ASPNETCORE_URLS = $ApiUrl
        ASPNETCORE_ENVIRONMENT = "Development"
    }

    $process = Start-Process -FilePath "dotnet" -ArgumentList $argumentList -Environment $environment -PassThru -WindowStyle Normal
    Write-Host "API started (PID $($process.Id)): ASPNETCORE_URLS=$ApiUrl"
}

function Start-WebProcess
{
    $webPath = Resolve-RepoPath -Path $WebDirectory
    $npm = (Get-Command npm.cmd -ErrorAction SilentlyContinue)?.Source
    if (-not $npm)
        { $npm = (Get-Command npm -ErrorAction Stop).Source }

    $argumentList = @("run", "dev", "--", "--host", "127.0.0.1", "--port", "5173")
    $process = Start-Process -FilePath $npm -ArgumentList $argumentList -WorkingDirectory $webPath -PassThru -WindowStyle Normal
    Write-Host "Web started (PID $($process.Id)): $WebUrl"
}

function Wait-ForHttpEndpoint
{
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$DisplayName,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline)
    {
        try
        {
            $null = Invoke-WebRequest -Uri $Url -SkipCertificateCheck -SkipHttpErrorCheck -Method Get -TimeoutSec 3
            Write-Host "$DisplayName is responding at $Url"
            return
        }
        catch
        {
            Start-Sleep -Milliseconds 500
        }
    }

    throw "Timed out waiting for $DisplayName at $Url after $TimeoutSeconds seconds."
}

function Start-Database
{
    $dbScript = Join-Path $PSScriptRoot "db.ps1"
    Write-Host "Ensuring PostgreSQL is running (Podman)..."
    & $dbScript -Action Start
    if ($LASTEXITCODE -ne 0)
        { throw "Failed to start database via scripts/db.ps1 (exit $LASTEXITCODE)." }
}

function Stop-ProjectProcesses
{
    param([switch]$Quiet)

    $api = @(Get-ApiProcesses)
    $web = @(Get-WebProcesses)
    foreach ($process in $api)
        { Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue }
    foreach ($process in $web)
        { Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue }

    if (-not $Quiet)
    {
        if ($api.Count -eq 0 -and $web.Count -eq 0)
            { Write-Host "No running API or Web process found." }
        else
            { Write-Host "Stopped API and Web processes." }
    }

    if ($api.Count -gt 0 -or $web.Count -gt 0)
        { Start-Sleep -Milliseconds 700 }
}

function Start-Projects
{
    # Stop first so rebuilds are not blocked by locked DLLs from a previous API instance.
    Stop-ProjectProcesses -Quiet
    Start-Database
    Invoke-RepoBuild
    Start-ApiProcess
    Start-WebProcess
    Wait-ForHttpEndpoint -Url "$ApiUrl/api/health" -DisplayName "API" -TimeoutSeconds $StartupTimeoutSeconds
    Wait-ForHttpEndpoint -Url $WebUrl -DisplayName "Web" -TimeoutSeconds $StartupTimeoutSeconds
    Write-Host "Opening browser at $WebUrl"
    Start-Process -FilePath $WebUrl
}

function Get-ApiProcesses
{
    $resolvedProjectPath = (Resolve-RepoPath -Path $ApiProject).ToLowerInvariant()
    $escapedProjectPath = [Regex]::Escape($resolvedProjectPath)
    $dotnetProcesses = Get-CimInstance -ClassName Win32_Process -Filter "Name = 'dotnet.exe'"

    return $dotnetProcesses |
        Where-Object {
            $commandLine = $_.CommandLine
            if ([string]::IsNullOrWhiteSpace($commandLine))
                { return $false }

            $normalizedCommandLine = $commandLine.ToLowerInvariant()
            return $normalizedCommandLine -match "(^|\s)run(\s|$)" -and
                $normalizedCommandLine -match "(^|\s)--project(\s|$)" -and
                $normalizedCommandLine -match $escapedProjectPath
        }
}

function Get-WebProcesses
{
    $webPath = (Resolve-RepoPath -Path $WebDirectory).ToLowerInvariant()
    $nodeProcesses = Get-CimInstance -ClassName Win32_Process -Filter "Name = 'node.exe'"
    return $nodeProcesses |
        Where-Object {
            $commandLine = $_.CommandLine
            if ([string]::IsNullOrWhiteSpace($commandLine))
                { return $false }

            return $commandLine.ToLowerInvariant().Contains("vite") -and
                $commandLine.ToLowerInvariant().Contains($webPath)
        }
}

switch ($Action)
{
    "Start" { Start-Projects }
    "Status"
    {
        $api = @(Get-ApiProcesses)
        $web = @(Get-WebProcesses)
        if ($api.Count -eq 0) { Write-Host "API is not running." } else { Write-Host "API PID(s): $(($api | ForEach-Object ProcessId) -join ', ')" }
        if ($web.Count -eq 0) { Write-Host "Web is not running." } else { Write-Host "Web PID(s): $(($web | ForEach-Object ProcessId) -join ', ')" }
    }
    "Stop" { Stop-ProjectProcesses }
    "Restart"
    {
        Stop-ProjectProcesses
        Start-Projects
    }
}
