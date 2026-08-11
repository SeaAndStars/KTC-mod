# IL2CPP / Mono Dual Compatibility Architecture

This document explains how **Kingdom Enhanced** keeps a single codebase compatible with both the **IL2CPP** and **Mono** BepInEx runtimes through three complementary mechanisms: the **build system**, **conditional compilation**, and **abstraction layers**.

---

## 1. Build System (MSBuild)

### 1.1 The Three-Configuration Model

`KingdomEnhanced/KingdomEnhanced.csproj` defines three build configurations and injects compile-time symbols via `DefineConstants`:

```xml
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
    <TargetFramework>net6.0</TargetFramework>
    <DefineConstants>IL2CPP,BIE,BIE6</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='BIE6_IL2CPP|AnyCPU'">
    <TargetFramework>net6.0</TargetFramework>
    <DefineConstants>IL2CPP,BIE,BIE6</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='BIE6_Mono|AnyCPU'">
    <TargetFramework>netstandard2.1</TargetFramework>
    <DefineConstants>MONO,BIE,BIE6</DefineConstants>
</PropertyGroup>
```

| Configuration | Target Framework | Defines | Purpose |
|:---|:---|:---|:---|
| `Debug` | `net6.0` | `IL2CPP, BIE, BIE6` | Development (same DLL references as IL2CPP) |
| `BIE6_IL2CPP` | `net6.0` | `IL2CPP, BIE, BIE6` | Release build for the IL2CPP game version |
| `BIE6_Mono` | `netstandard2.1` | `MONO, BIE, BIE6` | Release build for the Mono game version |

### 1.2 Configuration-Conditional DLL References

```xml
<!-- BIE6_IL2CPP / Debug -->
<ItemGroup Condition="'$(Configuration)'=='BIE6_IL2CPP' or '$(Configuration)'=='Debug'">
    <Reference Include="BepInEx.Unity.IL2CPP">
        <HintPath>..\deps\KTC-ModDevLibs\BIE6_IL2CPP\core\BepInEx.Unity.IL2CPP.dll</HintPath>
        <Private>False</Private>
    </Reference>
    <!-- Il2Cpp interop DLLs -->
    <Reference Include="Assembly-CSharp">
        <HintPath>..\deps\KTC-ModDevLibs\BIE6_IL2CPP\interop\Assembly-CSharp.dll</HintPath>
    </Reference>
    <Reference Include="Il2Cppmscorlib">
        <HintPath>..\deps\KTC-ModDevLibs\BIE6_IL2CPP\interop\Il2Cppmscorlib.dll</HintPath>
    </Reference>
    <!-- ... -->
</ItemGroup>

<!-- BIE6_Mono -->
<ItemGroup Condition="'$(Configuration)'=='BIE6_Mono'">
    <Reference Include="BepInEx.Unity.Mono">
        <HintPath>..\deps\KTC-ModDevLibs\BIE6_Mono\core\BepInEx.Unity.Mono.dll</HintPath>
    </Reference>
    <Reference Include="Assembly-CSharp">
        <HintPath>..\deps\KTC-ModDevLibs\BIE6_Mono\Managed\Assembly-CSharp-publicized.dll</HintPath>
    </Reference>
    <!-- Note: no Il2Cpp-family DLLs (Il2Cppmscorlib, Il2CppInterop, etc.) -->
</ItemGroup>
```

**Key differences:**
- IL2CPP: references `BepInEx.Unity.IL2CPP` plus the full `Il2CppInterop` / `Il2CppSystem` interop set
- Mono: references `BepInEx.Unity.Mono` plus `Assembly-CSharp-publicized.dll` (a publicized build exposing all private members), and **no** Il2Cpp-specific DLLs

All DLLs come from the Git submodule `deps/KTC-ModDevLibs`, organized into `BIE6_IL2CPP/` and `BIE6_Mono/` directories.

---

## 2. Runtime Toolchain (Unity 6)

Kingdom Two Crowns runs on **Unity 6**, which requires a matching BepInEx + Il2CppInterop toolchain:

### 2.1 BepInEx be.785 + Il2CppInterop 1.5.3

