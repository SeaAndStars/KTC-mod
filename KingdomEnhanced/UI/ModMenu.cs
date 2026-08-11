using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using BepInEx.Configuration;
using KingdomEnhanced.Features;
using KingdomEnhanced.Systems;
using KingdomEnhanced.Core;
using KingdomEnhanced.Shared;
#if IL2CPP
using KingdomEnhanced.Shared.Attributes;
#endif

namespace KingdomEnhanced.UI
{
    /// <summary>
    /// Sidebar tab categories of the mod menu.
    /// </summary>
    public enum TabCategory { Main, Cheats, Lab, Hard, Info, Guide, Settings, Report }

    /// <summary>
    /// Describes metadata for a feature item shown in the ModMenu.
    /// </summary>
    public struct FeatureMeta
    {
        /// <summary>
        /// Stable feature identifier.
        /// </summary>
        public string Id;

        /// <summary>
        /// Final feature title text for legacy callers.
        /// </summary>
        public string Label;

        /// <summary>
        /// Feature title resource key.
        /// </summary>
        public string LabelKey;

        /// <summary>
        /// Final feature section title text for legacy callers.
        /// </summary>
        public string Section;

        /// <summary>
        /// Feature section title resource key.
        /// </summary>
        public string SectionKey;

        /// <summary>
        /// Tab category the feature belongs to.
        /// </summary>
        public TabCategory Category;

        /// <summary>
        /// Final feature description text for legacy callers.
        /// </summary>
        public string Description;

        /// <summary>
        /// Feature description resource key.
        /// </summary>
        public string DescriptionKey;

        /// <summary>
        /// Delegate that reads the current value of a boolean feature.
        /// </summary>
        public Func<bool> GetValue;

        /// <summary>
        /// Delegate that writes the current value of a boolean feature.
        /// </summary>
        public Action<bool> SetValue;

        /// <summary>
        /// Delegate that reads the current value of a slider feature.
        /// </summary>
        public Func<float> GetFloatValue;

        /// <summary>
        /// Delegate that writes the current value of a slider feature.
        /// </summary>
        public Action<float> SetFloatValue;

        /// <summary>
        /// Minimum value allowed for a slider feature.
        /// </summary>
        public float MinVal;

        /// <summary>
        /// Maximum value allowed for a slider feature.
        /// </summary>
        public float MaxVal;

        /// <summary>
        /// Action executed when a button feature is clicked.
        /// </summary>
        public Action OnAction;

        /// <summary>
        /// Delegate that determines whether the feature is locked.
        /// </summary>
        public Func<bool> IsLocked;

        /// <summary>
        /// Delegate returning the final lock reason text for legacy callers.
        /// </summary>
        public Func<string> GetLockReason;

        /// <summary>
        /// Delegate returning the lock reason resource key.
        /// </summary>
        public Func<string> GetLockReasonKey;

        /// <summary>
        /// Delegate that determines whether a conflict warning should be shown.
        /// </summary>
        public Func<bool> HasConflict;

        /// <summary>
        /// Gets the feature title text in the current language.
        /// </summary>
        /// <returns>The resolved feature title text.</returns>
        public string GetLabelText()
        {
            return Label ?? LocalizationService.Get(LabelKey);
        }

        /// <summary>
        /// Gets the feature section title text in the current language.
        /// </summary>
        /// <returns>The resolved section title text.</returns>
        public string GetSectionText()
        {
            return Section ?? LocalizationService.Get(SectionKey);
        }

        /// <summary>
        /// Gets the feature description text in the current language.
        /// </summary>
        /// <returns>The resolved feature description text.</returns>
        public string GetDescriptionText()
        {
            return Description ?? LocalizationService.Get(DescriptionKey);
        }

        /// <summary>
        /// Gets the feature lock reason text in the current language.
        /// </summary>
        /// <returns>The resolved lock reason text; falls back to a generic message when no delegate is set.</returns>
        public string GetLockReasonText()
        {
            if (GetLockReason != null)
            {
                return GetLockReason();
            }

            string reasonKey = GetLockReasonKey != null
                ? GetLockReasonKey()
                : "feature.lock.locked";
            return LocalizationService.Get(reasonKey);
        }
    }

#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    /// <summary>
    /// The in-game mod menu: renders all feature toggles, sliders and buttons
    /// in an IMGUI window and persists settings to the BepInEx config.
    /// </summary>
    public class ModMenu : MonoBehaviour
    {
#if IL2CPP
        public ModMenu(IntPtr ptr) : base(ptr) { }
#endif
        #region PUBLIC SETTINGS
        /// <summary>Toggles the mount stamina bar overlay on the HUD.</summary>
        public static bool ShowStaminaBar;
        /// <summary>Master switch for accessibility narration, radar and proximity alerts.</summary>
        public static bool EnableAccessibility;
        /// <summary>Enables text-to-speech output for narration.</summary>
        public static bool EnableTTS           = true;
        /// <summary>Queues narration messages instead of interrupting the current speech.</summary>
        public static bool NarratorQueueMode   = false;
        /// <summary>Simplifies object names before narration.</summary>
        public static bool SimplifyNames       = true;
        /// <summary>Announces entering and leaving the castle area via TTS.</summary>
        public static bool EnableCastleAnnouncer = false;
        /// <summary>Draws debug boxes for announcer trigger zones.</summary>
        public static bool DebugZones = false;
        /// <summary>Shows the HUD overlay (day/night time and coins).</summary>
        public static bool DisplayTimes;
        /// <summary>Uses 12-hour clock with AM/PM instead of 24-hour format in the HUD.</summary>
        public static bool Use12HourClock = false;
        /// <summary>Unlocks the Cheats tab in the mod menu.</summary>
        public static bool CheatsUnlocked;
        /// <summary>Prevents mount stamina from depleting.</summary>
        public static bool InfiniteStamina;
        /// <summary>Walls self-repair to full hit points.</summary>
        public static bool InvincibleWalls;
        /// <summary>Disables item ability cooldowns.</summary>
        public static bool NoToolCooldowns;
        /// <summary>Arrows fired per Artemis Bow ability cast.</summary>
        public static float ArtemisArrowCount    = 6f;
        /// <summary>Artemis Bow ability range multiplier.</summary>
        public static float ArtemisRangeMult     = 1.0f;
        /// <summary>Artemis Bow arrow damage multiplier.</summary>
        public static float ArtemisArrowDamageMult = 1.0f;
        /// <summary>Boosts builder movement and work speed for near-instant construction.</summary>
        public static bool HyperBuilders;
        /// <summary>Expands vagrant camps (more beggars, faster spawn interval).</summary>
        public static bool LargerCamps;
        /// <summary>Accelerates citizen house construction.</summary>
        public static bool BetterCitizenHouses;
        /// <summary>Grants elite knight buffs (50 hit points).</summary>
        public static bool BetterKnight;
        /// <summary>Locks the season to summer.</summary>
        public static bool LockSummer;
        /// <summary>Forces clear weather by disabling precipitation.</summary>
        public static bool ClearWeather;
        /// <summary>Disables blood moon events.</summary>
        public static bool NoBloodMoons;
        /// <summary>Keeps coins from sinking in water.</summary>
        public static bool CoinsStayDry;
        /// <summary>Enables scaling of the monarch sprite.</summary>
        public static bool EnableSizeHack = false;
        /// <summary>Player scale multiplier, active when size hack is enabled.</summary>
        public static float  TargetSize      = 1.0f;
        /// <summary>Mounted travel speed multiplier.</summary>
        public static float  SpeedMultiplier = 1.0f;
        /// <summary>Shows the greed counter overlay on the HUD.</summary>
        public static bool   ShowGreedCounter = false;

