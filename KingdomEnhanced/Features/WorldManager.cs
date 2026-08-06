using UnityEngine;
using KingdomEnhanced.Core;
using KingdomEnhanced.UI;
using System.Reflection;
using HarmonyLib;
using System;
using System.Collections.Generic;
#if IL2CPP
using KingdomEnhanced.Shared.Attributes;
#endif

namespace KingdomEnhanced.Features
{
#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    public class WorldManager : MonoBehaviour
    {
#if IL2CPP
        public WorldManager(IntPtr ptr) : base(ptr) { }
#endif
        private GUIStyle _timeStyle;
        private GUIStyle _coinStyle;

        private float _statusTimer = 0f;
        private float _radarTimer = 0f;
        private float _lastAttackAlert = 0f;
        
        private const float STATUS_CHECK_INTERVAL = 2.0f;
        private const float RADAR_CHECK_INTERVAL = 4.0f;
        private const float ATTACK_ALERT_COOLDOWN = 60f;

        private bool _wasDay = true;

        /// <summary>时间文本缓存:仅当小时/分钟/昼夜/天数/时钟制式变化时才重建字符串,避免每帧分配</summary>
        private string _cachedTimeText;
        private int _cachedTimeHour = -1;
        private int _cachedTimeMinute = -1;
        private bool _cachedTimeDaytime;
        private int _cachedTimeDay;
        private bool _cachedUse12Hour;

        /// <summary>钱包文本缓存:仅当金币/宝石数值变化时才重建字符串,避免每帧分配</summary>
        private string _cachedWalletText;
        private int _cachedCoins = -1;
        private int _cachedGems = -1;

        /// <summary>钱包反射字段缓存:首次发现后复用,避免失败路径每帧全字段反射</summary>
        private FieldInfo _walletCoinsField;
        private FieldInfo _walletGemsField;
        private bool _walletReflectDiscovered = false;

        private FieldInfo _enemiesListField;
        private bool _fieldsDiscovered = false;

        void Start()
        {
            DiscoverFields();
            Debug.Log("[WorldManager] Started - HUD Display Mode");
        }

        void Update()
        {
            if (!IsManagersValid()) return;
            UpdateTimers(Time.deltaTime);
        }

        void OnGUI()
        {
            if (!ModMenu.DisplayTimes || !IsManagersValid()) return;
            InitializeStyles();
            DrawHUD();
        }

        private void DiscoverFields()
        {
            if (_fieldsDiscovered) return;

            try
            {
                

                var enemyType = typeof(EnemyManager);
                var enemyFields = enemyType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (var field in enemyFields)
                {
                    string fieldName = field.Name.ToLower();
                    if (fieldName.Contains("enem") || fieldName.Contains("list"))
                    {
                        _enemiesListField = field;
                        Debug.Log($"[WorldManager] Found enemy list field: {field.Name}");
                        break;
                    }
                }

                _fieldsDiscovered = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldManager] Error discovering fields: {ex.Message}");
            }
        }

