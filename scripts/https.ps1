#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [ValidateSet("Setup", "Status")]
    [string]$Action = "Status"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$certDir = Join-Path $repoRoot ".dev-certs"
$viteCert = Join-Path $certDir "vite.pem"
$viteKey = Join-Path $certDir "vite-key.pem"

# Prefer the user-local preview.6 SDK when present (see global.json).
$localDotnet = Join-Path $env:LOCALAPPDATA "dotnet"
if (Test-Path -LiteralPath (Join-Path $localDotnet "dotnet.exe"))
    { $env:PATH = "$localDotnet;$env:PATH" }

function Test-DotnetDevCertTrusted
{
    & dotnet dev-certs https --check --trust 2>&1 | Out-String
    return $LASTEXITCODE -eq 0
}

function Install-DotnetDevCert
{
    Write-Host "Trusting ASP.NET HTTPS development certificate..."
    & dotnet dev-certs https --trust
    if ($LASTEXITCODE -ne 0)
        { throw "Failed to trust the ASP.NET HTTPS development certificate." }
}

function Test-ViteCerts
{
    return (Test-Path -LiteralPath $viteCert) -and (Test-Path -LiteralPath $viteKey)
}

function Test-MkcertCaInstalled
{
    $caRoot = & mkcert -CAROOT 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($caRoot))
        { return $false }

    return (Test-Path -LiteralPath (Join-Path $caRoot.Trim() "rootCA.pem"))
}

function Install-MkcertCa
{
    Write-Host "Installing local CA (mkcert)..."
    $installOutput = & mkcert -install 2>&1
    $installOutput | ForEach-Object { Write-Host $_ }

    if ($LASTEXITCODE -eq 0)
        { return }

    if (Test-MkcertCaInstalled)
    {
        Write-Warning @"
mkcert -install reported errors (often Java keytool access denied on Windows).
The local CA is already available for browsers; continuing certificate generation.
"@
        return
    }

    throw "mkcert -install failed and no local CA was found."
}

function Install-ViteCerts
{
    if (Test-ViteCerts)
    {
        Write-Host "Vite certificates already exist at $certDir"
        return
    }

    $mkcert = Get-Command mkcert -ErrorAction SilentlyContinue
    if (-not $mkcert)
    {
        Write-Host @"
mkcert was not found on PATH.

Install it, then re-run this script:
  winget install FiloSottile.mkcert
  # or: choco install mkcert

Without mkcert, dev.ps1 -Https falls back to Vite's self-signed certificate
(@vitejs/plugin-basic-ssl). Browsers will show a security warning you must accept.
"@
        return
    }

    Install-MkcertCa

    New-Item -ItemType Directory -Force -Path $certDir | Out-Null
    Write-Host "Generating trusted Vite dev certificate for localhost / 127.0.0.1..."
    & mkcert -cert-file $viteCert -key-file $viteKey localhost 127.0.0.1 ::1
    if ($LASTEXITCODE -ne 0)
        { throw "mkcert certificate generation failed." }
}

function Show-Status
{
    $apiOk = Test-DotnetDevCertTrusted
    $viteOk = Test-ViteCerts

    if ($apiOk)
        { Write-Host "API: ASP.NET dev certificate is trusted (https://127.0.0.1:7016)." }
    else
        { Write-Host "API: ASP.NET dev certificate is missing or not trusted. Run: ./scripts/https.ps1 -Action Setup" }

    if ($viteOk)
        { Write-Host "Web: mkcert certificates found in .dev-certs/ (use dev.ps1 -Https)." }
    else
        { Write-Host "Web: no mkcert certificates in .dev-certs/. Run Setup or use dev.ps1 -Https for self-signed fallback." }
}

switch ($Action)
{
    "Setup"
    {
        if (-not (Test-DotnetDevCertTrusted))
            { Install-DotnetDevCert }
        else
            { Write-Host "ASP.NET HTTPS development certificate is already trusted." }

        Install-ViteCerts
        Show-Status
        Write-Host ""
        Write-Host "Start with HTTPS: ./scripts/dev.ps1 -Action Start -Https"
    }
    "Status" { Show-Status }
}
