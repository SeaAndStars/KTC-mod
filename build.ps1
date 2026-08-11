# Kingdom Enhanced Mod - Build Script for Windows PowerShell
param(
    [switch]$SkipMono,
    # Optional BepInEx plugins root; when empty, only builds without deploying.
    [string]$PluginsPath
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Csproj = Join-Path $ScriptDir "KingdomEnhanced\KingdomEnhanced.csproj"

$failed = $false

# Deploys a build output and the full localization directory to the local BepInEx plugins directory.
function Copy-BuildOutput {
    param(
        [string]$Configuration
    )

    if ([string]::IsNullOrWhiteSpace($PluginsPath)) {
        return
    }

    $outputDir = Join-Path $ScriptDir "KingdomEnhanced\bin\$Configuration"
    $dllSource = Join-Path $outputDir 'KingdomEnhanced.dll'
    $localizationSource = Join-Path $ScriptDir 'KingdomEnhanced\Localization'
    $pluginDir = Join-Path $PluginsPath 'KingdomEnhanced'
    $localizationDir = Join-Path $pluginDir 'Localization'

    if (-not (Test-Path $dllSource)) {
        throw "Build DLL not found: $dllSource"
    }
    if (-not (Test-Path $localizationSource)) {
        throw "Localization directory not found: $localizationSource"
    }

    New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
    New-Item -ItemType Directory -Path $localizationDir -Force | Out-Null
    Copy-Item -Path $dllSource -Destination $pluginDir -Force
    Copy-Item -Path (Join-Path $localizationSource '*') -Destination $localizationDir -Recurse -Force
    Write-Host "Deployed $Configuration to: $pluginDir" -ForegroundColor Green
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Kingdom Enhanced - Build All Configs" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n[1/2] Building BIE6_IL2CPP..." -ForegroundColor Yellow
dotnet build $Csproj -c BIE6_IL2CPP
if ($LASTEXITCODE -ne 0) {
    Write-Host "BIE6_IL2CPP build FAILED!" -ForegroundColor Red
    $failed = $true
} else {
    Write-Host "BIE6_IL2CPP build succeeded." -ForegroundColor Green
    Copy-BuildOutput -Configuration 'BIE6_IL2CPP'
}

if (-not $SkipMono) {
    Write-Host "`n[2/2] Building BIE6_Mono..." -ForegroundColor Yellow
    dotnet build $Csproj -c BIE6_Mono
    if ($LASTEXITCODE -ne 0) {
        Write-Host "BIE6_Mono build FAILED!" -ForegroundColor Red
        $failed = $true
    } else {
        Write-Host "BIE6_Mono build succeeded." -ForegroundColor Green
        Copy-BuildOutput -Configuration 'BIE6_Mono'
    }
}

if ($failed) {
    Write-Host "`nBuild completed with errors." -ForegroundColor Red
    exit 1
} else {
    Write-Host "`nAll builds succeeded!" -ForegroundColor Green
}
