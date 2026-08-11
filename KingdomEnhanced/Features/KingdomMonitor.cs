using System;
using System.Linq;
using UnityEngine;
using KingdomEnhanced.Core;
using KingdomEnhanced.UI;
#if IL2CPP
using KingdomEnhanced.Shared.Attributes;
#endif

namespace KingdomEnhanced.Features
{
#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    /// <summary>
    /// In-game window monitoring kingdom state: population, wallet, day, cycle, and threat.
    /// </summary>
    public class KingdomMonitor : MonoBehaviour
    {
#if IL2CPP
        /// <summary>IL2CPP constructor required by Unity's Il2Cpp interop.</summary>
        public KingdomMonitor(IntPtr ptr) : base(ptr) { }
#endif
        /// <summary>Cached reference to the player's Kingdom.</summary>
        private Kingdom _kingdom;
        /// <summary>Cached reference to the EnemyManager.</summary>
        private EnemyManager _enemyManager;
        
        /// <summary>Global instance, assigned in Start().</summary>
        public static KingdomMonitor Instance { get; private set; }
        
        /// <summary>Whether the monitor window is currently shown.</summary>
        private bool _isVisible = true;
        /// <summary>Whether the monitor window is currently shown.</summary>
        public bool IsVisible => _isVisible;
        /// <summary>Window rectangle in screen space, draggable and resizable.</summary>
        private Rect _windowRect = new Rect(10, 10, 250, 350);
        /// <summary>Whether the user is currently dragging the resize handle.</summary>
        private bool _isResizing = false;

        
        /// <summary>Available visual styles for the monitor window.</summary>
        public enum MonitorStyle { Classic, Neon, Light, Ghost }
        /// <summary>Currently selected monitor style.</summary>
        private MonitorStyle _currentStyle = MonitorStyle.Classic;
        /// <summary>Style names in MonitorStyle order.</summary>
        private readonly string[] _styleNames = { "Classic", "Neon", "Light", "Ghost" };
        /// <summary>Shared button style used inside the window.</summary>
        private GUIStyle _styleBtn;
        /// <summary>Shared 1x1 button background texture.</summary>
        private Texture2D _btnTex;
        /// <summary>Guards one-time construction of GUIStyles.</summary>
        private bool _stylesBuilt;

        
        /// <summary>Census counters, polled one step per tick to spread frame cost.</summary>
        /// <summary>Archer population count (census step 0).</summary>
        private int _archerCount;
        /// <summary>Worker population count (census step 1).</summary>
        private int _workerCount;
        /// <summary>Peasant population count (census step 2).</summary>
        private int _peasantCount;
        /// <summary>Knight population count (census step 3).</summary>
        private int _knightCount;
        /// <summary>Enemy population count (census step 7).</summary>
        private int _enemyCount;
        /// <summary>Beggar population count (census step 4).</summary>
        private int _vagrantCount;
        /// <summary>Farmer population count (census step 5).</summary>
        private int _farmerCount;
        /// <summary>Pikeman population count (census step 6).</summary>
        private int _pikemanCount;
        /// <summary>Timestamp of the next census poll.</summary>
        private float _nextCensusTime;
        /// <summary>Next census step to poll (0-9, loops).</summary>
        private int _censusStep = 0;

        /// <summary>
        /// Language code used when the cached monitor strings were last generated.
        /// </summary>
        private string _localizedLanguageCode;

        /// <summary>
        /// Last polled island day count, used to rebuild the day text on language change.
        /// </summary>
        private int _currentDay;

        /// <summary>
        /// Last polled day/night cycle localization key, used to rebuild the day text on language change.
        /// </summary>
        private string _currentCycleKey = "monitor.cycle.unknown";

        /// <summary>
        /// Last polled threat state, used to keep the threat text's rich-text color on language change.
        /// </summary>
        private bool _isThreatDangerous;

        /// <summary>
        /// Last polled wallet coin count, used to rebuild the wallet text on language change.
        /// </summary>
        private int _walletCoins;

        /// <summary>
        /// Last polled wallet gem count, used to rebuild the wallet text on language change.
        /// </summary>
        private int _walletGems;

        /// <summary>Cached localized strings rendered in the window.</summary>
        /// <summary>Cached day + cycle text.</summary>
        private string _strDay;
        /// <summary>Cached threat text with rich-text color.</summary>
        private string _strThreat;
        /// <summary>Cached enemy/greed count text.</summary>
        private string _strGreed;
        /// <summary>Cached wallet coins/gems text.</summary>
        private string _strWallet;
        
