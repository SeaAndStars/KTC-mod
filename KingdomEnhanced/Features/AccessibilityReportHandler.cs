using System;
using System.Reflection;
using UnityEngine;
using KingdomEnhanced.Core;
using KingdomEnhanced.Systems;
using KingdomEnhanced.UI;
using KingdomEnhanced.Utils;

namespace KingdomEnhanced.Features
{
    /// <summary>
        /// Provides accessibility status announcements triggered by the F5-F10 hotkeys.
    /// </summary>
    public static class AccessibilityReportHandler
    {
        /// <summary>
        /// Reflection cache from payable object type to its Price property, avoiding repeated lookups.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<Type, PropertyInfo> _priceCache = new();

        /// <summary>
        /// Announces the player's facing, day/night state, threat state, and distance to the nearest wall.
        /// </summary>
        /// <param name="player">The current player instance.</param>
        public static void CheckCompassAndSafety(Player player)
        {
            string dir = LocalizationService.Get(player.mover.GetDirection() == Side.Left ? "accessibility.direction.left" : "accessibility.direction.right");

            string threat = LocalizationService.Get("accessibility.report.threat.safe");
            string timeOfDay = LocalizationService.Get("accessibility.report.time.day");
            string borderInfo = "";
            try
            {
                var enemyMgr = UnityEngine.Object.FindFirstObjectByType<EnemyManager>();
                if (enemyMgr != null && enemyMgr.IsDangerous)
                    threat = LocalizationService.Get("accessibility.report.threat.danger");

                var kingdom = UnityEngine.Object.FindFirstObjectByType<Kingdom>();
                if (kingdom != null && !kingdom.isDaytime)
                    timeOfDay = LocalizationService.Get("accessibility.report.time.night");

                var walls = UnityEngine.Object.FindObjectsByType<Wall>(FindObjectsSortMode.None);
                if (walls != null && walls.Length > 0)
                {
                    float min = float.MaxValue;
                    foreach (var w in walls)
                    {
                        float d = Mathf.Abs(w.transform.position.x - player.transform.position.x);
                        if (d < min) min = d;
                    }
                    if (min < float.MaxValue) borderInfo = LocalizationService.Format("accessibility.report.wall_distance", Mathf.RoundToInt(min));
                }
            }
            catch { }

            ModMenu.Speak(LocalizationService.Format("accessibility.report.compass", dir, timeOfDay, threat, borderInfo));
        }

        /// <summary>
        /// Announces the coin and gem counts in the player's wallet.
        /// </summary>
        /// <param name="player">The current player instance.</param>
        public static void ReportWallet(Player player)
        {
            int coins = player.wallet.GetCurrency(CurrencyType.Coins);
            int gems = player.wallet.GetCurrency(CurrencyType.Gems);
            ModMenu.Speak(LocalizationService.Format("accessibility.report.wallet", coins, gems));
        }

        /// <summary>
        /// Announces the current island day count and day/night state.
        /// </summary>
        public static void ReportWorld()
        {
            var d = Managers.Inst.director;
            string time = LocalizationService.Get(d.IsDaytime ? "accessibility.report.time.day" : "accessibility.report.time.night");
            ModMenu.Speak(LocalizationService.Format("accessibility.report.world", d.CurrentIslandDays, time));
        }

        /// <summary>
        /// Announces the current mount's name and tiredness state.
        /// </summary>
        /// <param name="player">The current player instance.</param>
        public static void ReportMount(Player player)
        {
            if (player.steed == null) return;
            string n = PayableNameResolver.GetLocalizedDisplayName(player.steed.name);
            string status = LocalizationService.Get(player.steed.IsTired ? "accessibility.report.mount.tired" : "accessibility.report.mount.ready");
            ModMenu.Speak(LocalizationService.Format("accessibility.report.mount", n, status));
        }

        /// <summary>
        /// Announces the counts of nearby followers of each type.
        /// </summary>
        public static void ReportCompanions()
        {
            int archers = UnityEngine.Object.FindObjectsByType<Archer>(FindObjectsSortMode.None).Length;
            int workers = UnityEngine.Object.FindObjectsByType<Worker>(FindObjectsSortMode.None).Length;
            int peasants = UnityEngine.Object.FindObjectsByType<Peasant>(FindObjectsSortMode.None).Length;
            int knights = UnityEngine.Object.FindObjectsByType<Knight>(FindObjectsSortMode.None).Length;

            ModMenu.Speak(LocalizationService.Format("accessibility.report.companions", archers, workers, peasants, knights));
        }