        private void InitializeStyles()
        {
            if (_timeStyle == null)
            {
                _timeStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = new Color(1f, 0.9f, 0.5f) },
                    alignment = TextAnchor.MiddleCenter
                };
                // 非动态字体不支持字号/样式覆盖,设置会触发每帧日志警告刷屏,故跳过
                if (IsDynamicFont(_timeStyle)) _timeStyle.fontSize = 16;
            }

            if (_coinStyle == null)
            {
                _coinStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = new Color(1f, 0.85f, 0.2f) },
                    alignment = TextAnchor.MiddleCenter
                };
                if (IsDynamicFont(_coinStyle)) _coinStyle.fontSize = 14;
            }
        }

        /// <summary>判断样式字体是否支持动态属性(非动态字体设置 fontSize/fontStyle 会触发引擎警告)</summary>
        private bool IsDynamicFont(GUIStyle style)
        {
            try { return style.font == null || style.font.dynamic; }
            catch { return false; }
        }

        /// <summary>带缓存的 HUD 文本渲染:仅当值变化时才格式化,避免每帧字符串分配</summary>
        private void DrawHUD()
        {
            try
            {
                const float hudWidth = 320f;
                float hudX = (Screen.width / 2) - (hudWidth / 2);
                const float hudY = 20f;

                var director = Managers.Inst?.director;
                if (director == null) return;

                string timeDisplay = GetCachedTimeDisplay(director);
                DrawShadowedLabel(new Rect(hudX, hudY, hudWidth, 25), timeDisplay, _timeStyle);

                var stats = GetPlayerWalletStats();
                if (stats.Coins >= 0)
                {
                    DrawShadowedLabel(
                        new Rect(hudX, hudY + 25, hudWidth, 22),
                        GetCachedWalletText(stats),
                        _coinStyle
                    );
                }
            }
            catch { }
        }

        /// <summary>缓存的时间文本:小时/分钟/昼夜/天数未变化时直接返回上次结果</summary>
        private string GetCachedTimeDisplay(Director director)
        {
            try
            {
                GetPreciseTimeOfDay(director.currentTime, out int hour, out int minute);
                bool isDaytime = director.IsDaytime;
                int day = director.CurrentIslandDays;

                if (_cachedTimeText != null &&
                    _cachedTimeHour == hour &&
                    _cachedTimeMinute == minute &&
                    _cachedTimeDaytime == isDaytime &&
                    _cachedTimeDay == day &&
                    _cachedUse12Hour == ModMenu.Use12HourClock)
                {
                    return _cachedTimeText;
                }

                _cachedTimeHour = hour;
                _cachedTimeMinute = minute;
                _cachedTimeDaytime = isDaytime;
                _cachedTimeDay = day;
                _cachedUse12Hour = ModMenu.Use12HourClock;
                _cachedTimeText = FormatTimeDisplay(director, hour, minute);
                return _cachedTimeText;
            }
            catch { return LocalizationService.Get("hud.error"); }
        }

        /// <summary>将游戏内累计小时换算为精确的 24 小时制时分,小时与分钟同源计算避免浮点进位偏差</summary>
        /// <param name="currentTime">游戏内累计小时数。</param>
        /// <param name="hour">0-23 的小时。</param>
        /// <param name="minute">0-59 的分钟。</param>
        private static void GetPreciseTimeOfDay(float currentTime, out int hour, out int minute)
        {
            float totalHours = currentTime % 24f;
            int totalMinutesOfDay = Mathf.FloorToInt(totalHours * 60f + 0.0005f);
            hour = totalMinutesOfDay / 60;
            minute = totalMinutesOfDay % 60;
        }

        /// <summary>缓存的钱包文本:金币/宝石数值未变化时直接返回上次结果</summary>
        private string GetCachedWalletText((int Coins, int Gems) stats)
        {
            if (_cachedWalletText != null &&
                _cachedCoins == stats.Coins &&
                _cachedGems == stats.Gems)
            {
                return _cachedWalletText;
            }

            _cachedCoins = stats.Coins;
            _cachedGems = stats.Gems;
            _cachedWalletText = LocalizationService.Format("hud.wallet", stats.Coins, stats.Gems);
            return _cachedWalletText;
        }

        /// <summary>格式化时间显示文本(使用精确的 24 小时制时分)</summary>
        /// <param name="director">游戏导演实例。</param>
        /// <param name="hour">精确小时(0-23)。</param>
        /// <param name="minute">精确分钟(0-59)。</param>
        private string FormatTimeDisplay(Director director, int hour, int minute)
        {
            if (director == null) return LocalizationService.Get("hud.error");

            try
            {
                string clock;
                if (ModMenu.Use12HourClock)
                {
                    int hour12 = hour % 12;
                    if (hour12 == 0) hour12 = 12;
                    string suffix = hour < 12
                        ? LocalizationService.Get("hud.time.am")
                        : LocalizationService.Get("hud.time.pm");
                    clock = string.Format("{0}:{1:00} {2}", hour12, minute, suffix);
                }
                else
                {
                    clock = string.Format("{0:00}:{1:00}", hour, minute);
                }

                string timeStr = LocalizationService.Get(director.IsDaytime ? "hud.time.day" : "hud.time.night");
                
                return LocalizationService.Format("hud.time.display", director.CurrentIslandDays, timeStr, clock);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldManager] Error formatting time: {ex.Message}");
                return LocalizationService.Get("hud.time.error");
            }
        }

        private void DrawShadowedLabel(Rect rect, string text, GUIStyle style)
        {
            Color originalColor = GUI.color;
            
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);
            
            GUI.color = originalColor;
            GUI.Label(rect, text, style);
        }

        private (int Coins, int Gems) GetPlayerWalletStats()
        {
            try
            {
                var player = Managers.Inst?.kingdom?.GetPlayer(0);
                if (player == null) return (-1, 0);

                var wallet = player.wallet;
                if (wallet == null) return (-1, 0);

                try
                {
                    int coins = wallet.GetCurrency(CurrencyType.Coins);
                    int gems = wallet.GetCurrency(CurrencyType.Gems);
                    return (coins, gems);
                }
                catch
                {
                    return GetWalletStatsByReflection(wallet);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldManager] Error getting wallet stats: {ex.Message}");
                return (-1, 0);
            }
        }

        /// <summary>反射回退取钱包数值:字段只发现一次后缓存复用,避免每帧全字段反射扫描</summary>
#if IL2CPP
        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
