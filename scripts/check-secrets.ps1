#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [switch]$StagedOnly = $true
)

$ErrorActionPreference = "Stop"

$patterns = @(
    "GOCSPX-[A-Za-z0-9_-]+",
    "AIza[0-9A-Za-z\-_]{35}",
    "ghp_[0-9A-Za-z]{36}",
    "github_pat_[0-9A-Za-z_]{82,}",
    "xox[baprs]-[0-9A-Za-z-]+",
    "AKIA[0-9A-Z]{16}",
    "(?i)clientsecret\s*[:=]\s*[""'][^""']{8,}[""']",
    "(?i)apikey\s*[:=]\s*[""'][^""']{12,}[""']",
    "(?i)password\s*[:=]\s*[""'][^""']{8,}[""']"
)

$excludePattern = "(^|\\|/)(bin|obj|node_modules|\.git|wwwroot/lib)(\\|/)|\.(png|jpg|jpeg|gif|webp|ico|pdf|zip|db|db-shm|db-wal)$"

if ($StagedOnly)
{
    $candidateFiles = git diff --cached --name-only --diff-filter=ACMRTUXB
}
else
{
    $candidateFiles = git ls-files
}

$files = @($candidateFiles | Where-Object { $_ -and ($_ -notmatch $excludePattern) })
if ($files.Count -eq 0)
{
    Write-Host "Secret scan: no candidate files."
    exit 0
}

$violations = @()
foreach ($file in $files)
{
    if (-not (Test-Path -LiteralPath $file))
    {
        continue
    }

    foreach ($pattern in $patterns)
    {
        $matches = Select-String -Path $file -Pattern $pattern -AllMatches
        foreach ($match in $matches)
        {
            $violations += [PSCustomObject]@{
                File    = $file
                Line    = $match.LineNumber
                Pattern = $pattern
                Sample  = $match.Line.Trim()
            }
        }
    }
}

if ($violations.Count -eq 0)
{
    Write-Host "Secret scan: no issues found."
    exit 0
}

Write-Error "Potential secrets detected. Remove or move to user-secrets/env vars before commit."
$violations |
    Sort-Object File, Line |
    ForEach-Object {
        Write-Host "  $($_.File):$($_.Line) [$($_.Pattern)]"
    }

exit 1
