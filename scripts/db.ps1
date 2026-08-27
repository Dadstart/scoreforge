#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [ValidateSet("Start", "Stop", "Status")]
    [string]$Action = "Status",
    [string]$ComposeFile = ".\docker-compose.yml",
    [string]$HostAddress = "127.0.0.1",
    [int]$Port = 5432,
    [int]$ReadyTimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

function Resolve-PodmanPath
{
    $fromPath = Get-Command podman -ErrorAction SilentlyContinue
    if ($fromPath)
        { return $fromPath.Source }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Podman\podman.exe"),
        (Join-Path $env:ProgramFiles "RedHat\Podman\podman.exe"),
        (Join-Path $env:ProgramFiles "Podman\podman.exe")
    )

    foreach ($candidate in $candidates)
    {
        if (Test-Path -LiteralPath $candidate)
            { return $candidate }
    }

    throw @"
Podman CLI was not found on PATH.

Install Podman Desktop (or Podman), then either:
  - Enable 'Install CLI / add to PATH' in Podman Desktop settings, or
  - Add the folder containing podman.exe to PATH (often %LOCALAPPDATA%\Programs\Podman)

Then open a new terminal and re-run this script.
"@
}

function Resolve-ComposeFilePath
{
    if ([IO.Path]::IsPathRooted($ComposeFile))
        { return (Resolve-Path -LiteralPath $ComposeFile).Path }

    return (Resolve-Path -LiteralPath (Join-Path $repoRoot $ComposeFile)).Path
}

function Invoke-Podman
{
    param(
        [Parameter(Mandatory = $true)][string]$PodmanPath,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    & $PodmanPath @Arguments
    if ($LASTEXITCODE -ne 0)
        { throw "podman $($Arguments -join ' ') failed with exit code $LASTEXITCODE." }
}

function Ensure-PodmanMachine
{
    param([Parameter(Mandatory = $true)][string]$PodmanPath)

    function Get-MachineList
    {
        $raw = & $PodmanPath machine list --format json 2>$null
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($raw) -or $raw -eq "[]")
            { return @() }

        return @($raw | ConvertFrom-Json)
    }

    function Test-MachineRunning
    {
        param($Machine)

        if ($null -eq $Machine)
            { return $false }

        if ($Machine.PSObject.Properties.Name -contains "Running")
            { return [bool]$Machine.Running }

        if ($Machine.PSObject.Properties.Name -contains "LastUp")
            { return "$($Machine.LastUp)" -match "Currently running" }

        return $false
    }

    $machines = Get-MachineList
    if ($machines.Count -eq 0)
    {
        Write-Host "No Podman machine found. Initializing podman-machine-default..."
        Invoke-Podman -PodmanPath $PodmanPath -Arguments @("machine", "init")
        $machines = Get-MachineList
    }

    $default = $machines | Where-Object { $_.Name -eq "podman-machine-default" -or $_.Default -eq $true } | Select-Object -First 1
    if (-not $default)
        { $default = $machines | Select-Object -First 1 }

    if (-not $default)
        { throw "Unable to resolve a Podman machine after init." }

    if (Test-MachineRunning -Machine $default)
    {
        Write-Host "Podman machine '$($default.Name)' is already running."
        return
    }

    Write-Host "Starting Podman machine '$($default.Name)'..."
    & $PodmanPath machine start $default.Name
    if ($LASTEXITCODE -eq 0)
        { return }

    Write-Warning "Podman machine start failed (exit $LASTEXITCODE). Recreating machine '$($default.Name)'..."
    & $PodmanPath machine rm -f $default.Name | Out-Null
    Invoke-Podman -PodmanPath $PodmanPath -Arguments @("machine", "init", $default.Name)
    Invoke-Podman -PodmanPath $PodmanPath -Arguments @("machine", "start", $default.Name)
}

function Test-PostgresPort
{
    try
    {
        $client = [System.Net.Sockets.TcpClient]::new()
        $async = $client.BeginConnect($HostAddress, $Port, $null, $null)
        $ok = $async.AsyncWaitHandle.WaitOne(1000)
        if ($ok -and $client.Connected)
        {
            $client.EndConnect($async)
            $client.Dispose()
            return $true
        }

        $client.Dispose()
        return $false
    }
    catch
    {
        return $false
    }
}

function Wait-ForPostgres
{
    $deadline = (Get-Date).AddSeconds($ReadyTimeoutSeconds)
    while ((Get-Date) -lt $deadline)
    {
        if (Test-PostgresPort)
        {
            Write-Host "PostgreSQL is accepting connections on ${HostAddress}:${Port}"
            return
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for PostgreSQL on ${HostAddress}:${Port} after $ReadyTimeoutSeconds seconds."
}

function Start-Database
{
    $podman = Resolve-PodmanPath
    $composePath = Resolve-ComposeFilePath
    Write-Host "Using Podman: $podman"
    Ensure-PodmanMachine -PodmanPath $podman
    Write-Host "Starting Postgres via podman compose ($composePath)..."
    Invoke-Podman -PodmanPath $podman -Arguments @("compose", "-f", $composePath, "up", "-d")
    Wait-ForPostgres
}

function Stop-Database
{
    $podman = Resolve-PodmanPath
    $composePath = Resolve-ComposeFilePath
    Write-Host "Stopping Postgres via podman compose..."
    Invoke-Podman -PodmanPath $podman -Arguments @("compose", "-f", $composePath, "down")
}

function Show-DatabaseStatus
{
    $podman = Resolve-PodmanPath
    $composePath = Resolve-ComposeFilePath
    Write-Host "Using Podman: $podman"
    & $podman machine list
    Write-Host ""
    & $podman compose -f $composePath ps
    if (Test-PostgresPort)
        { Write-Host "Port ${HostAddress}:${Port} is open." }
    else
        { Write-Host "Port ${HostAddress}:${Port} is not open." }
}

switch ($Action)
{
    "Start" { Start-Database }
    "Stop" { Stop-Database }
    "Status" { Show-DatabaseStatus }
}
