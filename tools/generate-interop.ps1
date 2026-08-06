# =============================================================================
# generate-interop.ps1
# Generates BepInEx IL2CPP interop assemblies for Kingdom Two Crowns 2.4.0
# (Unity 6000.0.61, IL2CPP metadata v31.1).
#
# Background:
#   The game update broke the official Cpp2IL toolchain: every released Cpp2IL
#   version throws NullReferenceException while parsing the compiler-generated
#   type AndroidManager+<_InitiateSignIn>d__21_Server (see
#   SamboyCoding/Cpp2IL issue #471 - still unfixed upstream).
#   This script automates the workaround:
#     1. Download Cpp2IL sources (tag 2022.1.0-pre-release.21)
#     2. Apply community patches (3 edits, see PATCH section below)
#     3. Build patched Cpp2IL
#     4. Generate dummy assemblies (dll_default + attributeinjector, matching
#        BepInEx's built-in pipeline)
#     5. Generate interop assemblies with Il2CppInterop CLI 1.5.3 (requires
#        --game-assembly, otherwise the xref cache stays empty and the game
#        crashes at runtime)
#     6. Compute and write assembly-hash.txt (same algorithm as
#        Il2CppInteropManager.ComputeHash in BepInEx)
#
# Usage:
#   pwsh -File tools/generate-interop.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Kingdom Two Crowns"
#
# Output:
#   <repo-root>/interop/ which is consumed by build_releases.ps1.
# =============================================================================
param(
    # Game installation directory (GameAssembly.dll + global-metadata.dat)
    [string]$GameDir = 'D:\SteamLibrary\steamapps\common\Kingdom Two Crowns',
    # Scratch directory for downloads/builds (safe to delete afterwards)
    [string]$WorkDir = (Join-Path $PSScriptRoot '..\temp_work\interop_gen')
)

$ErrorActionPreference = 'Stop'

# Repo root (parent of tools/)
$RepoRoot = Split-Path $PSScriptRoot -Parent
# Interop output directory consumed by build_releases.ps1
$OutDir = Join-Path $RepoRoot 'interop'

# Toolchain versions - must match the target BepInEx build (be.785)
$Cpp2IlTag  = '2022.1.0-pre-release.21'
$InteropVer = '1.5.3'

$GameAssembly = Join-Path $GameDir 'GameAssembly.dll'
$MetadataPath = Join-Path $GameDir 'KingdomTwoCrowns_Data\il2cpp_data\Metadata\global-metadata.dat'

# .NET SDK is required to build the patched Cpp2IL
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet SDK not found - required to build the patched Cpp2IL.'
}

if (-not (Test-Path $GameAssembly) -or -not (Test-Path $MetadataPath)) {
    throw "Invalid game directory: $GameDir"
}

Write-Host '==> Preparing directories' -ForegroundColor Cyan
if (Test-Path $WorkDir) { Remove-Item $WorkDir -Recurse -Force }
New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null
if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

