#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [ValidateSet("Start", "Stop", "Restart", "Status")]
    [string]$Action = "Status",
    [string]$ApiProject = ".\src\ScoreForge.Api\ScoreForge.Api.csproj",
    [string]$ClientProject = ".\src\ScoreForge.Client\ScoreForge.Client.csproj",
    [string]$ApiUrl = "https://localhost:7016",
    [string]$ClientUrl = "https://localhost:7150",
    [int]$StartupTimeoutSeconds = 90
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path

function Resolve-ProjectPath
{
    param([Parameter(Mandatory = $true)][string]$ProjectPath)

    if ([IO.Path]::IsPathRooted($ProjectPath))
        { return (Resolve-Path -LiteralPath $ProjectPath).Path }

    return (Resolve-Path -LiteralPath (Join-Path $repoRoot $ProjectPath)).Path
}

function Invoke-RepoBuild
{
    Write-Host "Building solution: dotnet build"
    & dotnet build $repoRoot
    if ($LASTEXITCODE -ne 0)
        { throw "Build failed with exit code $LASTEXITCODE." }
}

function Start-ProjectProcess
{
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $resolvedProjectPath = Resolve-ProjectPath -ProjectPath $ProjectPath
    $arguments = @("run", "--project", "`"$resolvedProjectPath`"", "--launch-profile", "https")
    $argumentList = $arguments -join " "

    $process = Start-Process -FilePath "dotnet" -ArgumentList $argumentList -PassThru -WindowStyle Normal
    Write-Host "$Name started (PID $($process.Id)): dotnet $argumentList"
}

function Wait-ForHttpsEndpoint
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

function Start-Projects
{
    Invoke-RepoBuild
    Start-ProjectProcess -ProjectPath $ApiProject -Name "API"
    Start-ProjectProcess -ProjectPath $ClientProject -Name "Client"
    Wait-ForHttpsEndpoint -Url $ApiUrl -DisplayName "API" -TimeoutSeconds $StartupTimeoutSeconds
    Wait-ForHttpsEndpoint -Url $ClientUrl -DisplayName "Client" -TimeoutSeconds $StartupTimeoutSeconds

    Write-Host "Opening browser at $ClientUrl"
    Start-Process -FilePath $ClientUrl
}

function Get-ProjectDotnetProcesses
{
    param([Parameter(Mandatory = $true)][string]$ProjectPath)

    $resolvedProjectPath = (Resolve-ProjectPath -ProjectPath $ProjectPath).ToLowerInvariant()
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

function Show-ProjectStatus
{
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$ProjectPath
    )

    $processes = @(Get-ProjectDotnetProcesses -ProjectPath $ProjectPath)
    if ($processes.Count -eq 0)
    {
        Write-Host "$Name is not running."
        return
    }

    $processIds = ($processes | ForEach-Object { $_.ProcessId }) -join ", "
    Write-Host "$Name is running. PID(s): $processIds"
}

function Stop-ProjectProcesses
{
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$ProjectPath
    )

    $processes = @(Get-ProjectDotnetProcesses -ProjectPath $ProjectPath)
    if ($processes.Count -eq 0)
    {
        Write-Host "No running $Name process found."
        return
    }

    $processIds = @($processes | ForEach-Object { [int]$_.ProcessId } | Sort-Object -Unique)
    Write-Host "Stopping $Name process(es): $($processIds -join ', ')"

    foreach ($processId in $processIds)
    {
        try
        {
            Stop-Process -Id $processId -ErrorAction Stop
        }
        catch
        {
            Write-Warning "Graceful stop failed for PID $processId ($Name): $($_.Exception.Message)"
        }
    }

    Start-Sleep -Milliseconds 700

    foreach ($processId in $processIds)
    {
        $remaining = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if (-not $remaining)
            { continue }

        Write-Host "Force stopping $Name process PID $processId"
        Stop-Process -Id $processId -Force -ErrorAction Stop
    }
}

function Restart-Projects
{
    Start-Projects
}

switch ($Action)
{
    "Start"
    {
        Start-Projects
    }
    "Status"
    {
        Show-ProjectStatus -Name "API" -ProjectPath $ApiProject
        Show-ProjectStatus -Name "Client" -ProjectPath $ClientProject
    }
    "Stop"
    {
        Stop-ProjectProcesses -Name "API" -ProjectPath $ApiProject
        Stop-ProjectProcesses -Name "Client" -ProjectPath $ClientProject
    }
    "Restart"
    {
        Stop-ProjectProcesses -Name "API" -ProjectPath $ApiProject
        Stop-ProjectProcesses -Name "Client" -ProjectPath $ClientProject
        Restart-Projects
    }
}
