using HarmonyLib;
using KingdomEnhanced.Systems;
using KingdomEnhanced.UI;
using UnityEngine;

namespace KingdomEnhanced.Hooks
{
    /// <summary>
    /// Announces the selected island on the world map when accessibility mode is enabled.
    /// </summary>
    [HarmonyPatch(typeof(UIMainMap), "SelectLand")]
    public static class MapHooks
    {
        /// <summary>Last announced island index, preventing duplicate announcements on repeated triggers.</summary>
        private static int _lastSpokenIslandIndex = -1;

        /// <summary>Speaks the selected island when accessibility mode is enabled.</summary>
        [HarmonyPostfix]
        public static void Postfix(int index)
        {
            if (!ModMenu.EnableAccessibility) return;
            if (index == _lastSpokenIslandIndex) return;

            _lastSpokenIslandIndex = index;
            TTSManager.Speak($"Island {index + 1}", interrupt: true);
        }
    }

    /// <summary>
    /// Announces the hovered land on the world map when accessibility mode is enabled.
    /// </summary>
    [HarmonyPatch(typeof(UIMainMapLand), "SelectButton")]
    public static class MapLandHoverHook
    {
        /// <summary>Last announced land name, preventing repeated announcements for the same object.</summary>
        private static string _lastSpokenLandName = string.Empty;

        /// <summary>Speaks the hovered land name when accessibility mode is enabled.</summary>
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