# -----------------------------------------------------------------------------
# Step 1: download Cpp2IL sources (tag 2022.1.0-pre-release.21)
# -----------------------------------------------------------------------------
Write-Host "==> Downloading Cpp2IL sources ($Cpp2IlTag)" -ForegroundColor Cyan
$srcZip = Join-Path $WorkDir 'cpp2il-src.zip'
Invoke-WebRequest -Uri "https://github.com/SamboyCoding/Cpp2IL/archive/refs/tags/$Cpp2IlTag.zip" `
    -OutFile $srcZip -UseBasicParsing
$srcRoot = Join-Path $WorkDir 'cpp2il-src'
Expand-Archive $srcZip $srcRoot -Force
$srcDir = Join-Path $srcRoot "Cpp2IL-$Cpp2IlTag"

# -----------------------------------------------------------------------------
# Step 2: apply issue #471 patches (3 edits)
#   PATCH 1 - LibCpp2IL/Metadata/Il2CppPropertyDefinition.cs
#             RawPropertyType getter: safe-navigate Setter/Parameters instead
#             of Setter!.Parameters![0] (source of the NullReferenceException).
#   PATCH 2 - Cpp2IL.Core/Utils/AsmResolver/AsmResolverAssemblyPopulator.cs
#             CopyPropertiesInType: skip properties whose definition or raw
#             type is null (already present in this tag - guarded below).
#   PATCH 3 - same file, PopulateCustomAttributes: null-check the
#             "AsmResolverProperty" extra data before copying custom
#             attributes (skipped properties have no managed counterpart).
# -----------------------------------------------------------------------------
Write-Host '==> Applying issue #471 patches' -ForegroundColor Cyan

# PATCH 1: safe-navigate RawPropertyType getter
$propDef = Join-Path $srcDir 'LibCpp2IL\Metadata\Il2CppPropertyDefinition.cs'
$pdContent = Get-Content $propDef -Raw
if ($pdContent -notmatch 'Setter\?\.Parameters') {
    $oldGetter = 'public Il2CppType\? RawPropertyType => LibCpp2IlMain\.TheMetadata == null \? null : Getter == null \? Setter!\.Parameters!\[0\]\.RawType : Getter!\.RawReturnType;'
    $newGetter = @'
public Il2CppType? RawPropertyType => LibCpp2IlMain.TheMetadata == null ? null :
        Getter == null ? (Setter?.Parameters?.FirstOrDefault()?.RawType) :
        Getter.RawReturnType;
'@
    if ((Select-String -Path $propDef -Pattern $oldGetter).Count -eq 0) {
        throw 'PATCH 1 failed: RawPropertyType getter pattern not found in sources.'
    }
    $pdContent = [regex]::Replace($pdContent, $oldGetter, $newGetter)
    Set-Content $propDef -Value $pdContent -NoNewline -Encoding utf8
}

# PATCH 2: skip null-typed properties in CopyPropertiesInType
$populator = Join-Path $srcDir 'Cpp2IL.Core\Utils\AsmResolver\AsmResolverAssemblyPopulator.cs'
$content = Get-Content $populator -Raw
$loopPattern = 'foreach \(var propertyCtx in typeContext\.Properties\)\s*\{'
$loopGuard = @'
foreach (var propertyCtx in typeContext.Properties)
        {
            if (propertyCtx.Definition == null || propertyCtx.Definition.RawPropertyType == null)
            {
                //Skip properties that have no definition or no type information.
                continue;
            }

'@
if ($content -notmatch 'propertyCtx\.Definition == null') {
    if ([regex]::Matches($content, $loopPattern).Count -eq 0) {
        throw 'PATCH 2 failed: property loop pattern not found in sources.'
    }
    $content = [regex]::Replace($content, $loopPattern, $loopGuard)
}

# PATCH 3: null-check AsmResolverProperty in PopulateCustomAttributes
$attrPattern = 'foreach \(var property in type\.Properties\)\s*\r?\n\s*CopyCustomAttributes\(property, property\.GetExtraData<PropertyDefinition>\("AsmResolverProperty"\)!\.CustomAttributes\);'
$attrGuard = @'
foreach (var property in type.Properties)
                {
                    var asmResolverProperty = property.GetExtraData<PropertyDefinition>("AsmResolverProperty");
                    if (asmResolverProperty != null)
                    {
                        CopyCustomAttributes(property, asmResolverProperty.CustomAttributes);
                    }
                }
'@
if ($content -notmatch 'asmResolverProperty != null') {
    if ([regex]::Matches($content, $attrPattern).Count -eq 0) {
        throw 'PATCH 3 failed: custom attribute loop pattern not found in sources.'
    }
    $content = [regex]::Replace($content, $attrPattern, $attrGuard)
    Set-Content $populator -Value $content -NoNewline -Encoding utf8
}

# -----------------------------------------------------------------------------
# Step 3: build the patched Cpp2IL
# -----------------------------------------------------------------------------
Write-Host '==> Building patched Cpp2IL' -ForegroundColor Cyan
dotnet build (Join-Path $srcDir 'Cpp2IL\Cpp2IL.csproj') -c Release --nologo -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Cpp2IL build failed.' }
$cpp2ilDll = Join-Path $srcDir 'Cpp2IL\bin\Release\net9.0\Cpp2IL.dll'
if (-not (Test-Path $cpp2ilDll)) { throw 'Cpp2IL output not found after build.' }

# -----------------------------------------------------------------------------
# Step 4: generate dummy assemblies
#         (dll_default + attributeinjector = BepInEx built-in pipeline)
# -----------------------------------------------------------------------------
Write-Host '==> Generating dummy assemblies' -ForegroundColor Cyan
$dummyDir = Join-Path $WorkDir 'dummy'
dotnet $cpp2ilDll --game-path $GameDir --output-as dll_default `
    --use-processor attributeinjector --output-to $dummyDir | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Cpp2IL dummy assembly generation failed.' }