        /// <summary>Cached archer population line.</summary>
        private string _strArcher;
        /// <summary>Cached worker population line.</summary>
        private string _strWorker;
        /// <summary>Cached peasant population line.</summary>
        private string _strPeasant;
        /// <summary>Cached farmer population line.</summary>
        private string _strFarmer;
        /// <summary>Cached pikeman population line.</summary>
        private string _strPikeman;
        /// <summary>Cached knight population line.</summary>
        private string _strKnight;
        /// <summary>Cached vagrant population line.</summary>
        private string _strVagrant;

        /// <summary>Color palettes, one per MonitorStyle.</summary>
        private StyleColors[] _stylePalette;

        /// <summary>Color and hex values defining a monitor style.</summary>
        private struct StyleColors
        {
            /// <summary>Background gradient bottom color.</summary>
            public Color bgBottom;
            /// <summary>Background gradient top color.</summary>
            public Color bgTop;
            /// <summary>Header text color.</summary>
            public Color header;
            /// <summary>Body text color.</summary>
            public Color body;
            /// <summary>Footer / resize handle color.</summary>
            public Color footer;
            /// <summary>Button background color.</summary>
            public Color btnBg;
            /// <summary>Button text color.</summary>
            public Color btnText;
            /// <summary>Window frame border color.</summary>
            public Color frameColor;
            /// <summary>Hex color for safe threat state.</summary>
            public string safeHex;
            /// <summary>Hex color for dangerous threat state.</summary>
            public string dangerHex;
            /// <summary>Background alpha applied to the window.</summary>
            public float baseAlpha;
            /// <summary>Window frame border thickness in pixels.</summary>
            public int frameThickness;

            /// <summary>Assigns all palette values from positional arguments.</summary>
            public StyleColors(Color bb, Color bt, Color h, Color b, Color f, Color bbgn, Color btnT, Color fc, string s, string d, float a, int ft)
            {
                bgBottom = bb; bgTop = bt; header = h; body = b; footer = f;
                btnBg = bbgn; btnText = btnT; frameColor = fc;
                safeHex = s; dangerHex = d; baseAlpha = a; frameThickness = ft;
            }
        }

