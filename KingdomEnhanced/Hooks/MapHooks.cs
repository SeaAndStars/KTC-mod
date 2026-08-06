using HarmonyLib;
using KingdomEnhanced.Systems;
using KingdomEnhanced.UI;
using UnityEngine;

namespace KingdomEnhanced.Hooks
{
    [HarmonyPatch(typeof(UIMainMap), "SelectLand")]
    public static class MapHooks
    {
        /// <summary>上次播报的岛屿序号，用于避免选择逻辑重复触发时反复播报。</summary>
        private static int _lastSpokenIslandIndex = -1;

        [HarmonyPostfix]
        public static void Postfix(int index)
        {
            if (!ModMenu.EnableAccessibility) return;
            if (index == _lastSpokenIslandIndex) return;

            _lastSpokenIslandIndex = index;
            TTSManager.Speak($"Island {index + 1}", interrupt: true);
        }
    }

    [HarmonyPatch(typeof(UIMainMapLand), "SelectButton")]
    public static class MapLandHoverHook
    {
        /// <summary>上次播报的领地按钮名称，用于避免每帧重复触发时反复播报同名对象。</summary>
        private static string _lastSpokenLandName = string.Empty;

        [HarmonyPostfix]
        public static void Postfix(UIMainMapLand __instance)
        {
            if (!ModMenu.EnableAccessibility) return;
            if (__instance == null || __instance.name == null) return;

            if (string.Equals(__instance.name, _lastSpokenLandName, System.StringComparison.Ordinal)) return;

            _lastSpokenLandName = __instance.name;
            TTSManager.Speak(__instance.name, interrupt: true);
        }
    }
}