        /// <summary>Coin income multiplier.</summary>
        public static float CoinIncomeMult  = 1.0f;
        /// <summary>Bag drop multiplier.</summary>
        public static float BagDropMult     = 1.0f;

        /// <summary>Doubles the archer fire rate.</summary>
        public static bool  ArcherFireBoost  = false;
        /// <summary>Enables berserker rage mode with earlier activation.</summary>
        public static bool  BerserkerRage    = false;
        /// <summary>Doubles the ninja movement speed.</summary>
        public static bool  NinjaSpeedBoost  = false;
        /// <summary>Recruit cap override (0 = game default).</summary>
        public static int   RecruitCap       = 0;
        /// <summary>Tree regrowth speed multiplier.</summary>
        public static float TreeRegrowthMult = 1.0f;
        /// <summary>Boosts animal spawning.</summary>
        public static bool  AnimalSpawnBoost = false;
        /// <summary>Skips the daytime/nighttime stopper for instant day transitions.</summary>
        public static bool  InstantDaySkip   = false;
        /// <summary>Doubles farm coin output.</summary>
        public static bool  FarmOutputBoost  = false;
        /// <summary>Doubles the tower archer fire rate.</summary>
        public static bool  TowerFireBoost   = false;
        /// <summary>Doubles ballista bolt damage.</summary>
        public static bool  BallistaBoost    = false;
        /// <summary>Ballista reload speed multiplier.</summary>
        public static float BallistaReloadMult = 1.0f;
        /// <summary>Ballista bolt flight speed multiplier.</summary>
        public static float BallistaFlightMult = 1.0f;
        /// <summary>Boosts catapult reload speed.</summary>
        public static bool  CatapultBoost    = false;
        /// <summary>Catapult reload speed multiplier.</summary>
        public static float CatapultReloadMult = 1.0f;
        /// <summary>Catapult projectile flight speed multiplier.</summary>
        public static float CatapultFlightMult = 1.0f;
        /// <summary>Completes castle upgrades instantly.</summary>
        public static bool  InstantCastle    = false;

        /// <summary>Builder movement speed multiplier.</summary>
        public static float BuilderSpeedMult = 1.0f;
        /// <summary>Builder work efficiency multiplier (lower is faster).</summary>
        public static float BuilderEfficiencyMult = 1.0f;

        /// <summary>Steed run speed scale.</summary>
        public static float SteedSpeedMult   = 1.0f;
        /// <summary>Doubles steed charge damage.</summary>
        public static bool  ChargeDmgBoost   = false;
        /// <summary>Steed buff aura duration multiplier.</summary>
        public static float BuffAuraDuration = 1.0f;

        /// <summary>Enemy wave size multiplier.</summary>
        public static float WaveSizeMult       = 1.0f;
        /// <summary>Enemy movement speed multiplier.</summary>
        public static float EnemySpeedMult     = 1.0f;
        /// <summary>Portal spawn rate multiplier.</summary>
        public static float PortalSpawnRate    = 1.0f;
        /// <summary>Disables crown stealing by enemies.</summary>
        public static bool  NoCrownStealing    = false;
        /// <summary>Greed Queen hit point scale.</summary>
        public static float GreedQueenHPScale  = 1.0f;
        /// <summary>Director threat ramp multiplier.</summary>
        public static float DirectorThreatMult = 1.0f;

        /// <summary>Mod menu window scale.</summary>
        public static float WindowScale   = 1.0f;
        /// <summary>Mod menu background opacity.</summary>
        public static float MenuOpacity   = 0.98f;
        /// <summary>Kingdom monitor panel opacity.</summary>
        public static float MonitorOpacity = 0.95f;

        /// <summary>Last accessibility narration message.</summary>
        public static string LastAccessMessage = "";
        /// <summary>Remaining display time of the last access message.</summary>
        public static float  MessageTimer      = 0f;
        /// <summary>Number of units spawned per spawn action.</summary>
        public static float  SpawnUnitCount    = 1f;

        #endregion

        #region COLORS
        /// <summary>Theme color: window background.</summary>
        private static readonly Color C_BG              = new Color(0.06f, 0.05f, 0.03f);
        /// <summary>Theme color: panel background.</summary>
        public static readonly Color C_PANEL            = new Color(0.10f, 0.08f, 0.05f);
        /// <summary>Theme color: card background.</summary>
        public static readonly Color C_CARD             = new Color(0.13f, 0.10f, 0.06f);
        /// <summary>Theme color: panel border.</summary>
        public static readonly Color C_BORDER           = new Color(0.55f, 0.42f, 0.18f);
        /// <summary>Theme color: gold accent (headings, active tabs).</summary>
        public static readonly Color C_GOLD             = new Color(0.90f, 0.72f, 0.30f);
        /// <summary>Theme color: dimmed gold.</summary>
        private static readonly Color C_GOLD_DIM        = new Color(0.70f, 0.55f, 0.20f);
        /// <summary>Theme color: primary text.</summary>
        public static readonly Color C_TEXT             = new Color(0.95f, 0.90f, 0.75f);
        /// <summary>Theme color: dimmed secondary text.</summary>
        public static readonly Color C_TEXT_DIM         = new Color(0.60f, 0.55f, 0.45f);
        /// <summary>Theme color: active accent.</summary>
        public static readonly Color C_ACCENT_ACTIVE    = new Color(1.00f, 0.80f, 0.35f);
        /// <summary>Theme color: danger red.</summary>
        private static readonly Color C_DANGER          = new Color(0.75f, 0.25f, 0.20f);

        /// <summary>State color: feature enabled.</summary>
        public static readonly Color C_ON               = new Color(0.22f, 0.72f, 0.32f, 1f);
        /// <summary>State color: feature disabled.</summary>
        public static readonly Color C_OFF              = new Color(0.45f, 0.18f, 0.18f, 1f);
        /// <summary>Theme color: button background.</summary>
        public static readonly Color C_BTN              = C_CARD;
        /// <summary>Theme color: hot button background.</summary>
        public static readonly Color C_BTN_HOT          = new Color(0.18f, 0.14f, 0.08f, 1f);
        /// <summary>Theme color: danger red.</summary>
        public static readonly Color C_DANGER_BG        = C_DANGER;
        /// <summary>State color: locked feature text.</summary>
        public static readonly Color C_LOCK             = new Color(0.96f, 0.35f, 0.35f, 1f);
        /// <summary>Theme color: locked feature background.</summary>
        public static readonly Color C_LOCK_BG          = new Color(0.35f, 0.10f, 0.10f, 1f);
        /// <summary>Theme color: coming-soon background.</summary>
        public static readonly Color C_SOON_BG          = new Color(0.22f, 0.22f, 0.22f, 1f);

