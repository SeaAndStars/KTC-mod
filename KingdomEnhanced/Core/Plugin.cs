using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

#if IL2CPP
using BepInEx.Unity.IL2CPP;
using KingdomEnhanced.Shared.Attributes;
#endif

#if MONO
using BepInEx.Unity.Mono;
#endif

using KingdomEnhanced.Features;
using KingdomEnhanced.UI;
using KingdomEnhanced.Systems;
using KingdomEnhanced.Hooks;

namespace KingdomEnhanced.Core
{
    /// <summary>
    /// Mod entry point: initializes settings, localization, UI components, and Harmony patches.
    /// </summary>
    [BepInPlugin("kingdomenhanced", "Kingdom Enhanced", ModVersion.FULL)]
    public class Plugin :
#if IL2CPP
        BasePlugin
#else
        BaseUnityPlugin
#endif
    {
        /// <summary>Global plugin instance, assigned during initialization.</summary>
        public static Plugin Instance;

        /// <summary>BepInEx log source for the plugin.</summary>
        public ManualLogSource LogSource
#if IL2CPP
            => Log;
#else
            => Logger;
#endif

#if IL2CPP
        /// <summary>IL2CPP entry point: registers Il2Cpp types and initializes the plugin.</summary>
        public override void Load()
        {
            RegisterTypeInIl2Cpp.RegisterAssembly(Assembly.GetExecutingAssembly());

            Init();
        }
#else
        /// <summary>Mono entry point: initializes the plugin.</summary>
        internal void Awake()
        {
            Init();
        }
#endif

        /// <summary>Initializes configuration, localization, UI components, and Harmony patches.</summary>
        private void Init()
        {
            Instance = this;
            LogSource.LogInfo($"Kingdom Enhanced {ModVersion.DISPLAY} loaded!");
            Settings.Init(Config);
            InitializeLocalization();
            StaminaBarHolder.Initialize();

            var go = new GameObject("KingdomEnhanced_UI");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<ModMenu>();
            go.AddComponent<AccessibilityFeature>();

            var harmony = new Harmony("kingdomenhanced.harmony");
            harmony.PatchAll();
            UnitCachePatches.ApplyAll(harmony);
            LabPatches.ApplyAll(harmony);
            LogSource.LogInfo("Kingdom Enhanced ready. Waiting for player spawn...");
        }

        /// <summary>
        /// Initializes the localization service from the Localization directory next to the plugin DLL.
        /// </summary>
        private void InitializeLocalization()
        {
            string pluginLocation = Assembly.GetExecutingAssembly().Location;
            string pluginDirectory = string.Empty;
            if (!string.IsNullOrWhiteSpace(pluginLocation))
            {
                pluginDirectory = Path.GetDirectoryName(pluginLocation);
            }

            if (string.IsNullOrWhiteSpace(pluginDirectory))
            {
                pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
                LogSource.LogWarning($"Could not resolve assembly location; fell back to: {pluginDirectory}");
            }

            string localizationDirectory = Path.Combine(pluginDirectory, "Localization");
            LocalizationService.Initialize(localizationDirectory, Settings.Language.Value);
        }
    }
}
