# Kingdom Enhanced Mod - Build Script for Windows PowerShell
param(
    [switch]$SkipMono,
    # 可选 BepInEx plugins 根目录；为空时保持仅构建行为。
    [string]$PluginsPath
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Csproj = Join-Path $ScriptDir "KingdomEnhanced\KingdomEnhanced.csproj"

$failed = $false

# 将指定构建输出及完整本地化目录部署到本地 BepInEx plugins 目录。
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
        throw "找不到构建 DLL：$dllSource"
    }
    if (-not (Test-Path $localizationSource)) {
        throw "找不到本地化目录：$localizationSource"
    }

    New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
    New-Item -ItemType Directory -Path $localizationDir -Force | Out-Null
    Copy-Item -Path $dllSource -Destination $pluginDir -Force
    Copy-Item -Path (Join-Path $localizationSource '*') -Destination $localizationDir -Recurse -Force
    Write-Host "已部署 $Configuration 到：$pluginDir" -ForegroundColor Green
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