#endif
        private (int Coins, int Gems) GetWalletStatsByReflection(object wallet)
        {
            try
            {
                if (!_walletReflectDiscovered)
                {
                    var walletType = wallet.GetType();
                    var cFields = walletType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var field in cFields)
                    {
                        if (field.FieldType != typeof(int)) continue;
                        string fn = field.Name.ToLower();
                        bool isCoins = fn == "_coins" || fn == "coins" || (fn.Contains("coin") && !fn.Contains("gem"));
                        bool isGems = fn == "_gems" || fn == "gems" || (fn.Contains("gem") && !fn.Contains("coin"));
                        if (_walletCoinsField == null && isCoins) _walletCoinsField = field;
                        if (_walletGemsField == null && isGems) _walletGemsField = field;
                    }
                    _walletReflectDiscovered = true;
                }

                int c = _walletCoinsField != null ? (int)_walletCoinsField.GetValue(wallet) : -1;
                int g = _walletGemsField != null ? (int)_walletGemsField.GetValue(wallet) : 0;
                return (c, g);
            }
            catch
            {
                return (-1, 0);
            }
        }

        private void UpdateTimers(float deltaTime)
        {
            _statusTimer += deltaTime;
            _radarTimer += deltaTime;

            if (_statusTimer >= STATUS_CHECK_INTERVAL)
            {
                _statusTimer = 0f;
                CheckGameStatus();
                if (ModMenu.InvincibleWalls) RepairWalls();
                if (ModMenu.ClearWeather) ClearWeather();
                ApplyPortalRates();
            }

            if (_radarTimer >= RADAR_CHECK_INTERVAL)
            {
                _radarTimer = 0f;
                CheckForGreedAttack();
            }
        }

        private void RepairWalls()
        {
            var walls = FindObjectsByType<Wall>(FindObjectsSortMode.None);
            foreach (var w in walls) {
                if (w == null || !w.gameObject.activeInHierarchy) continue;
                var d = w.GetComponent<Damageable>();
                if (d != null && d.initialHitPoints > 0) d.hitPoints = d.initialHitPoints;
            }
        }

        private void ClearWeather()
        {
            var pre = FindObjectsByType<Precipitation>(FindObjectsSortMode.None);
            foreach (var p in pre) {
                if (p != null && p.gameObject.activeSelf) p.gameObject.SetActive(false);
            }
        }

        private void ApplyPortalRates()
        {
            if (ModMenu.PortalSpawnRate == 1.0f) return;
            var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
            foreach (var p in portals) {
                if (p != null) KingdomEnhanced.Hooks.LabPatches.PortalApplyRate(p);
            }
        }

        private void CheckGameStatus()
        {
            var director = Managers.Inst.director;
            if (director == null) return;
            
            CheckDayNightTransition(director);
        }

        public static void SkipDaytime()
        {
            var director = Managers.Inst?.director;
            if (director == null || !director.IsDaytime) return;

            float currentHour = director.currentTime % 24f;
            float endHour = director.dayEnd;
            float diff = endHour - currentHour;
            if (diff <= 0f) diff += 24f;

            
            director.AdvanceTime(diff + 0.1f);
            ModMenu.Speak(LocalizationService.Get("hud.announcement.skip_to_night"));
        }

        public static void SkipNighttime()
        {
            var director = Managers.Inst?.director;
            if (director == null || director.IsDaytime) return;

            float currentHour = director.currentTime % 24f;
            float startHour = director.dayStart;
            float diff = startHour - currentHour;
            if (diff <= 0f) diff += 24f;

            director.AdvanceTime(diff + 0.1f);
            ModMenu.Speak(LocalizationService.Get("hud.announcement.skip_to_dawn"));
        }

        private void CheckDayNightTransition(Director director)
        {
            try
            {
                if (director.IsDaytime && !_wasDay)
                {
                    ModMenu.Speak(LocalizationService.Get("hud.announcement.sunrise"));
                    _wasDay = true;
                }
                else if (!director.IsDaytime && _wasDay)
                {
                    ModMenu.Speak(LocalizationService.Get("hud.announcement.nightfall"));
                    _wasDay = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WorldManager] Error checking day/night: {ex.Message}");
            }
        }

        
        private void CheckForGreedAttack()
        {
            try
            {
                if (Managers.Inst?.director == null || Managers.Inst.director.IsDaytime) return;

                var enemyManager = UnityEngine.Object.FindFirstObjectByType<EnemyManager>();
                if (enemyManager == null || _enemiesListField == null) return;

                int enemyCount = GetEnemyCount(enemyManager);

                if (ShouldTriggerSiegeAlert(enemyCount))
                {
                    ModMenu.Speak(LocalizationService.Get("hud.announcement.siege"));
                    _lastAttackAlert = Time.time;
                }
            }
            catch { }
        }

        private int GetEnemyCount(EnemyManager enemyManager)
        {
            try
            {
                var enemyList = _enemiesListField.GetValue(enemyManager);
                if (enemyList == null) return 0;

                var countProperty = enemyList.GetType().GetProperty("Count");
                if (countProperty != null)
                {
                    return (int)countProperty.GetValue(enemyList);
                }

                var countField = enemyList.GetType().GetField("Count");
                if (countField != null)
                {
                    return (int)countField.GetValue(enemyList);
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private bool ShouldTriggerSiegeAlert(int enemyCount)
        {
            const int SIEGE_THRESHOLD = 15;
            return enemyCount > SIEGE_THRESHOLD && 
                   Time.time > _lastAttackAlert + ATTACK_ALERT_COOLDOWN;
        }

        private bool IsManagersValid()
        {
            try { return Managers.Inst != null && Managers.Inst.director != null; }
            catch { return false; }
        }
    }
}