        /// <summary>
        /// Initializes cached references, localized strings, visibility, and style palettes.
        /// </summary>
        private void Start()
        {
            Instance = this;
            _kingdom = FindFirstObjectByType<Kingdom>();
            _enemyManager = FindFirstObjectByType<EnemyManager>();
            _strDay = LocalizationService.Format("monitor.day", 0, LocalizationService.Get("monitor.cycle.unknown"));
            _strThreat = LocalizationService.Format("monitor.threat", "<color=#00ff00>" + LocalizationService.Get("monitor.threat.safe") + "</color>");
            _strGreed = LocalizationService.Format("monitor.greed", 0);
            _strWallet = LocalizationService.Format("monitor.wallet", 0, 0);
            _strArcher = LocalizationService.Format("monitor.population.archer", 0);
            _strWorker = LocalizationService.Format("monitor.population.worker", 0);
            _strPeasant = LocalizationService.Format("monitor.population.peasant", 0);
            _strFarmer = LocalizationService.Format("monitor.population.farmer", 0);
            _strPikeman = LocalizationService.Format("monitor.population.pikeman", 0);
            _strKnight = LocalizationService.Format("monitor.population.knight", 0);
            _strVagrant = LocalizationService.Format("monitor.population.vagrant", 0);
            _localizedLanguageCode = LocalizationService.CurrentLanguageCode;
            
            if (_kingdom == null) Plugin.Instance.LogSource.LogWarning("KingdomMonitor: Kingdom not found.");
            if (_enemyManager == null) Plugin.Instance.LogSource.LogWarning("KingdomMonitor: EnemyManager not found.");
            
            if (_kingdom != null && _kingdom.playerOne != null)
                _isVisible = false;

            
            _stylePalette = new StyleColors[]
            {
                
                new StyleColors(
                    new Color(0.12f, 0.12f, 0.12f, 1.0f),
                    new Color(0.18f, 0.18f, 0.18f, 1.0f),
                    new Color(0.95f, 0.90f, 0.70f),
                    new Color(0.85f, 0.85f, 0.85f),
                    new Color(0.60f, 0.60f, 0.60f),
                    new Color(0.25f, 0.20f, 0.15f, 0.9f),
                    new Color(0.95f, 0.90f, 0.70f),
                    new Color(0.75f, 0.65f, 0.45f, 1.0f),
                    "#44ff44", "#ff4444", 0.95f, 3
                ),
                
                new StyleColors(
                    new Color(0.05f, 0.0f, 0.15f, 1.0f),
                    new Color(0.10f, 0.0f, 0.25f, 1.0f),
                    new Color(0.0f, 1.0f, 1.0f),
                    new Color(0.0f, 1.0f, 0.5f),
                    new Color(1.0f, 0.0f, 1.0f),
                    new Color(0.0f, 0.3f, 0.4f, 0.9f),
                    new Color(0.0f, 1.0f, 1.0f),
                    new Color(0.0f, 1.0f, 1.0f, 1.0f),
                    "#00ff88", "#ff0088", 0.90f, 2
                ),
                
                new StyleColors(
                    new Color(0.92f, 0.92f, 0.88f, 1.0f),
                    new Color(0.98f, 0.98f, 0.95f, 1.0f),
                    new Color(0.15f, 0.10f, 0.05f),
                    new Color(0.25f, 0.20f, 0.15f),
                    new Color(0.45f, 0.40f, 0.35f),
                    new Color(0.75f, 0.70f, 0.65f, 0.9f),
                    new Color(0.15f, 0.10f, 0.05f),
                    new Color(0.35f, 0.30f, 0.25f, 1.0f),
                    "#006600", "#990000", 0.95f, 3
                ),
                
                new StyleColors(
                    new Color(0.00f, 0.00f, 0.00f, 0.00f),
                    new Color(0.00f, 0.00f, 0.00f, 0.00f),
                    new Color(1.00f, 1.00f, 0.80f),
                    new Color(0.95f, 0.95f, 0.95f),
                    new Color(0.85f, 0.85f, 0.85f),
                    new Color(0.00f, 0.00f, 0.00f, 0.7f),
                    new Color(1.00f, 1.00f, 0.80f),
                    new Color(1.0f, 1.0f, 1.0f, 0.5f),
                    "#7CFC00", "#FF4500", 0.00f, 2
                )
            };
        }

        /// <summary>Cached delegate for rendering the window.</summary>
        private GUI.WindowFunction _drawWindowFunc;
        /// <summary>Cached window background style.</summary>
        private GUIStyle _cachedWindowStyle;

        /// <summary>
        /// Renders the monitor window on the ImGUI layer when visible.
        /// </summary>
        private void OnGUI()
        {
            RefreshLocalizedStringsIfLanguageChanged();

            if (!_isVisible) return;
            BuildStyles();

            StyleColors colors = _stylePalette[(int)_currentStyle];
            
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 1f, 1f, colors.baseAlpha);
            
