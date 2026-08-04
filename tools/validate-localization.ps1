[CmdletBinding()]
param(
    [Parameter()]
    [string]$LocalizationDirectory = (Join-Path $PSScriptRoot '..\KingdomEnhanced\Localization')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Read-LanguageDocument {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )

    if (-not (Test-Path -LiteralPath $FilePath)) {
        throw "Resource file missing: $FilePath"
    }

    $jsonText = Get-Content -LiteralPath $FilePath -Raw -Encoding UTF8
    $document = $jsonText | ConvertFrom-Json -Depth 8

    if ([string]::IsNullOrWhiteSpace($document.language)) {
        throw "language must not be empty: $FilePath"
    }

    if ([string]::IsNullOrWhiteSpace($document.displayName)) {
        throw "displayName must not be empty: $FilePath"
    }

    if ([string]::IsNullOrWhiteSpace($document.fallback)) {
        throw "fallback must not be empty: $FilePath"
    }

    if ($null -eq $document.entries) {
        throw "entries must not be empty: $FilePath"
    }

    $keys = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($entry in $document.entries) {
        if ([string]::IsNullOrWhiteSpace($entry.key)) {
            throw "Empty key found: $FilePath"
        }

        if ([string]::IsNullOrWhiteSpace($entry.value)) {
            throw "Empty value found: $FilePath -> $($entry.key)"
        }

        $normalizedKey = $entry.key.Trim()
        if (-not $keys.Add($normalizedKey)) {
            throw "Duplicate key found: $FilePath -> $normalizedKey"
        }
    }

    return [pscustomobject]@{
        FilePath = $FilePath
        Language = [string]$document.language
        Keys = @($keys)
    }
}

try {
    $resolvedDirectory = (Resolve-Path -LiteralPath $LocalizationDirectory).Path
} catch {
    throw "Localization directory not found: $LocalizationDirectory"
}

$requiredFiles = @(
    (Join-Path $resolvedDirectory 'en-US.json'),
    (Join-Path $resolvedDirectory 'zh-CN.json')
)

$documents = foreach ($file in $requiredFiles) {
    Read-LanguageDocument -FilePath $file
}

$englishKeys = @($documents | Where-Object Language -eq 'en-US' | Select-Object -First 1).Keys
if ($null -eq $englishKeys -or $englishKeys.Count -eq 0) {
    throw 'en-US.json must contain at least one resource key'
}

$englishKeySet = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($key in $englishKeys) {
    [void]$englishKeySet.Add($key)
}

foreach ($document in $documents) {
    $currentSet = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($key in $document.Keys) {
        [void]$currentSet.Add($key)
    }

    if ($currentSet.Count -ne $englishKeySet.Count) {
        throw "Key count mismatch: $($document.Language)"
    }

    foreach ($key in $englishKeySet) {
        if (-not $currentSet.Contains($key)) {
            throw "Missing resource key: $($document.Language) -> $key"
        }
    }
}

Write-Host "Localization validation passed: $resolvedDirectory"