        /// <summary>
        /// Announces price and level information for the currently selected or nearest payable object.
        /// </summary>
        /// <param name="player">The current player instance.</param>
        public static void ReportDetailedInfo(Player player)
        {
            var current = player.selectedPayable as MonoBehaviour ?? GetClosestPayable(player);
            if (current == null)
            {
                ModMenu.Speak(LocalizationService.Get("accessibility.report.no_object_selected"));
                return;
            }

            string name = PayableNameResolver.GetLocalizedDisplayName(current.name);
            string currency = GetCurrencyName(current);
            int price = 0;

            var currentType = current.GetType();
            if (!_priceCache.TryGetValue(currentType, out var priceProp))
            {
                priceProp = currentType.GetProperty("Price");
                _priceCache[currentType] = priceProp;
            }
            if (priceProp != null) price = (int)priceProp.GetValue(current, null);

            string levelInfo = "";
            var wall = current.GetComponent<Wall>();
            if (wall != null) levelInfo = LocalizationService.Format("accessibility.report.level", wall.level);

            var castle = current.GetComponent<Castle>();
            if (castle != null) levelInfo = LocalizationService.Format("accessibility.report.level", (int)castle.level);

            var tower = current.GetComponent<Tower>();
            if (tower != null)
            {
                name = LocalizationService.Get("payable.name.watchtower");
                levelInfo = LocalizationService.Format("accessibility.report.level", tower.level);
            }

            ModMenu.Speak(LocalizationService.Format("accessibility.report.detailed", name, price, currency, levelInfo));
        }

        /// <summary>
        /// Writes the reflected fields of the selected or nearest payable object to the dev log and announces the result.
        /// </summary>
        /// <param name="player">The current player instance.</param>
        public static void DumpPayableInfo(Player player)
        {
            var current = player.selectedPayable as MonoBehaviour ?? GetClosestPayable(player);

            if (current == null)
            {
                ModMenu.Speak(LocalizationService.Get("accessibility.report.no_object_to_inspect"));
                return;
            }

            ModMenu.Speak(LocalizationService.Format("accessibility.report.inspecting", current.name));
            Debug.Log($"[DEBUG] Inspecting {current.name} ({current.GetType().Name})");

            var fields = current.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                Debug.Log($"   Field: {f.Name} = {f.GetValue(current)}");
            }

            var props = current.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var p in props)
            {
                try
                {
                    Debug.Log($"   Property: {p.Name} = {p.GetValue(current, null)}");
                }
                catch { }
            }

            ModMenu.Speak(LocalizationService.Get("accessibility.report.dumped_fields"));
        }

        /// <summary>
        /// Finds the nearest active payable object near the player.
        /// </summary>
        /// <param name="player">The current player instance.</param>
        /// <returns>The nearest payable object, or null if none exists.</returns>
        private static MonoBehaviour GetClosestPayable(Player player)
        {
            if (Managers.Inst == null || Managers.Inst.payables == null) return null;

            float searchRange = 18.0f;
            float playerX = player.transform.position.x;

            MonoBehaviour closest = null;
            float closestDist = float.MaxValue;

            foreach (var p in Managers.Inst.payables.AllPayables)
            {
                if (p == null) continue;
                var mb = p as MonoBehaviour;
                if (mb == null || !mb.gameObject.activeInHierarchy) continue;

                if (player.steed != null && mb.gameObject == player.steed.gameObject) continue;

                float dist = Mathf.Abs(mb.transform.position.x - playerX);
                if (dist < searchRange && dist < closestDist)
                {
                    closest = mb;
                    closestDist = dist;
                }
            }
            return closest;
        }

        /// <summary>
        /// Identifies the localized currency name from the object type and its reflected payment fields.
        /// </summary>
        /// <param name="target">The payable object to identify.</param>
        /// <returns>The currency name in the current language.</returns>
        private static string GetCurrencyName(MonoBehaviour target)
        {
            if (target.name.Contains("Gem Guard") || target.name.Contains("GemKeeper")) return LocalizationService.Get("accessibility.currency.gems");

            try
            {
                var type = target.GetType();
                var fieldNames = new[] { "currency", "priceType", "coinType", "paymentType" };

                foreach (var fieldName in fieldNames)
                {
                    FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        object value = field.GetValue(target);
                        if (value != null)
                        {
                            string sVal = value.ToString().ToLower();
                            if (sVal.Contains("gem")) return LocalizationService.Get("accessibility.currency.gems");
                            if (sVal.Contains("coin") || sVal.Contains("gold")) return LocalizationService.Get("accessibility.currency.coins");
                        }
                    }

                    PropertyInfo prop = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                    {
                        object value = prop.GetValue(target, null);
                        if (value != null)
                        {
                            string sVal = value.ToString().ToLower();
                            if (sVal.Contains("gem")) return LocalizationService.Get("accessibility.currency.gems");
                            if (sVal.Contains("coin") || sVal.Contains("gold")) return LocalizationService.Get("accessibility.currency.coins");
                        }
                    }
                }
            }
            catch { }

            return LocalizationService.Get("accessibility.currency.coins");
        }
    }
}
