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
        throw "缺少资源文件: $FilePath"
    }

    $jsonText = Get-Content -LiteralPath $FilePath -Raw -Encoding UTF8
    $document = $jsonText | ConvertFrom-Json -Depth 8

    if ([string]::IsNullOrWhiteSpace($document.language)) {
        throw "language 不能为空: $FilePath"
    }

    if ([string]::IsNullOrWhiteSpace($document.displayName)) {
        throw "displayName 不能为空: $FilePath"
    }

    if ([string]::IsNullOrWhiteSpace($document.fallback)) {
        throw "fallback 不能为空: $FilePath"
    }

    if ($null -eq $document.entries) {
        throw "entries 不能为空: $FilePath"
    }

    $keys = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($entry in $document.entries) {
        if ([string]::IsNullOrWhiteSpace($entry.key)) {
            throw "存在空 key: $FilePath"
        }

        if ([string]::IsNullOrWhiteSpace($entry.value)) {
            throw "存在空 value: $FilePath -> $($entry.key)"
        }

        $normalizedKey = $entry.key.Trim()
        if (-not $keys.Add($normalizedKey)) {
            throw "存在重复 key: $FilePath -> $normalizedKey"
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
    throw "资源目录不存在: $LocalizationDirectory"
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
    throw 'en-US.json 必须包含至少一个资源键'
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
        throw "键数量不一致: $($document.Language)"
    }

    foreach ($key in $englishKeySet) {
        if (-not $currentSet.Contains($key)) {
            throw "缺少资源键: $($document.Language) -> $key"
        }
    }
}

Write-Host "Localization validation passed: $resolvedDirectory"