            if (_cachedWindowStyle == null)
            {
                _cachedWindowStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10 + colors.frameThickness, 10 + colors.frameThickness, 10 + colors.frameThickness, 10 + colors.frameThickness),
                    border = new RectOffset(colors.frameThickness, colors.frameThickness, colors.frameThickness, colors.frameThickness)
                };
            }
            
            if (_drawWindowFunc == null) _drawWindowFunc = (GUI.WindowFunction)DrawWindow;
            
            _windowRect = GUI.Window(999, _windowRect, _drawWindowFunc, LocalizationService.Get("monitor.window.title"), _cachedWindowStyle);
            
            GUI.backgroundColor = prevBg;
        }

        /// <summary>
        /// Builds the shared button style once, then reuses it.
        /// </summary>
        private void BuildStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            StyleColors colors = _stylePalette[(int)_currentStyle];

            
            _btnTex = new Texture2D(1, 1);
            _btnTex.SetPixel(0, 0, colors.btnBg);
            _btnTex.Apply();

            _styleBtn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = _btnTex, textColor = colors.btnText },
                padding = new RectOffset(4, 4, 2, 2),
                fixedWidth = 60,
                fixedHeight = 18
            };
        }

        /// <summary>
        /// Draws the window content and handles the resize handle.
        /// </summary>
        private void DrawWindow(int windowID)
        {
            StyleColors colors = _stylePalette[(int)_currentStyle];

            float yPos = 25f;

            GUI.contentColor = colors.header;
            GUI.Label(new Rect(15, yPos, 200, 20), _strDay);
            yPos += 25f;

            GUI.contentColor = colors.body;
            GUI.Label(new Rect(15, yPos, 200, 20), _strThreat); yPos += 20f;
            GUI.Label(new Rect(15, yPos, 200, 20), _strGreed); yPos += 20f;
            GUI.Label(new Rect(15, yPos, 200, 20), _strWallet); yPos += 25f;
            GUI.contentColor = Color.white;

            GUI.contentColor = colors.header;
            GUI.Label(new Rect(15, yPos, 200, 20), LocalizationService.Get("monitor.population.header")); yPos += 20f;
            GUI.contentColor = colors.body;
            
            if (_archerCount > 0) { GUI.Label(new Rect(15, yPos, 200, 20), _strArcher); yPos += 18f; }
            if (_workerCount > 0) { GUI.Label(new Rect(15, yPos, 200, 20), _strWorker); yPos += 18f; }
            if (_peasantCount > 0) { GUI.Label(new Rect(15, yPos, 200, 20), _strPeasant); yPos += 18f; }
            if (_farmerCount > 0) { GUI.Label(new Rect(15, yPos, 200, 20), _strFarmer); yPos += 18f; }
            if (_pikemanCount > 0) { GUI.Label(new Rect(15, yPos, 200, 20), _strPikeman); yPos += 18f; }
            if (_knightCount > 0) { GUI.Label(new Rect(15, yPos, 200, 20), _strKnight); yPos += 18f; }
            if (_vagrantCount > 0) { GUI.Label(new Rect(15, yPos, 200, 20), _strVagrant); yPos += 18f; }
            
            GUI.contentColor = Color.white; 

            
            var handleSize = 20f;
            var resizeRect = new Rect(_windowRect.width - handleSize, _windowRect.height - handleSize, handleSize, handleSize);
            GUI.color = colors.footer;
            GUI.Box(resizeRect, LocalizationService.Get("monitor.resize_handle"));
            GUI.color = Color.white;

            Event e = Event.current;
            if (e.type == EventType.MouseDown && resizeRect.Contains(e.mousePosition))
            {
                _isResizing = true;
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                _isResizing = false;
            }
            
            if (_isResizing && e.type == EventType.MouseDrag)
            {
                _windowRect.width = Mathf.Max(200, _windowRect.width + e.delta.x);
                _windowRect.height = Mathf.Max(200, _windowRect.height + e.delta.y);
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        
        /// <summary>Shows the monitor window.</summary>
        public void Show() => _isVisible = true;
        /// <summary>Hides the monitor window.</summary>
        public void Hide() => _isVisible = false;
        /// <summary>Toggles the monitor window visibility.</summary>
        public void Toggle() => _isVisible = !_isVisible;
        /// <summary>Cycles to the next monitor style and rebuilds cached styles.</summary>
        public void NextStyle()
        {
            _currentStyle = (MonitorStyle)(((int)_currentStyle + 1) % _stylePalette.Length);
            _stylesBuilt = false;
            _cachedWindowStyle = null;
        }

        /// <summary>
        /// Periodically polls kingdom state and refreshes cached strings while visible.
        /// </summary>
        private void Update()
        {
            RefreshLocalizedStringsIfLanguageChanged();

            if (!_isVisible) return;

            // Try resolving cached references slowly
            if (_kingdom == null && Time.frameCount % 120 == 0) _kingdom = FindFirstObjectByType<Kingdom>();
            if (_enemyManager == null && Time.frameCount % 120 == 60) _enemyManager = FindFirstObjectByType<EnemyManager>();

            
            if (Time.time > _nextCensusTime)
            {
                _nextCensusTime = Time.time + 0.3f;
                
                try 
                {
                    switch (_censusStep)
                    {
                        case 0: _archerCount = UnitCacheManager.Archers.Count; _strArcher = LocalizationService.Format("monitor.population.archer", _archerCount); break;
                        case 1: _workerCount = UnitCacheManager.Workers.Count; _strWorker = LocalizationService.Format("monitor.population.worker", _workerCount); break;
                        case 2: _peasantCount = UnitCacheManager.Peasants.Count; _strPeasant = LocalizationService.Format("monitor.population.peasant", _peasantCount); break;
                        case 3: _knightCount = UnitCacheManager.Knights.Count; _strKnight = LocalizationService.Format("monitor.population.knight", _knightCount); break;
                        case 4: _vagrantCount = UnitCacheManager.Beggars.Count; _strVagrant = LocalizationService.Format("monitor.population.vagrant", _vagrantCount); break;
                        case 5: _farmerCount = UnitCacheManager.Farmers.Count; _strFarmer = LocalizationService.Format("monitor.population.farmer", _farmerCount); break;
                        case 6: _pikemanCount = UnitCacheManager.Pikemen.Count; _strPikeman = LocalizationService.Format("monitor.population.pikeman", _pikemanCount); break;
                        case 7:
                            _enemyCount = UnitCacheManager.Enemies.Count;
                            _strGreed = LocalizationService.Format("monitor.greed", _enemyCount);
                            break;
                        case 8:
                            var player = Managers.Inst?.kingdom?.GetPlayer(0);
                            if (player != null && player.wallet != null)
                            {
                                _walletCoins = player.wallet.Coins;
                                _walletGems = player.wallet.Gems;
                                _strWallet = LocalizationService.Format("monitor.wallet", _walletCoins, _walletGems);
                            }
                            break;
                        case 9:
                            int day = 0;
                            if (Managers.Inst != null && Managers.Inst.director != null) day = Managers.Inst.director.CurrentIslandDays;
                            _currentDay = day;
                            _currentCycleKey = _kingdom != null && _kingdom.isDaytime ? "monitor.cycle.day" : "monitor.cycle.night";
                            string cycle = LocalizationService.Get(_currentCycleKey);
                            _strDay = LocalizationService.Format("monitor.day", day, cycle);
                            
                            if (_enemyManager != null) {
                                bool danger = _enemyManager.IsDangerous;
                                _isThreatDangerous = danger;
                                string safeHex = _stylePalette[(int)_currentStyle].safeHex;
                                string dangerHex = _stylePalette[(int)_currentStyle].dangerHex;
                                string status = danger
                                    ? "<color=" + dangerHex + ">" + LocalizationService.Get("monitor.threat.danger") + "</color>"
                                    : "<color=" + safeHex + ">" + LocalizationService.Get("monitor.threat.safe") + "</color>";
                                _strThreat = LocalizationService.Format("monitor.threat", status);
                            }
                            break;
                    }
                } 
                catch (Exception ex)
                {
                    KingdomEnhanced.Core.Plugin.Instance.LogSource.LogError($"[KingdomMonitor] Census Error: {ex.Message}");
                }

                _censusStep++;
                if (_censusStep > 9) _censusStep = 0;
            }
        }

        /// <summary>
        /// Detects a language change and rebuilds all cached strings on the first frame after it.
        /// </summary>
        private void RefreshLocalizedStringsIfLanguageChanged()
        {
            string currentLanguageCode = LocalizationService.CurrentLanguageCode;
            if (string.Equals(_localizedLanguageCode, currentLanguageCode, StringComparison.Ordinal))
                return;

            _localizedLanguageCode = currentLanguageCode;
            RefreshLocalizedStrings();
        }

        /// <summary>
        /// Rebuilds the monitor strings from cached values and state without changing the polling cadence.
        /// </summary>
        private void RefreshLocalizedStrings()
        {
            _strDay = LocalizationService.Format("monitor.day", _currentDay, LocalizationService.Get(_currentCycleKey));

            string safeHex = _stylePalette[(int)_currentStyle].safeHex;
            string dangerHex = _stylePalette[(int)_currentStyle].dangerHex;
            string status = _isThreatDangerous
                ? "<color=" + dangerHex + ">" + LocalizationService.Get("monitor.threat.danger") + "</color>"
                : "<color=" + safeHex + ">" + LocalizationService.Get("monitor.threat.safe") + "</color>";
            _strThreat = LocalizationService.Format("monitor.threat", status);
            _strGreed = LocalizationService.Format("monitor.greed", _enemyCount);
            _strWallet = LocalizationService.Format("monitor.wallet", _walletCoins, _walletGems);

            _strArcher = LocalizationService.Format("monitor.population.archer", _archerCount);
            _strWorker = LocalizationService.Format("monitor.population.worker", _workerCount);
            _strPeasant = LocalizationService.Format("monitor.population.peasant", _peasantCount);
            _strFarmer = LocalizationService.Format("monitor.population.farmer", _farmerCount);
            _strPikeman = LocalizationService.Format("monitor.population.pikeman", _pikemanCount);
            _strKnight = LocalizationService.Format("monitor.population.knight", _knightCount);
            _strVagrant = LocalizationService.Format("monitor.population.vagrant", _vagrantCount);
        }
    }
}