        #endregion

        #region CONSTANTS
        /// <summary>Sidebar tab order across all categories.</summary>
        private static readonly TabCategory[] TAB_CATEGORIES =
        {
            TabCategory.Main, TabCategory.Cheats, TabCategory.Lab, TabCategory.Hard,
            TabCategory.Info, TabCategory.Guide, TabCategory.Settings, TabCategory.Report
        };
        /// <summary>Sidebar width in pixels.</summary>
        private const float SIDEBAR_W = 140f;
        /// <summary>Header height in pixels.</summary>
        private const float HEADER_H = 60f;
        /// <summary>Display version string of the mod.</summary>
        private static readonly string MOD_VERSION = ModVersion.DISPLAY;
        /// <summary>Localization key for the menu credit line.</summary>
        private const string MENU_CREDIT_KEY = "menu.credit";
        /// <summary>Key that toggles the mod menu (F1).</summary>
        private const KeyCode MENU_TOGGLE_KEY = KeyCode.F1;

        #endregion

        #region STATE
        /// <summary>Whether the mod menu is currently visible.</summary>
        private bool         _isVisible = false;
        /// <summary>Currently selected tab category.</summary>
        private TabCategory  _activeTab = TabCategory.Main;
        /// <summary>Screen rectangle of the mod menu window.</summary>
        private Rect         _windowRect = new Rect(30, 110, 600, 500);
        /// <summary>Whether the user is dragging the resize handle.</summary>
        private bool         _isResizing = false;
        /// <summary>Scroll position per tab category.</summary>
        private Vector2[]    _scrollPos = new Vector2[8];

        /// <summary>Center-screen feedback message text.</summary>
        private string _feedbackMsg = "";
        /// <summary>Remaining display time of the feedback message.</summary>
        private float  _feedbackTimer = 0f;

        /// <summary>Whether the reset button awaits a second confirmation click.</summary>
        private bool  _resetConfirmPending = false;
        /// <summary>Countdown for the reset confirmation window.</summary>
        private float _resetConfirmTimer   = 0f;

        /// <summary>All feature metadata built by ModMenuFeatures.</summary>
        private FeatureMeta[] _features;
        /// <summary>Report tab status per feature id (0 untested, 1 works, 2 broken).</summary>
        private Dictionary<string, int> _featureStatus = new Dictionary<string, int>();

        #endregion

        #region NOTIFICATIONS
        /// <summary>A transient toast notification shown on screen.</summary>
        public class Notification
        {
            /// <summary>Notification text.</summary>
            public string Message;
            /// <summary>Time (unscaled) when the notification expires.</summary>
            public float ExpiryTimestamp;
            /// <summary>Current fade alpha of the notification.</summary>
            public float Alpha = 1f;
            /// <summary>Text color of the notification.</summary>
            public Color TextColor;

            /// <summary>Creates a notification expiring after the given duration.</summary>
            /// <param name="msg">Notification text.</param>
            /// <param name="dur">Display duration in seconds.</param>
            /// <param name="color">Text color.</param>
            public Notification(string msg, float dur, Color color)
            {
                Message    = msg;
                ExpiryTimestamp = Time.unscaledTime + dur;
                TextColor  = color;
            }

            /// <summary>Determines whether the notification has expired (with a 1s grace period).</summary>
            /// <returns>True once the notification is no longer displayed.</returns>
            public bool IsExpired() => Time.unscaledTime > ExpiryTimestamp + 1f;

            /// <summary>Updates the fade alpha near the end of the notification lifetime.</summary>
            public void UpdateAlpha()
            {
                float left = ExpiryTimestamp - Time.unscaledTime;
                Alpha = left < 1f ? Mathf.Max(0f, left) : 1f;
            }
        }

        /// <summary>A transient toast notification shown on screen.</summary>
        private static readonly List<Notification> _notifications = new List<Notification>();
        /// <summary>Display duration of a notification in seconds.</summary>
        private const float NotificationDuration = 5f;
        /// <summary>Maximum number of notifications kept at once.</summary>
        private const int MaxNotifications = 8;

        #endregion

        #region STYLES
        /// <summary>GUI style: window frame.</summary>
        private GUIStyle _styleWindow;
        /// <summary>GUI style: window title.</summary>
        private GUIStyle _styleTitle;
        /// <summary>GUI style: subtitle text.</summary>
        private GUIStyle _styleSubtitle;
        /// <summary>GUI style: section heading.</summary>
        private GUIStyle _styleSectionLabel;
        /// <summary>GUI style: body text.</summary>
        private GUIStyle _styleBodyText;
        /// <summary>GUI style: dimmed italic text.</summary>
        private GUIStyle _styleDimText;
        /// <summary>GUI style: primary button.</summary>
        private GUIStyle _styleBtn;
        /// <summary>GUI style: dimmed button.</summary>
        private GUIStyle _styleBtnDim;
        /// <summary>GUI style: sidebar tab button.</summary>
        private GUIStyle _styleTabBtn;
        /// <summary>GUI style: small state pill button.</summary>
        private GUIStyle _stylePill;
        /// <summary>GUI style: notification text.</summary>
        private GUIStyle _styleNotif;
        /// <summary>GUI style: credit footer.</summary>
        private GUIStyle _styleCredit;
        /// <summary>GUI style: locked feature hint.</summary>
        private GUIStyle _styleLocked;
        /// <summary>GUI style: card container.</summary>
        private GUIStyle _styleCard;
        /// <summary>Whether the GUI styles have been built once.</summary>
        private bool _stylesBuilt = false;

        #endregion

        #region LIFECYCLE
        void Start()
        {
            _features = ModMenuFeatures.Build();
            LoadFromSettings();
            TTSManager.Initialize();
            Speak(LocalizationService.Get("menu.notification.initialized"), C_ON);
        }

        void Update()
        {
            if (Input.GetKeyDown(MENU_TOGGLE_KEY))
            {
                _isVisible = !_isVisible;
                if (!_isVisible)
                {
                    if (CursorSystem.Inst != null)
                    {
                        CursorSystem.Inst.SetForceVisibleCursor(false);
                    }
                    else
                    {
                        Cursor.visible = false;
                        Cursor.lockState = CursorLockMode.Locked;
                    }
                    SaveToSettings();
                }
                else
                {
                    if (CursorSystem.Inst != null)
                    {
                        CursorSystem.Inst.SetForceVisibleCursor(true);
                    }
                }
            }

            if (_isVisible)
            {
                if (CursorSystem.Inst == null)
                {
                    if (!Cursor.visible) Cursor.visible = true;
                    if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
                }
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                DisplayTimes = !DisplayTimes;
                Settings.DisplayTimes.Value = DisplayTimes;
            }

            if (Input.GetKeyDown(KeyCode.F3))
                KingdomEnhanced.Features.KingdomMonitor.Instance?.Toggle();

            if (MessageTimer > 0f) MessageTimer -= Time.deltaTime;
            if (_feedbackTimer > 0f) _feedbackTimer -= Time.deltaTime;
            if (_resetConfirmTimer > 0f) { _resetConfirmTimer -= Time.deltaTime; if (_resetConfirmTimer <= 0f) _resetConfirmPending = false; }

            _notifications.RemoveAll(n => n.IsExpired());
        }

