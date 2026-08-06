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
    /// <summary>Runs periodic world checks (wall repairs, weather, portal rates, siege radar, day/night announcements) and draws the HUD time and wallet display.</summary>
    public class WorldManager : MonoBehaviour
    {
#if IL2CPP
        /// <summary>IL2CPP interop constructor.</summary>
        public WorldManager(IntPtr ptr) : base(ptr) { }
#endif
        /// <summary>HUD label style for the time text.</summary>
        private GUIStyle _timeStyle;
        /// <summary>Style for the wallet coin/gem HUD text.</summary>
        private GUIStyle _coinStyle;

        /// <summary>Accumulated timer for the periodic status check.</summary>
        private float _statusTimer = 0f;
        /// <summary>Accumulated timer for the periodic radar sweep.</summary>
        private float _radarTimer = 0f;
        /// <summary>Time.time timestamp of the last siege alert.</summary>
        private float _lastAttackAlert = 0f;
        
        /// <summary>Seconds between periodic status checks.</summary>
        private const float STATUS_CHECK_INTERVAL = 2.0f;
        /// <summary>Seconds between radar sweeps for the siege alert.</summary>
        private const float RADAR_CHECK_INTERVAL = 4.0f;
        /// <summary>Minimum seconds between consecutive siege alerts.</summary>
        private const float ATTACK_ALERT_COOLDOWN = 60f;

        /// <summary>Tracks the previous day/night state to detect transitions.</summary>
        private bool _wasDay = true;

        /// <summary>Time text cache: the string is rebuilt only when hour/minute/day-night/day count/clock format change, avoiding per-frame allocations</summary>
        private string _cachedTimeText;
        /// <summary>Hour backing the cached time text; -1 = not cached.</summary>
        private int _cachedTimeHour = -1;
        /// <summary>Minute backing the cached time text; -1 = not cached.</summary>
        private int _cachedTimeMinute = -1;
        /// <summary>Day/night state backing the cached time text.</summary>
        private bool _cachedTimeDaytime;
        /// <summary>Day count backing the cached time text.</summary>
        private int _cachedTimeDay;
        /// <summary>12-hour clock setting backing the cached time text.</summary>
        private bool _cachedUse12Hour;

        /// <summary>Wallet text cache: the string is rebuilt only when coin/gem values change, avoiding per-frame allocations</summary>
        private string _cachedWalletText;
        /// <summary>Coin count backing the cached wallet text; -1 = not cached.</summary>
        private int _cachedCoins = -1;
        /// <summary>Gem count backing the cached wallet text; -1 = not cached.</summary>
        private int _cachedGems = -1;

        /// <summary>Wallet reflection field cache: reused after first discovery, avoiding a full per-frame field scan on failure paths</summary>
        private FieldInfo _walletCoinsField;
        /// <summary>Cached wallet gems FieldInfo.</summary>
        private FieldInfo _walletGemsField;
        /// <summary>True once the wallet reflection fields have been discovered.</summary>
        private bool _walletReflectDiscovered = false;

        /// <summary>Reflection-cached enemy list field.</summary>
        private FieldInfo _enemiesListField;
        /// <summary>True once the enemy list reflection field has been discovered.</summary>
        private bool _fieldsDiscovered = false;

        /// <summary>Initializes the HUD and discovers the reflected enemy list field.</summary>
        void Start()
        {
            DiscoverFields();
            Debug.Log("[WorldManager] Started - HUD Display Mode");
        }

        /// <summary>Advances the periodic timers each frame when the managers are valid.</summary>
        void Update()
        {
            if (!IsManagersValid()) return;
            UpdateTimers(Time.deltaTime);
        }

        /// <summary>Renders the HUD every GUI frame when the display is enabled.</summary>
        void OnGUI()
        {
            if (!ModMenu.DisplayTimes || !IsManagersValid()) return;
            InitializeStyles();
            DrawHUD();
        }

        /// <summary>Reflects over EnemyManager to locate and cache the enemy list field once.</summary>
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

        /// <summary>Creates the time and wallet GUIStyles once, with dynamic font size adjustment.</summary>
        private void InitializeStyles()
        {
            if (_timeStyle == null)
            {
                _timeStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = new Color(1f, 0.9f, 0.5f) },
                    alignment = TextAnchor.MiddleCenter
                };
                // Non-dynamic fonts do not support font size/style overrides; setting them would spam log warnings every frame, so skip
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

        /// <summary>Checks whether the style's font supports dynamic properties (setting fontSize/fontStyle on a non-dynamic font triggers engine warnings)</summary>
        private bool IsDynamicFont(GUIStyle style)
        {
            try { return style.font == null || style.font.dynamic; }
            catch { return false; }
        }

        /// <summary>Cached HUD text rendering: formats only when values change, avoiding per-frame string allocations</summary>
        private void DrawHUD()
        {
            try
            {
                /// <summary>Width of the HUD display in pixels.</summary>
                const float hudWidth = 320f;
                float hudX = (Screen.width / 2) - (hudWidth / 2);
                /// <summary>Top Y position of the HUD display in pixels.</summary>
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

        /// <summary>Cached time text: returns the previous result while hour/minute/day-night/day count are unchanged</summary>
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

        /// <summary>Converts accumulated in-game hours to precise 24-hour time; hour and minute are computed from the same source to avoid float rounding drift</summary>
        /// <param name="currentTime">The accumulated in-game hours.</param>
        /// <param name="hour">The hour, 0-23.</param>
        /// <param name="minute">The minute, 0-59.</param>
        private static void GetPreciseTimeOfDay(float currentTime, out int hour, out int minute)
        {
            float totalHours = currentTime % 24f;
            int totalMinutesOfDay = Mathf.FloorToInt(totalHours * 60f + 0.0005f);
            hour = totalMinutesOfDay / 60;
            minute = totalMinutesOfDay % 60;
        }

        /// <summary>Cached wallet text: returns the previous result while coin/gem values are unchanged</summary>
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

        /// <summary>Formats the time display text (using precise 24-hour time)</summary>
        /// <param name="director">The game director instance.</param>
        /// <param name="hour">The precise hour, 0-23.</param>
        /// <param name="minute">The precise minute, 0-59.</param>
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

        /// <summary>Draws the time and wallet labels with a black shadow for readability.</summary>
        private void DrawShadowedLabel(Rect rect, string text, GUIStyle style)
        {
            Color originalColor = GUI.color;
            
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);
            
            GUI.color = originalColor;
            GUI.Label(rect, text, style);
        }

        /// <summary>Reads the player's coin and gem counts, falling back to reflection when needed.</summary>
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

        /// <summary>Reflection fallback for wallet values: fields are discovered once and cached for reuse, avoiding a full per-frame reflection scan</summary>
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

        /// <summary>Accumulates timers and triggers the periodic status, repair, weather, portal, and radar checks.</summary>
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

        /// <summary>Restores all active walls to their initial hit points when invincible walls are enabled.</summary>
        private void RepairWalls()
        {
            var walls = FindObjectsByType<Wall>(FindObjectsSortMode.None);
            foreach (var w in walls) {
                if (w == null || !w.gameObject.activeInHierarchy) continue;
                var d = w.GetComponent<Damageable>();
                if (d != null && d.initialHitPoints > 0) d.hitPoints = d.initialHitPoints;
            }
        }

        /// <summary>Disables all active precipitation objects when clear weather is enabled.</summary>
        private void ClearWeather()
        {
            var pre = FindObjectsByType<Precipitation>(FindObjectsSortMode.None);
            foreach (var p in pre) {
                if (p != null && p.gameObject.activeSelf) p.gameObject.SetActive(false);
            }
        }

        /// <summary>Applies the configured portal spawn rate to all portals unless it is left at default.</summary>
        private void ApplyPortalRates()
        {
            if (ModMenu.PortalSpawnRate == 1.0f) return;
            var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
            foreach (var p in portals) {
                if (p != null) KingdomEnhanced.Hooks.LabPatches.PortalApplyRate(p);
            }
        }

        /// <summary>Runs the periodic game status checks, currently the day/night transition check.</summary>
        private void CheckGameStatus()
        {
            var director = Managers.Inst.director;
            if (director == null) return;
            
            CheckDayNightTransition(director);
        }

        /// <summary>Skips ahead to nightfall and announces it, if it is currently daytime.</summary>
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

        /// <summary>Skips ahead to dawn and announces it, if it is currently nighttime.</summary>
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

        /// <summary>Announces sunrise/nightfall once when the day/night state changes.</summary>
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

        
        /// <summary>Periodically announces a siege when many enemies gather at night, respecting the cooldown.</summary>
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

        /// <summary>Returns the enemy list count via the cached field, or 0 on failure.</summary>
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

        /// <summary>Determines whether a siege alert should fire based on the enemy count and cooldown.</summary>
        private bool ShouldTriggerSiegeAlert(int enemyCount)
        {
            /// <summary>Enemy count above which a siege alert is triggered.</summary>
            const int SIEGE_THRESHOLD = 15;
            return enemyCount > SIEGE_THRESHOLD && 
                   Time.time > _lastAttackAlert + ATTACK_ALERT_COOLDOWN;
        }

        /// <summary>Checks whether the Managers and director are available for use.</summary>
        private bool IsManagersValid()
        {
            try { return Managers.Inst != null && Managers.Inst.director != null; }
            catch { return false; }
        }
    }
}
