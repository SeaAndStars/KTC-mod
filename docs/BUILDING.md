# Building & Development

How to build Kingdom Enhanced for both backends, regenerate IL2CPP interop assemblies and package releases.

---

## Requirements

- .NET SDK 6.0+ (project targets `net6.0` for IL2CPP and `netstandard2.1` for Mono).
- Game interop dependencies are already checked in under `deps/KTC-ModDevLibs/` (BepInEx 6 core assemblies, Assembly-CSharp interop/managed DLLs).
- `interop/` directory: pre-generated IL2CPP interop assemblies (only needed for release packaging).

## Build targets

The project has three configurations:

| Configuration | Target framework | Backend | Output |
|---------------|------------------|---------|--------|
| `Debug` | net6.0 | IL2CPP | `bin/Debug/` |
| `BIE6_IL2CPP` | net6.0 | IL2CPP (BepInEx 6) | `bin/BIE6_IL2CPP/` |
| `BIE6_Mono` | netstandard2.1 | Mono (BepInEx 6) | `bin/BIE6_Mono/` |

```powershell
dotnet build -c BIE6_IL2CPP   # IL2CPP build
dotnet build -c BIE6_Mono     # Mono build
```

Both must compile with 0 warnings / 0 errors before contributing.

The mod DLL embeds `Localization/*.json` as embedded resources (fallback catalogs); the same files are also copied next to the DLL for external override.

## Code conventions

- All classes, methods, properties and fields carry `/// <summary>` XML doc comments (English).
- New features are registered in `KingdomEnhanced/UI/ModMenuFeatures.cs` with stable ids and localization keys.
- All user-facing text goes through `LocalizationService.Get/Format` — no hardcoded UI strings.
- Works on the `dev` branch; open a PR to `main`.

## Regenerating IL2CPP interop assemblies

KTC 2.4.0 uses metadata v31.1 which crashes the official Cpp2IL (issue #471), so interop must be generated in advance:

```powershell
.\tools\generate-interop.ps1
```

The output lands in `interop/` and is embedded into IL2CPP release packages. Run it again when the game updates (new Unity version / new DLL hashes).

## Packaging releases

```powershell
.\build_releases.ps1
```

What it does:

1. Fetches BepInEx `6.0.0-be.785` builds (Mono/IL2CPP × win/linux × x86/x64) — or uses local zips from `BaseZips/` if present.
2. Builds the matching mod target, copies `KingdomEnhanced.dll` and the `Localization/` folder into `BepInEx/plugins/KingdomEnhanced/`.
3. IL2CPP packages: embeds the pre-generated `interop/` assemblies and ships a `BepInEx.cfg` with `UpdateInteropAssemblies = false` (guards against the crashing Cpp2IL on game updates).
4. Windows packages: copies `System.Speech.dll` for TTS support.
5. Outputs `Releases/KTC_Mod_v2.2.0_<Type>_<OS>_<Arch>.zip`.

> BepInEx be.785 ships Il2CppInterop 1.5.3 with the Unity 6 GenericMethod hook fix. Older builds (753/754/755) bundle Il2CppInterop 1.5.0 which crashes on Unity 6 with `AccessViolation`.

## Architecture overview

See [ARCHITECTURE.md](ARCHITECTURE.md) for the module layout and [IL2CPP-MONO-COMPAT.md](IL2CPP-MONO-COMPAT.md) for dual-backend compatibility details.
