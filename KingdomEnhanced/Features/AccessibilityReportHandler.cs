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
        /// 提供 F5-F10 热键触发的无障碍状态播报功能。
    /// </summary>
    public static class AccessibilityReportHandler
    {
        /// <summary>
        /// 可支付对象类型到价格属性的反射缓存，避免重复查找。
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<Type, PropertyInfo> _priceCache = new();

        /// <summary>
        /// 播报玩家朝向、昼夜、威胁状态和最近城墙距离。
        /// </summary>
        /// <param name="player">当前玩家实例。</param>
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
        /// 播报玩家钱包中的金币和宝石数量。
        /// </summary>
        /// <param name="player">当前玩家实例。</param>
        public static void ReportWallet(Player player)
        {
            int coins = player.wallet.GetCurrency(CurrencyType.Coins);
            int gems = player.wallet.GetCurrency(CurrencyType.Gems);
            ModMenu.Speak(LocalizationService.Format("accessibility.report.wallet", coins, gems));
        }

        /// <summary>
        /// 播报当前岛屿天数与昼夜状态。
        /// </summary>
        public static void ReportWorld()
        {
            var d = Managers.Inst.director;
            string time = LocalizationService.Get(d.IsDaytime ? "accessibility.report.time.day" : "accessibility.report.time.night");
            ModMenu.Speak(LocalizationService.Format("accessibility.report.world", d.CurrentIslandDays, time));
        }

        /// <summary>
        /// 播报当前坐骑名称与疲劳状态。
        /// </summary>
        /// <param name="player">当前玩家实例。</param>
        public static void ReportMount(Player player)
        {
            if (player.steed == null) return;
            string n = PayableNameResolver.GetLocalizedDisplayName(player.steed.name);
            string status = LocalizationService.Get(player.steed.IsTired ? "accessibility.report.mount.tired" : "accessibility.report.mount.ready");
            ModMenu.Speak(LocalizationService.Format("accessibility.report.mount", n, status));
        }

        /// <summary>
        /// 播报附近各类追随者数量。
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
        /// 播报当前选中或最近可支付对象的价格与等级信息。
        /// </summary>
        /// <param name="player">当前玩家实例。</param>
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
        /// 将当前选中或最近对象的反射字段写入开发日志，并播报操作结果。
        /// </summary>
        /// <param name="player">当前玩家实例。</param>
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
        /// 查找玩家附近处于激活状态的最近可支付对象。
        /// </summary>
        /// <param name="player">当前玩家实例。</param>
        /// <returns>最近可支付对象；不存在时返回空。</returns>
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
        /// 通过对象类型与反射支付字段识别当前语言的货币名称。
        /// </summary>
        /// <param name="target">待识别的可支付对象。</param>
        /// <returns>当前语言的货币名称。</returns>
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