# -----------------------------------------------------------------------------
# Step 5: generate interop assemblies with Il2CppInterop CLI $InteropVer
#         --game-assembly is mandatory: without it the method-address map and
#         xref cache come out empty and the game crashes with AccessViolation.
# -----------------------------------------------------------------------------
Write-Host "==> Downloading Il2CppInterop CLI $InteropVer" -ForegroundColor Cyan
$cliZip = Join-Path $WorkDir "Il2CppInterop.CLI.$InteropVer.zip"
Invoke-WebRequest -Uri "https://github.com/BepInEx/Il2CppInterop/releases/download/v$InteropVer/Il2CppInterop.CLI.$InteropVer.zip" `
    -OutFile $cliZip -UseBasicParsing
$cliDir = Join-Path $WorkDir 'il2cpp-cli'
Expand-Archive $cliZip $cliDir -Force

Write-Host '==> Generating interop assemblies' -ForegroundColor Cyan
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$cli = Join-Path $cliDir 'net6.0\Il2CppInterop.CLI.dll'
$unityLibs = Join-Path $GameDir 'BepInEx\unity-libs'
if (-not (Test-Path $unityLibs)) { throw "unity-libs directory missing: $unityLibs" }
dotnet $cli generate --input $dummyDir --output $OutDir `
    --unity $unityLibs --game-assembly $GameAssembly | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Il2CppInterop generation failed.' }

# -----------------------------------------------------------------------------
# Step 6: compute assembly-hash.txt
#   Mirrors BepInEx Il2CppInteropManager.ComputeHash:
#   MD5(GameAssembly.dll bytes, each unity-libs dll (name + bytes),
#       InteropAssemblyGenerator assembly version, Cpp2IL.Core assembly version)
#   Versions below must match the target BepInEx be.785 runtime (1.5.3 / 2022.1.0)
# -----------------------------------------------------------------------------
Write-Host '==> Computing assembly-hash' -ForegroundColor Cyan
$md5 = [System.Security.Cryptography.MD5]::Create()
function HashFile($hash, $path) {
    $b = [System.IO.File]::ReadAllBytes($path)
    $null = $hash.TransformBlock($b, 0, $b.Length, $b, 0)
}
function HashString($hash, $s) {
    $b = [System.Text.Encoding]::UTF8.GetBytes($s)
    $null = $hash.TransformBlock($b, 0, $b.Length, $b, 0)
}

HashFile $md5 $GameAssembly
Get-ChildItem $unityLibs -Filter *.dll | ForEach-Object {
    HashString $md5 $_.Name
    HashFile $md5 $_.FullName
}
HashString $md5 '1.5.3.0'
HashString $md5 '2022.1.0.0'
$md5.TransformFinalBlock([byte[]]::new(0), 0, 0)
$hashHex = -join ($md5.Hash | ForEach-Object { $_.ToString('x2') })
Set-Content -Path (Join-Path $OutDir 'assembly-hash.txt') -Value $hashHex -NoNewline -Encoding ascii

Write-Host ''
Write-Host "==> Done. Interop assemblies written to: $OutDir" -ForegroundColor Green
Write-Host "    Files: $((Get-ChildItem $OutDir -Recurse -File | Measure-Object).Count)"
Write-Host "    hash:  $hashHex"