        /// <summary>Cached window draw delegate for GUI.Window.</summary>
        private GUI.WindowFunction _drawWindowFunc;

        void OnGUI()
        {
            BuildStyles();
            DrawFeedbackOverlay();
            DrawNotificationLog();

            if (!_isVisible) return;

            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(C_BG.r, C_BG.g, C_BG.b, MenuOpacity);

            if (_drawWindowFunc == null) _drawWindowFunc = (GUI.WindowFunction)DrawWindow;
            _windowRect = GUI.Window(9900, _windowRect, _drawWindowFunc, GUIContent.none, _styleWindow);

            GUI.backgroundColor = originalBg;
        }

        /// <summary>Builds all cached GUIStyles once on first render.</summary>
        private void BuildStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            _styleWindow = new GUIStyle(GUI.skin.window) {
                padding = new RectOffset(0, 0, 0, 0),
                normal = { background = GUI.skin.box.normal.background },
                onNormal = { background = GUI.skin.box.normal.background }
            };

            _styleTitle = new GUIStyle(GUI.skin.label) {
                fontSize = 16, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, richText = true,
                normal = { textColor = C_GOLD }
            };

            _styleSubtitle = new GUIStyle(GUI.skin.label) {
                fontSize = 11, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = C_TEXT_DIM }
            };

