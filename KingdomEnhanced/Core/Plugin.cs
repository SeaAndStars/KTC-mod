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
    [BepInPlugin("kingdomenhanced", "Kingdom Enhanced", ModVersion.FULL)]
    public class Plugin :
#if IL2CPP
        BasePlugin
#else
        BaseUnityPlugin
#endif
    {
        public static Plugin Instance;

        public ManualLogSource LogSource
#if IL2CPP
            => Log;
#else
            => Logger;
#endif

#if IL2CPP
        public override void Load()
        {
            RegisterTypeInIl2Cpp.RegisterAssembly(Assembly.GetExecutingAssembly());

            Init();
        }
#else
        internal void Awake()
        {
            Init();
        }
#endif

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
        /// 使用插件 DLL 同级的 Localization 目录初始化本地化服务。
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
                LogSource.LogWarning($"程序集位置无法解析目录，已回退到程序集目录：{pluginDirectory}");
            }

            string localizationDirectory = Path.Combine(pluginDirectory, "Localization");
            LocalizationService.Initialize(localizationDirectory, Settings.Language.Value);
        }
    }
}