- Release packages bundle **BepInEx 6.0.0-be.785** (`6.0.0-be.785+6abdba4`)
- `be.785` ships **Il2CppInterop 1.5.3**, which includes the Unity 6 GenericMethod hook fix required for patching game methods on Unity 6 builds
- **Do not** downgrade `Il2CppInterop.Runtime` — older versions fail to hook generic game methods on Unity 6

### 2.2 Pre-Generated Interop Assemblies

The game update to 2.4.0 (Unity 6000.0.61, IL2CPP metadata v31.1) broke the official Cpp2IL toolchain (Cpp2IL issue #471 — NullReferenceException on the compiler-generated `AndroidManager+<_InitiateSignIn>d__21_Server` type). The repo therefore ships **pre-generated interop assemblies**:

- `tools/generate-interop.ps1` automates the workaround:
  1. Downloads Cpp2IL sources (tag `2022.1.0-pre-release.21`)
  2. Applies community patches (3 edits)
  3. Builds the patched Cpp2IL
  4. Generates dummy assemblies (`dll_default` + attributeinjector, matching BepInEx's built-in pipeline)
  5. Generates interop assemblies with the **Il2CppInterop CLI 1.5.3** (`--game-assembly` is required, otherwise the xref cache stays empty and the game crashes at runtime)
  6. Computes and writes `assembly-hash.txt` (same algorithm as `Il2CppInteropManager.ComputeHash` in BepInEx)
- Output goes to `<repo-root>/interop/`, which is consumed by `build_releases.ps1`
- This is a release-time step for maintainers; regular contributors only need the checked-in interop assemblies

### 2.3 UpdateInteropAssemblies = false

BepInEx would normally regenerate interop assemblies at first launch. For out-of-box releases this is disabled:

- `build_releases.ps1` sets `UpdateInteropAssemblies = false` in the bundled BepInEx configuration, so players get the exact pre-generated assemblies matching the game version
- This prevents both first-launch regeneration delays and interop mismatches

---

## 3. Plugin Entry Point (Base-Class Switching)

The plugin class is the compatibility core. `Core/Plugin.cs` selects its base class and lifecycle methods via conditional compilation:

```csharp
#if IL2CPP
using BepInEx.Unity.IL2CPP;
using KingdomEnhanced.Shared.Attributes;
#endif

#if MONO
using BepInEx.Unity.Mono;
#endif

[BepInPlugin("kingdomenhanced", "Kingdom Enhanced", ModVersion.FULL)]
public class Plugin :
#if IL2CPP
    BasePlugin          // IL2CPP base class
#else
    BaseUnityPlugin     // Mono base class
#endif
{
    public static Plugin Instance;

    // Log adapter
    public ManualLogSource LogSource
#if IL2CPP
        => Log;          // BasePlugin.Log
#else
        => Logger;       // BaseUnityPlugin.Logger
#endif

#if IL2CPP
    public override void Load()
    {
        // IL2CPP must register every MonoBehaviour annotated with [RegisterTypeInIl2Cpp]
        RegisterTypeInIl2Cpp.RegisterAssembly(Assembly.GetExecutingAssembly());
        Init();
    }
#else
    internal void Awake()    // Mono uses the Unity MonoBehaviour lifecycle
    {
        Init();
    }
#endif

    private void Init()
    {
        Instance = this;
        Settings.Init(Config);
        // ... localization init, Harmony patching, UI GameObject creation
    }
}
```

### 3.1 Difference Table

| Aspect | IL2CPP | Mono |
|:---|:---|:---|
| Base class | `BasePlugin` (BepInEx.Unity.IL2CPP) | `BaseUnityPlugin` (BepInEx.Unity.Mono) |
| Entry method | `override void Load()` | `void Awake()` |
| Log property | `Log` | `Logger` |
| Type registration | Required: `ClassInjector.RegisterTypeInIl2Cpp()` | Not needed |

---

## 4. MonoBehaviour (Type Registration & Constructors)

IL2CPP requires every `MonoBehaviour` subclass to be registered at runtime through `ClassInjector`, and constructors must chain to the base `(IntPtr)` constructor.

### 4.1 Standard Pattern

```csharp
#if IL2CPP
using KingdomEnhanced.Shared.Attributes;
#endif

#if IL2CPP
[RegisterTypeInIl2Cpp]      // Custom attribute that triggers type registration
#endif
public class MyHolder : MonoBehaviour
{
    public static MyHolder Instance { get; private set; }

#if IL2CPP
    public MyHolder(IntPtr ptr) : base(ptr) { }   // Constructor required by Il2CppObjectBase
#endif

    public static void Initialize(Plugin plugin)
    {
        Instance = new GameObject("MyHolder").AddComponent<MyHolder>();
        DontDestroyOnLoad(Instance.gameObject);
    }
}
```

### 4.2 The `[RegisterTypeInIl2Cpp]` Attribute

Defined in `Shared/Attributes/RegisterTypeInIl2Cpp.cs`, wrapped in an IL2CPP-only `#if` block (the whole class is absent from Mono builds). Its static `RegisterAssembly()` reflects over the assembly and calls `ClassInjector.RegisterTypeInIl2Cpp()` for every annotated type:

```csharp
#if IL2CPP
using Il2CppInterop.Runtime.Injection;

[AttributeUsage(AttributeTargets.Class)]
public class RegisterTypeInIl2Cpp : Attribute
{
    public static void RegisterAssembly(Assembly asm)
    {
        foreach (var type in asm.GetTypes())
        {
            var attr = type.GetCustomAttribute<RegisterTypeInIl2Cpp>(false);
            if (attr == null) continue;
            ClassInjector.RegisterTypeInIl2Cpp(type);
        }
    }
}
#endif
```

---

## 5. Collection Type Differences

IL2CPP and Mono expose collections from different namespaces but with identical APIs. Where a file needs collections, it switches with a conditional `using`:

```csharp
#if IL2CPP
using Il2CppSystem.Collections.Generic;      // Il2Cpp versions of List<T>, Dictionary<K,V>, HashSet<T>
#else
using System.Collections.Generic;           // Managed versions of List<T>, Dictionary<K,V>, HashSet<T>
#endif
```

Because the API signatures are identical (only the backing type differs), the rest of the file compiles unchanged for both runtimes. Examples in this repo: `Shared/GameExtensions.cs`, `Shared/GameObjectDetails.cs`, `Features/DifficultyUIPatch.cs`.

**Important:** an unconditional `using Il2CppSystem...` makes a file IL2CPP-only — it must then be excluded from (or wrapped in `#if` for) Mono builds, or the Mono build will fail.

---

## 6. NullableAttributes Polyfill (`netstandard2.1`)

The Mono build targets `netstandard2.1`, which lacks the built-in `System.Runtime.CompilerServices.NullableAttribute` and `NullableContextAttribute`. `Shared/NullableAttributes.cs` provides equivalent definitions:

```csharp
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property /* ... */, Inherited = false)]
    internal sealed class NullableAttribute : Attribute { /* ... */ }
    internal sealed class NullableContextAttribute : Attribute { /* ... */ }
}
```

It is compiled directly into the assembly via the `.csproj` — no extra package reference needed.

---

## 7. The `[HideFromIl2Cpp]` Attribute

The IL2CPP interop code generator emits bindings for all public members automatically. Some C# constructs cannot be mapped correctly to Il2Cpp:

- Generic-parameterized delegate types (e.g. `Action<int, int>`)
- Native C# `event` fields (Il2Cpp uses add/remove delegate pairs)
- Complex generic signatures such as `Dictionary<Type, ...>`

**Solution:** annotate such members with `[HideFromIl2Cpp]` (from `Il2CppInterop.Runtime.Attributes`) so the interop generator skips them:

```csharp
using Il2CppInterop.Runtime.Attributes;

[HideFromIl2Cpp]
public event GameStateEventHandler OnGameStateChanged;

[HideFromIl2Cpp]
public void SetResolvers(Dictionary<Type, List<IMarkerResolver>> resolvers) { ... }
```

Missing this annotation causes `System.TypeLoadException` at IL2CPP runtime. Real usages in this repo: `Features/AutoPayHandler.cs` and `Features/WorldManager.cs`.

---

## 8. Type Reflection Differences

### 8.1 Getting the Runtime Type

```csharp
comp.
#if IL2CPP
    GetIl2CppType()     // Il2Cpp runtime type system
#else
    GetType()           // Standard CLR reflection
#endif
    .FullName;
```

### 8.2 Conditional Usings (using-Level Compilation)

```csharp
#if IL2CPP
using Il2CppInterop.Runtime;
using BepInEx.Unity.IL2CPP;
using KingdomEnhanced.Shared.Attributes;
#endif

#if MONO
using BepInEx.Unity.Mono;
#endif
```

---

## 9. Localization Resilience

The localization service (`Core/LocalizationService.cs`) is deliberately free of third-party dependencies:

- JSON parsing uses the project's own fixed-schema parser (no Unity JSON API, no external JSON library), so the same code runs on `net6.0` IL2CPP and `netstandard2.1` Mono
- `Localization/*.json` catalogs (`en-US.json`, `zh-CN.json`) are compiled into the DLL as **embedded resources** (`<EmbeddedResource Include="Localization\*.json" />`), so the mod works with zero extra files
- The external `Localization/` folder next to the DLL is an **optional override**: if it exists and is non-empty, it takes precedence; if it is missing or corrupt, the embedded catalogs are loaded instead
- Missing keys fall back to English, then to the raw resource key — this never blocks plugin loading

---

## 10. Architecture Overview

```
                     ┌──────────────────────────┐
                     │     C# Source Code        │
                     │  (Shared between both)     │
                     │  #if IL2CPP / #if MONO    │
                     └──────────┬───────────────┘
                                │
              ┌─────────────────┴─────────────────┐
              │                                   │
    ┌─────────▼──────────┐              ┌─────────▼──────────┐
    │  BIE6_IL2CPP Build │              │   BIE6_Mono Build  │
    │  net6.0             │              │  netstandard2.1    │
    │  IL2CPP,BIE,BIE6   │              │  MONO,BIE,BIE6     │
    └─────────┬──────────┘              └─────────┬──────────┘
              │                                   │
    ┌─────────▼──────────┐              ┌─────────▼──────────┐
    │ deps/KTC-ModDevLibs│              │ deps/KTC-ModDevLibs│
    │  BIE6_IL2CPP/      │              │  BIE6_Mono/        │
    │  core/ + interop/  │              │  core/ + Managed/  │
    │  Il2CppInterop     │              │  Assembly-CSharp   │
    │  Il2CppSystem      │              │  (publicized)      │
    └────────────────────┘              └────────────────────┘
```

Release packaging additionally bundles **BepInEx 6.0.0-be.785** (with **Il2CppInterop 1.5.3** for Unity 6), the **pre-generated interop assemblies** from `<repo-root>/interop/`, and sets **`UpdateInteropAssemblies = false`**.

### 10.1 Compatibility Abstraction Layer

| Difference | IL2CPP | Mono | Unification |
|:---|:---|:---|:---|
| Plugin base class | `BasePlugin` | `BaseUnityPlugin` | `#if` conditional compilation |
| MonoBehaviour constructor | `MonoBehaviour(IntPtr ptr)` | Default parameterless | `#if IL2CPP` adds the constructor |
| Type registration | `ClassInjector.RegisterTypeInIl2Cpp()` | Not needed | `[RegisterTypeInIl2Cpp]` attribute (IL2CPP only) |
| Collection types | `Il2CppSystem.Collections.Generic.*` | `System.Collections.Generic.*` | Conditional `using` (identical APIs) |
| Nullable attributes | Built into net6.0 | Missing on netstandard2.1 | `Shared/NullableAttributes.cs` polyfill |
| Interop binding hazards | Must `[HideFromIl2Cpp]` events/delegates/complex generics | Not applicable | `[HideFromIl2Cpp]` annotation |
| Log property | `Log` | `Logger` | Conditional property `LogSource` |

### 10.2 Adding a New Feature

1. Create the feature class under `Features/` (or `Systems/` if standalone)
2. Add `[RegisterTypeInIl2Cpp]` and the `(IntPtr)` constructor, wrapped in `#if IL2CPP`
3. Use conditional `using` whenever collections are involved
4. Annotate events, delegates, and complex generic members with `[HideFromIl2Cpp]`
5. Wrap any Il2Cpp-only API usage in `#if IL2CPP`
6. Verify both configurations build: `./build.ps1` (Windows) or `./build.sh` (Linux)