            _styleSectionLabel = new GUIStyle(GUI.skin.label) {
                fontSize = 12, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 4, 6, 4),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { textColor = C_GOLD }
            };

            _styleBodyText = new GUIStyle(GUI.skin.label) {
                fontSize = 12, richText = true, wordWrap = true,
                normal = { textColor = C_TEXT }
            };

            _styleDimText = new GUIStyle(GUI.skin.label) {
                fontSize = 11, fontStyle = FontStyle.Italic,
                richText = true,
                normal = { textColor = C_TEXT_DIM }
            };

            _styleBtn = new GUIStyle(GUI.skin.button) {
                fontSize = 12, richText = true,
                padding  = new RectOffset(8, 8, 5, 5),
                normal = { textColor = C_TEXT },
                hover = { textColor = C_GOLD },
                active = { textColor = C_ACCENT_ACTIVE },
            };

            _styleBtnDim = new GUIStyle(GUI.skin.button) {
                fontSize = 12, richText = true,
                padding  = new RectOffset(8, 8, 5, 5),
                normal = { textColor = C_TEXT_DIM },
                hover = { textColor = C_GOLD },
                active = { textColor = C_ACCENT_ACTIVE },
            };

            _styleTabBtn = new GUIStyle(GUI.skin.button) {
                fontSize = 13, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(16, 8, 8, 8),
                margin = new RectOffset(0, 0, 2, 2),
                normal = { textColor = C_TEXT_DIM, background = null },
                hover = { textColor = C_GOLD, background = null },
                active = { textColor = C_ACCENT_ACTIVE, background = null }
            };

            _stylePill = new GUIStyle(GUI.skin.button);
            _stylePill.alignment = TextAnchor.MiddleCenter;
            _stylePill.fontStyle = FontStyle.Bold;
            _stylePill.fixedHeight = 24;
            _stylePill.fontSize = 10;
            _stylePill.normal.textColor = Color.white;
            _stylePill.padding = new RectOffset(4, 4, 2, 2);
            _stylePill.margin = new RectOffset(0, 0, 2, 2);
            _stylePill.border = new RectOffset(2, 2, 2, 2);
            _styleNotif = new GUIStyle(GUI.skin.label) {
                fontSize = 13, fontStyle = FontStyle.Bold,
                richText = true, alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            _styleCredit = new GUIStyle(GUI.skin.label) {
                fontSize = 9, alignment = TextAnchor.LowerRight,
                normal = { textColor = C_TEXT_DIM }
            };

            _styleLocked = new GUIStyle(GUI.skin.label) {
                fontSize = 11, fontStyle = FontStyle.Italic,
                richText = true,
                padding = new RectOffset(0, 0, 4, 4),
                normal = { textColor = C_LOCK }
            };

            _styleCard = new GUIStyle(GUI.skin.box) {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(4, 4, 4, 4)
            };
        }

        /// <summary>
        /// Returns the main menu tab label resource key for a tab category.
        /// </summary>
        /// <param name="category">Target tab category.</param>
        /// <returns>The tab label resource key.</returns>
        private static string GetTabLabelKey(TabCategory category)
        {
            switch (category)
            {
                case TabCategory.Main: return "menu.tab.main";
                case TabCategory.Cheats: return "menu.tab.cheats";
                case TabCategory.Lab: return "menu.tab.lab";
                case TabCategory.Hard: return "menu.tab.hard";
                case TabCategory.Info: return "menu.tab.info";
                case TabCategory.Guide: return "menu.tab.guide";
                case TabCategory.Settings: return "menu.tab.settings";
                case TabCategory.Report: return "menu.tab.report";
                default: return "menu.tab.main";
            }
        }

        /// <summary>
        /// Returns the report badge abbreviation resource key for a tab category.
        /// </summary>
        /// <param name="category">Target tab category.</param>
        /// <returns>The report badge abbreviation resource key.</returns>
        private static string GetReportBadgeKey(TabCategory category)
        {
            switch (category)
            {
                case TabCategory.Main: return "menu.report.badge.main";
                case TabCategory.Cheats: return "menu.report.badge.cheats";
                case TabCategory.Lab: return "menu.report.badge.lab";
                case TabCategory.Hard: return "menu.report.badge.hard";
                case TabCategory.Info: return "menu.report.badge.info";
                case TabCategory.Guide: return "menu.report.badge.guide";
                case TabCategory.Settings: return "menu.report.badge.settings";
                case TabCategory.Report: return "menu.report.badge.report";
                default: return "menu.report.badge.main";
            }
        }

        /// <summary>
        /// Gets the tab title text in the current language.
        /// </summary>
        /// <param name="category">Target tab category.</param>
        /// <returns>The resolved tab title text.</returns>
        private static string GetTabLabelText(TabCategory category)
        {
            return LocalizationService.Get(GetTabLabelKey(category));
        }

        /// <summary>
        /// Formats a slider value into the multiplier text used by the menu.
        /// </summary>
        /// <param name="value">Value to format.</param>
        /// <returns>The multiplier text following the existing precision rules.</returns>
        private static string FormatScaledValue(float value)
        {
            return value < 0.01f
                ? $"{value:F4}"
                : value < 0.1f
                    ? $"{value:F3}"
                    : value < 1f
                        ? $"{value:F2}"
                        : $"{value:F1}";
        }

        /// <summary>
        /// Gets the display text of a language code under the current UI language.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>The language display name; falls back to the code when missing.</returns>
        private static string GetLanguageOptionLabel(string languageCode)
        {
            string key = $"settings.language.option.{languageCode}";
            string value = LocalizationService.Get(key);
            return string.Equals(value, key, StringComparison.Ordinal) ? languageCode : value;
        }

        #endregion

        #region UI RENDERING
        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            
            GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(HEADER_H));
            GUILayout.BeginVertical();
            GUILayout.Space(8);
            GUILayout.Label(LocalizationService.Get("menu.title"), _styleTitle);
            GUILayout.Label(MOD_VERSION, _styleSubtitle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            
            GUILayout.BeginHorizontal();

            
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(SIDEBAR_W));
            GUILayout.Space(10);
            for (int i = 0; i < TAB_CATEGORIES.Length; i++)
            {
                TabCategory tabCategory = TAB_CATEGORIES[i];
                bool active = _activeTab == tabCategory;
                Color originalColor = GUI.color;
                GUI.color = active ? C_GOLD : Color.white;
                
                if (GUILayout.Button(GetTabLabelText(tabCategory), _styleTabBtn, GUILayout.Height(32)))
                {
                    if ((int)_activeTab != i) _scrollPos[i] = Vector2.zero;
                    _activeTab = tabCategory;
                }
                    
                GUI.color = originalColor;
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(LocalizationService.Get(MENU_CREDIT_KEY), _styleCredit);
            GUILayout.Space(10);
            GUILayout.EndVertical();

            
            GUILayout.BeginVertical();
            _scrollPos[(int)_activeTab] = GUILayout.BeginScrollView(_scrollPos[(int)_activeTab]);
            DrawCurrentTab();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            
            GUILayout.EndHorizontal();

            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect resizeRect = GUILayoutUtility.GetRect(20, 20);
            GUI.Label(resizeRect, "➘", new GUIStyle(_styleDimText) { alignment = TextAnchor.LowerRight });
            HandleResize(resizeRect);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, _windowRect.width, HEADER_H));
            
            if (GUI.changed) SaveToSettings();
        }

        private void HandleResize(Rect grip)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && grip.Contains(e.mousePosition))
            { _isResizing = true; e.Use(); }
            else if (e.type == EventType.MouseUp) _isResizing = false;

            if (_isResizing && e.type == EventType.MouseDrag)
            {
                _windowRect.width  = Mathf.Max(500, _windowRect.width  + e.delta.x);
                _windowRect.height = Mathf.Max(400, _windowRect.height + e.delta.y);
            }
        }

        /// <summary>Loads all feature toggles and values from the BepInEx config into the static fields.</summary>
        private void LoadFromSettings()
        {
            ShowStaminaBar        = Settings.ShowStaminaBar.Value;
            DisplayTimes          = Settings.DisplayTimes.Value;
            ShowGreedCounter      = Settings.ShowGreedCounter.Value;
            Use12HourClock        = Settings.Use12HourClock.Value;
            EnableAccessibility   = Settings.EnableAccessibility.Value;
            EnableTTS             = Settings.EnableTTS.Value;
            NarratorQueueMode     = Settings.NarratorQueueMode.Value;
            SimplifyNames         = Settings.SimplifyNames.Value;
            EnableCastleAnnouncer = Settings.EnableCastleAnnouncer.Value;
            DebugZones            = Settings.DebugZones.Value;
            CheatsUnlocked        = Settings.CheatsUnlocked.Value;
            SpeedMultiplier       = Settings.SpeedMultiplier.Value;
            InfiniteStamina       = Settings.InfiniteStamina.Value;
            InvincibleWalls       = Settings.InvincibleWalls.Value;
            NoToolCooldowns       = Settings.NoToolCooldowns.Value;
            ArtemisArrowCount     = Settings.ArtemisArrowCount.Value;
            ArtemisRangeMult      = Settings.ArtemisRangeMult.Value;
            ArtemisArrowDamageMult = Settings.ArtemisArrowDamageMult.Value;
            CoinIncomeMult        = Settings.CoinIncomeMult.Value;
            BagDropMult           = Settings.BagDropMult.Value;
            SpawnUnitCount        = Settings.SpawnUnitCount.Value;
            HyperBuilders         = Settings.HyperBuilders.Value;
            LargerCamps           = Settings.LargerCamps.Value;
            BetterKnight          = Settings.BetterKnight.Value;
            BetterCitizenHouses   = Settings.BetterCitizenHouses.Value;
            LockSummer            = Settings.LockSummer.Value;
            ClearWeather          = Settings.ClearWeather.Value;
            NoBloodMoons          = Settings.NoBloodMoons.Value;
            CoinsStayDry          = Settings.CoinsStayDry.Value;
            ArcherFireBoost       = Settings.ArcherFireBoost.Value;
            BerserkerRage         = Settings.BerserkerRage.Value;
            NinjaSpeedBoost       = Settings.NinjaSpeedBoost.Value;
            RecruitCap            = Settings.RecruitCapOverride.Value;
            TreeRegrowthMult      = Settings.TreeRegrowthMult.Value;
            AnimalSpawnBoost      = Settings.AnimalSpawnBoost.Value;
            InstantDaySkip        = Settings.InstantDaySkip.Value;
            FarmOutputBoost       = Settings.FarmOutputBoost.Value;
            TowerFireBoost        = Settings.TowerFireBoost.Value;
            BallistaBoost         = Settings.BallistaBoost.Value;
            BallistaReloadMult    = Settings.BallistaReloadMult.Value;
            BallistaFlightMult    = Settings.BallistaFlightMult.Value;
            CatapultBoost         = Settings.CatapultBoost.Value;
            CatapultReloadMult    = Settings.CatapultReloadMult.Value;
            CatapultFlightMult    = Settings.CatapultFlightMult.Value;
            InstantCastle         = Settings.InstantCastle.Value;
            BuilderSpeedMult      = Settings.BuilderSpeedMult.Value;
            BuilderEfficiencyMult = Settings.BuilderWorkMult.Value;
            EnableSizeHack        = Settings.EnableSizeHack.Value;
            TargetSize            = Settings.TargetSize.Value;
            SteedSpeedMult        = Settings.SteedSpeedMult.Value;
            ChargeDmgBoost        = Settings.ChargeDmgBoost.Value;
            BuffAuraDuration      = Settings.BuffAuraDuration.Value;
            WaveSizeMult          = Settings.WaveSizeMult.Value;
            EnemySpeedMult        = Settings.EnemySpeedMult.Value;
            PortalSpawnRate       = Settings.PortalSpawnRate.Value;
            NoCrownStealing       = Settings.NoCrownStealing.Value;
            GreedQueenHPScale     = Settings.GreedQueenHPScale.Value;
            DirectorThreatMult    = Settings.DirectorThreatMult.Value;
        }

        /// <summary>Persists all feature toggles and values from the static fields to the BepInEx config.</summary>
        private void SaveToSettings()
        {
            Settings.ShowStaminaBar.Value        = ShowStaminaBar;
            Settings.DisplayTimes.Value          = DisplayTimes;
            Settings.ShowGreedCounter.Value      = ShowGreedCounter;
            Settings.Use12HourClock.Value        = Use12HourClock;
            Settings.EnableAccessibility.Value   = EnableAccessibility;
            Settings.EnableTTS.Value             = EnableTTS;
            Settings.NarratorQueueMode.Value     = NarratorQueueMode;
            Settings.SimplifyNames.Value         = SimplifyNames;
            Settings.EnableCastleAnnouncer.Value = EnableCastleAnnouncer;
            Settings.DebugZones.Value            = DebugZones;
            Settings.CheatsUnlocked.Value        = CheatsUnlocked;
            Settings.SpeedMultiplier.Value       = SpeedMultiplier;
            Settings.InfiniteStamina.Value       = InfiniteStamina;
            Settings.InvincibleWalls.Value       = InvincibleWalls;
            Settings.NoToolCooldowns.Value       = NoToolCooldowns;
            Settings.ArtemisArrowCount.Value      = ArtemisArrowCount;
            Settings.ArtemisRangeMult.Value       = ArtemisRangeMult;
            Settings.ArtemisArrowDamageMult.Value = ArtemisArrowDamageMult;
            Settings.CoinIncomeMult.Value        = CoinIncomeMult;
            Settings.BagDropMult.Value           = BagDropMult;
            Settings.SpawnUnitCount.Value        = SpawnUnitCount;
            Settings.HyperBuilders.Value         = HyperBuilders;
            Settings.LargerCamps.Value           = LargerCamps;
            Settings.BetterKnight.Value          = BetterKnight;
            Settings.BetterCitizenHouses.Value   = BetterCitizenHouses;
            Settings.LockSummer.Value            = LockSummer;
            Settings.ClearWeather.Value          = ClearWeather;
            Settings.NoBloodMoons.Value          = NoBloodMoons;
            Settings.CoinsStayDry.Value          = CoinsStayDry;
            Settings.ArcherFireBoost.Value       = ArcherFireBoost;
            Settings.BerserkerRage.Value         = BerserkerRage;
            Settings.NinjaSpeedBoost.Value       = NinjaSpeedBoost;
            Settings.RecruitCapOverride.Value    = RecruitCap;
            Settings.TreeRegrowthMult.Value      = TreeRegrowthMult;
            Settings.AnimalSpawnBoost.Value      = AnimalSpawnBoost;
            Settings.InstantDaySkip.Value        = InstantDaySkip;
            Settings.FarmOutputBoost.Value       = FarmOutputBoost;
            Settings.TowerFireBoost.Value        = TowerFireBoost;
            Settings.BallistaBoost.Value         = BallistaBoost;
            Settings.BallistaReloadMult.Value    = BallistaReloadMult;
            Settings.BallistaFlightMult.Value    = BallistaFlightMult;
            Settings.CatapultBoost.Value         = CatapultBoost;
            Settings.CatapultReloadMult.Value    = CatapultReloadMult;
            Settings.CatapultFlightMult.Value    = CatapultFlightMult;
            Settings.InstantCastle.Value         = InstantCastle;
            Settings.BuilderSpeedMult.Value      = BuilderSpeedMult;
            Settings.BuilderWorkMult.Value       = BuilderEfficiencyMult;
            Settings.EnableSizeHack.Value        = EnableSizeHack;
            Settings.TargetSize.Value            = TargetSize;
            Settings.SteedSpeedMult.Value        = SteedSpeedMult;
            Settings.ChargeDmgBoost.Value        = ChargeDmgBoost;
            Settings.BuffAuraDuration.Value      = BuffAuraDuration;
            Settings.WaveSizeMult.Value          = WaveSizeMult;
            Settings.EnemySpeedMult.Value        = EnemySpeedMult;
            Settings.PortalSpawnRate.Value       = PortalSpawnRate;
            Settings.NoCrownStealing.Value       = NoCrownStealing;
            Settings.GreedQueenHPScale.Value     = GreedQueenHPScale;
            Settings.DirectorThreatMult.Value    = DirectorThreatMult;
        }

        /// <summary>Draws the feature rows for the currently selected tab, grouped into section cards.</summary>
        private void DrawCurrentTab()
        {
            if (_activeTab == TabCategory.Cheats && !CheatsUnlocked)
            {
                DrawCheatsGate();
                return;
            }

            
            string lastSectionId = null;
            bool inCard = false;

            foreach (var f in _features)
            {
                if (f.Category != _activeTab) continue;

                string sectionId = f.SectionKey ?? f.Section;
                if (sectionId != lastSectionId)
                {
                    if (inCard) GUILayout.EndVertical(); 
                    GUILayout.BeginVertical(_styleCard);
                    inCard = true;
                    DrawFeatureSection(f);
                    lastSectionId = sectionId;
                }

                DrawFeatureRow(f);
            }
            
            if (inCard) GUILayout.EndVertical();
            
            
            switch (_activeTab)
            {
                case TabCategory.Info: DrawInfoTab(); break;
                case TabCategory.Settings: DrawSettingsTab(); break;
                case TabCategory.Guide: DrawGuideTab(); break;
                case TabCategory.Report: DrawReportTab(); break;
                case TabCategory.Lab: DrawLabExtras(); break;
            }
        }

        /// <summary>Draws the extra lab controls (instant day/night skip buttons).</summary>
        private void DrawLabExtras()
        {
            GUILayout.BeginVertical(_styleCard);
            GuiHelper.DrawSection("menu.lab.section.time_controls", _styleSectionLabel);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label(LocalizationService.Get("menu.lab.instant_time_jump"), _styleBodyText, GUILayout.Width(180));
            
            if (GUILayout.Button(LocalizationService.Get("menu.lab.button.skip_daytime"), _styleBtn, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            {
                Features.WorldManager.SkipDaytime();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button(LocalizationService.Get("menu.lab.button.skip_nighttime"), _styleBtn, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            {
                Features.WorldManager.SkipNighttime();
            }
            
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawFeatureRow(FeatureMeta f)
        {
            GUILayout.BeginHorizontal();
            
            
            GUILayout.Label(f.GetLabelText(), _styleBodyText, GUILayout.Width(180));
            
            
            bool isLocked = f.IsLocked != null && f.IsLocked();
            
            if (isLocked)
            {
                string reason = f.GetLockReasonText();
                GUILayout.Label(LocalizationService.Format("menu.feature.locked_reason", reason), _styleLocked, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                return;
            }

            
            if (f.HasConflict != null && f.HasConflict())
            {
                GUI.color = Color.yellow;
                GUILayout.Label("!", _styleBodyText, GUILayout.Width(20));
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Space(24);
            }

            
            if (f.GetFloatValue != null)
            {
                float val = f.GetFloatValue();
                // Adaptive precision: show enough decimals to see small values
                string valStr = FormatScaledValue(val);
                GUILayout.Label(valStr, _styleBodyText, GUILayout.Width(46));
                float newVal = GUILayout.HorizontalSlider(val, f.MinVal, f.MaxVal, GUILayout.ExpandWidth(true));
                if (Math.Abs(newVal - val) > 0.00001f) f.SetFloatValue(newVal);
            }
            else if (f.OnAction != null)
            {
                
                if (GUILayout.Button(LocalizationService.Get("common.button.apply"), _styleBtn, GUILayout.Width(100))) f.OnAction();
            }
            else if (f.GetValue != null)
            {
                
                bool val = f.GetValue();

                GUI.backgroundColor = val ? C_ON : C_OFF;
                if (GUILayout.Button(LocalizationService.Get(val ? "common.state.on" : "common.state.off"), _stylePill, GUILayout.Width(60)))
                {
                    f.SetValue(!val);
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>Draws the unlock gate for the Cheats tab.</summary>
        private void DrawCheatsGate()
        {
            GUILayout.BeginVertical(_styleCard);
            GuiHelper.DrawSection("menu.cheats.section.access_required", _styleSectionLabel);
            GUILayout.Label(LocalizationService.Get("menu.cheats.access_description"), _styleBodyText);
            GUILayout.Space(10);
            
            if (GUILayout.Button(LocalizationService.Get("menu.cheats.unlock_button"), _styleBtn, GUILayout.Height(32)))
            {
                CheatsUnlocked = true;
                Settings.CheatsUnlocked.Value = true;
                Speak(LocalizationService.Get("menu.notification.cheats_unlocked"), C_ON);
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the feature section title, compatible with both legacy final text and new resource keys.
        /// </summary>
        /// <param name="feature">Feature metadata.</param>
        private void DrawFeatureSection(FeatureMeta feature)
        {
            GUILayout.Space(18f);
            GUILayout.Label(feature.GetSectionText(), _styleSectionLabel);
            GUILayout.Space(6f);
        }

        /// <summary>Draws the guide tab listing every feature with its current state.</summary>
        private void DrawGuideTab()
        {
            GUILayout.BeginVertical(_styleCard);
            GuiHelper.DrawSection("menu.guide.section.title", _styleSectionLabel);
            GUILayout.Label(LocalizationService.Get("menu.guide.description"), _styleSubtitle);
            GUILayout.Space(10);

            
            foreach (var f in _features)
            {
                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    LocalizationService.Format("menu.guide.entry_title", GetTabLabelText(f.Category), f.GetLabelText()),
                    _styleTitle,
                    GUILayout.Height(20));
                GUILayout.FlexibleSpace();
                
                
                string stateStr = "";
                Color stateColor = Color.white;

                if (f.IsLocked != null && f.IsLocked())
                {
                    stateStr = f.GetLockReasonText();
                    stateColor = C_LOCK;
                }
                else if (f.GetFloatValue != null)
                {
                    float gv = f.GetFloatValue();
                    stateStr = LocalizationService.Format("common.value.multiplier", FormatScaledValue(gv));
                }
                else if (f.OnAction != null)
                {
                    stateStr = LocalizationService.Get("common.state.action");
                }
                else if (f.GetValue != null)
                {
                    stateStr = LocalizationService.Get(f.GetValue() ? "common.state.on" : "common.state.off");
                    stateColor = f.GetValue() ? C_ON : Color.white;
                }

                GUILayout.Label(stateStr, new GUIStyle(_styleTitle) { normal = { textColor = stateColor } }, GUILayout.Height(20));
                GUILayout.EndHorizontal();
                
                GUILayout.Label(f.GetDescriptionText(), _styleDimText);
                GUILayout.Space(4);
            }

            GUILayout.EndVertical();
        }

        /// <summary>Draws the report tab with per-feature test status and a copy-to-clipboard report builder.</summary>
        private void DrawReportTab()
        {
            GUILayout.BeginVertical(_styleCard);
            GuiHelper.DrawSection("menu.report.section.title", _styleSectionLabel);
            GUILayout.Label(LocalizationService.Get("menu.report.description"), _styleDimText);
            GUILayout.Space(8);

            
            foreach (var f in _features)
            {
                if (!_featureStatus.ContainsKey(f.Id)) _featureStatus[f.Id] = 0;
                int status = _featureStatus[f.Id];

                string statusLabel = status == 1
                    ? LocalizationService.Get("menu.report.status.works")
                    : status == 2
                        ? LocalizationService.Get("menu.report.status.broken")
                        : LocalizationService.Get("menu.report.status.not_tested");

                GUILayout.BeginHorizontal();
                
                
                string catDisplay = LocalizationService.Get(GetReportBadgeKey(f.Category));
                GUILayout.Label(LocalizationService.Format("menu.report.badge_format", catDisplay), _styleBodyText, GUILayout.Width(60));
                
                GUILayout.Label(f.GetLabelText(), _styleBodyText, GUILayout.Width(180));
                GUILayout.Label(statusLabel, _styleBodyText, GUILayout.ExpandWidth(true));

                Color prev = GUI.color;
                
                GUI.color = status == 1 ? C_ON : Color.white;
                if (GUILayout.Button(LocalizationService.Get("menu.report.button.works"), _styleBtn, GUILayout.Width(60))) _featureStatus[f.Id] = status == 1 ? 0 : 1;
                
                GUI.color = status == 2 ? C_LOCK : Color.white;
                if (GUILayout.Button(LocalizationService.Get("menu.report.button.broken"), _styleBtn, GUILayout.Width(60))) _featureStatus[f.Id] = status == 2 ? 0 : 2;
                
                GUI.color = prev;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(14);
            if (GUILayout.Button(LocalizationService.Get("menu.report.button.copy"), _styleBtn, GUILayout.Height(36)))
            {
                var sb = new StringBuilder();
                sb.AppendLine(LocalizationService.Format("menu.report.copy.header", System.DateTime.Now));
                sb.AppendLine(LocalizationService.Format("menu.report.copy.version", MOD_VERSION));
                
                foreach(var cat in (TabCategory[])Enum.GetValues(typeof(TabCategory)))
                {
                    var items = _features.Where(x => x.Category == cat).ToList();
                    if (items.Count == 0) continue;

                    sb.AppendLine();
                    sb.AppendLine(LocalizationService.Format("menu.report.copy.category", GetTabLabelText(cat)));
                    foreach (var f in items)
                    {
                        int s = _featureStatus.ContainsKey(f.Id) ? _featureStatus[f.Id] : 0;
                        string statusStr = s == 1
                            ? LocalizationService.Get("menu.report.copy.status.works")
                            : s == 2
                                ? LocalizationService.Get("menu.report.copy.status.broken")
                                : LocalizationService.Get("menu.report.copy.status.not_tested");
                        sb.AppendLine(LocalizationService.Format("menu.report.copy.item", statusStr, f.GetLabelText()));
                    }
                }

                GUIUtility.systemCopyBuffer = sb.ToString();
                ShowFeedback(LocalizationService.Get("menu.feedback.report_copied"));
            }

            GUILayout.EndVertical();
        }

        /// <summary>Draws the info tab with version and hotkey tips.</summary>
        private void DrawInfoTab()
        {
            GUILayout.BeginVertical(_styleCard);
            GuiHelper.DrawSection("menu.info.section.title", _styleSectionLabel);
            GUILayout.Label(LocalizationService.Format("menu.info.version", MOD_VERSION), _styleTitle);
            GUILayout.Space(10);
            GUILayout.Label(LocalizationService.Get("menu.info.developer"), _styleBodyText);
            GUILayout.Label(LocalizationService.Get("menu.info.special_thanks"), _styleBodyText);
            GUILayout.Space(20);
            GUILayout.Label(LocalizationService.Get("menu.info.tip.f1"), _styleDimText);
            GUILayout.Label(LocalizationService.Get("menu.info.tip.f3"), _styleDimText);
            GUILayout.Label(LocalizationService.Get("menu.info.tip.f4"), _styleDimText);
            GUILayout.EndVertical();
        }

        /// <summary>Draws the settings tab (window scale, opacity, language and one-click reset).</summary>
        private void DrawSettingsTab()
        {
            GUILayout.BeginVertical(_styleCard);
            GuiHelper.DrawSection("settings.section.global", _styleSectionLabel);

            GUILayout.Label(LocalizationService.Get("settings.window_scale.label"), _styleBodyText);
            WindowScale = GUILayout.HorizontalSlider(WindowScale, 0.5f, 2.0f);
            
            GUILayout.Label(LocalizationService.Get("settings.menu_opacity.label"), _styleBodyText);
            MenuOpacity = GUILayout.HorizontalSlider(MenuOpacity, 0.5f, 1.0f);

            GUILayout.Space(10);

            GUILayout.Label(LocalizationService.Get("settings.language.label"), _styleBodyText);
            GUILayout.BeginHorizontal();
            foreach (string languageCode in LocalizationService.GetAvailableLanguages())
            {
                bool isActiveLanguage = string.Equals(LocalizationService.CurrentLanguageCode, languageCode, StringComparison.OrdinalIgnoreCase);
                Color originalBackground = GUI.backgroundColor;
                GUI.backgroundColor = isActiveLanguage ? C_ON : C_BTN;
                if (GUILayout.Button(GetLanguageOptionLabel(languageCode), isActiveLanguage ? _styleBtn : _styleBtnDim, GUILayout.Height(28f)))
                {
                    LocalizationService.SetLanguage(languageCode);
                }

                GUI.backgroundColor = originalBackground;
                GUILayout.Space(6f);
            }

            GUILayout.EndHorizontal();
            GUILayout.Label(
                LocalizationService.Format("settings.language.current", GetLanguageOptionLabel(LocalizationService.CurrentLanguageCode)),
                _styleDimText);
            GUILayout.Label(LocalizationService.Get("settings.language.help"), _styleDimText);

            GUILayout.Space(10);

            if (GUILayout.Button(LocalizationService.Get("settings.reset.button"), _styleBtn))
            {
                if (!_resetConfirmPending)
                {
                    _resetConfirmPending = true;
                    _resetConfirmTimer = 3f;
                }
                else
                {
                    ResetAllSettings();
                    ShowFeedback(LocalizationService.Get("settings.reset.success"));
                    _resetConfirmPending = false;
                }
            }
            
            if (_resetConfirmPending)
            {
                GUILayout.Label(LocalizationService.Get("settings.reset.confirm"), _styleLocked);
            }

            GUILayout.EndVertical();
        }
        
        /// <summary>
        /// One-click reset: restores every bound ConfigEntry to its default value and refreshes in-memory feature toggles and UI settings.
        /// </summary>
        private void ResetAllSettings()
        {
            var fields = typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                Type fieldType = field.FieldType;
                if (!fieldType.IsGenericType || fieldType.GetGenericTypeDefinition() != typeof(ConfigEntry<>)) continue;

                var entry = field.GetValue(null) as ConfigEntryBase;
                if (entry != null) entry.BoxedValue = entry.DefaultValue;
            }

            WindowScale = 1.0f;
            MenuOpacity = 0.98f;
            LoadFromSettings();
        }

        /// <summary>Draws the center-screen feedback message overlay.</summary>
        private void DrawFeedbackOverlay()
        {
            if (_feedbackTimer <= 0) return;
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            GUI.color = new Color(1, 1, 1, Mathf.Clamp01(_feedbackTimer));
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100), _feedbackMsg, style);
            GUI.color = Color.white;
        }

        /// <summary>Draws the stacked notification toasts in the corner of the screen.</summary>
        private void DrawNotificationLog()
        {
            
            float y = Screen.height - 10; 
            
            
            
            
            
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                var n = _notifications[i];
                n.UpdateAlpha();
                if (n.Alpha <= 0) continue;

                GUI.color = new Color(n.TextColor.r, n.TextColor.g, n.TextColor.b, n.Alpha);
                
                y -= 26; 
                var rect = new Rect(10, y, 300, 24);
                GUI.Label(rect, n.Message, _styleNotif);
            }
            GUI.color = Color.white;
        }

        private void ShowFeedback(string msg)
        {
            _feedbackMsg = msg;
            _feedbackTimer = 2f;
        }

        
        public static void Speak(string text, Color color)
        {
            if (EnableTTS) TTSManager.Speak(text);
            _notifications.Add(new Notification(text, NotificationDuration, color));
        }

        
        public static void Speak(string text)
        {
            Speak(text, Color.white);
        }

        /// <summary>
        /// Speaks text (interruptible): clears the pending queue when interrupt is true.
        /// </summary>
        /// <param name="text">Text to speak.</param>
        /// <param name="interrupt">Whether to interrupt messages not yet spoken in the queue.</param>
        public static void Speak(string text, bool interrupt)
        {
            if (EnableTTS) TTSManager.Speak(text, interrupt);
            Speak(text, Color.white);
        }
        
        public static void GiveCurrency(int amount, bool isGem)
        {
            var player = Managers.Inst?.kingdom?.GetPlayer(0);
            if (player == null || player.wallet == null) return;

            if (isGem)
            {
                player.wallet.Gems = Mathf.Min(100, player.wallet.Gems + amount);
                Speak(LocalizationService.Format("menu.notification.gems_added", amount), C_GOLD);
            }
            else
            {
                player.wallet.Coins = Mathf.Min(100, player.wallet.Coins + amount);
                Speak(LocalizationService.Format("menu.notification.coins_added", amount), C_GOLD);
            }
        }

        /// <summary>Sets the first player wallet to 100 coins.</summary>
        public static void FillWallet()
        {
            var player = Managers.Inst?.kingdom?.GetPlayer(0);
            if (player == null || player.wallet == null) return;

            player.wallet.Coins = 100;
            Speak(LocalizationService.Get("menu.notification.wallet_filled"), C_GOLD);
        }

        /// <summary>Cycles the stamina bar visual style.</summary>
        public static void CycleStaminaBarStyle()
        {
            if (StaminaBarHolder.Instance == null) return;
            StaminaBarHolder.Instance.visualStyle =
                (StaminaBarHolder.Instance.visualStyle + 1) % 4;
            Speak(LocalizationService.Format("menu.notification.stamina_style", StaminaBarHolder.Instance.GetStyleName()));
        }

        /// <summary>Cycles the stamina bar screen position.</summary>
        public static void CycleStaminaBarPosition()
        {
            if (StaminaBarHolder.Instance == null) return;
            StaminaBarHolder.Instance.positionMode =
                (StaminaBarHolder.Instance.positionMode + 1) % 6;
            Speak(LocalizationService.Format("menu.notification.stamina_position", StaminaBarHolder.Instance.GetPositionName()));
        }

        #endregion
    }
}
