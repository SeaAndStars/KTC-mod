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
    public class KingdomMonitor : MonoBehaviour
    {
#if IL2CPP
        public KingdomMonitor(IntPtr ptr) : base(ptr) { }
#endif
        private Kingdom _kingdom;
        private EnemyManager _enemyManager;
        
        public static KingdomMonitor Instance { get; private set; }
        
        private bool _isVisible = true;
        public bool IsVisible => _isVisible;
        private Rect _windowRect = new Rect(10, 10, 250, 350);
        private bool _isResizing = false;

        
        public enum MonitorStyle { Classic, Neon, Light, Ghost }
        private MonitorStyle _currentStyle = MonitorStyle.Classic;
        private readonly string[] _styleNames = { "Classic", "Neon", "Light", "Ghost" };
        private GUIStyle _styleBtn;
        private Texture2D _btnTex;
        private bool _stylesBuilt;

        
        private int _archerCount;
        private int _workerCount;
        private int _peasantCount;
        private int _knightCount;
        private int _enemyCount;
        private int _vagrantCount;
        private int _farmerCount;
        private int _pikemanCount;
        private float _nextCensusTime;
        private int _censusStep = 0;

        /// <summary>
        /// 上次用于生成监视器缓存文案的语言代码。
        /// </summary>
        private string _localizedLanguageCode;

        /// <summary>
        /// 上次轮询到的岛屿天数，用于语言切换时重建日数文案。
        /// </summary>
        private int _currentDay;

        /// <summary>
        /// 上次轮询到的昼夜周期资源键，用于语言切换时重建日数文案。
        /// </summary>
        private string _currentCycleKey = "monitor.cycle.unknown";

        /// <summary>
        /// 上次轮询到的威胁状态，用于语言切换时保留威胁文案的富文本颜色。
        /// </summary>
        private bool _isThreatDangerous;

        /// <summary>
        /// 上次轮询到的钱包金币数量，用于语言切换时重建钱包文案。
        /// </summary>
        private int _walletCoins;

        /// <summary>
        /// 上次轮询到的钱包宝石数量，用于语言切换时重建钱包文案。
        /// </summary>
        private int _walletGems;

        private string _strDay;
        private string _strThreat;
        private string _strGreed;
        private string _strWallet;
        
        private string _strArcher;
        private string _strWorker;
        private string _strPeasant;
        private string _strFarmer;
        private string _strPikeman;
        private string _strKnight;
        private string _strVagrant;

        private StyleColors[] _stylePalette;

        private struct StyleColors
        {
            public Color bgBottom;
            public Color bgTop;
            public Color header;
            public Color body;
            public Color footer;
            public Color btnBg;
            public Color btnText;
            public Color frameColor;
            public string safeHex;
            public string dangerHex;
            public float baseAlpha;
            public int frameThickness;

            public StyleColors(Color bb, Color bt, Color h, Color b, Color f, Color bbgn, Color btnT, Color fc, string s, string d, float a, int ft)
            {
                bgBottom = bb; bgTop = bt; header = h; body = b; footer = f;
                btnBg = bbgn; btnText = btnT; frameColor = fc;
                safeHex = s; dangerHex = d; baseAlpha = a; frameThickness = ft;
            }
        }

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

        private GUI.WindowFunction _drawWindowFunc;
        private GUIStyle _cachedWindowStyle;

        private void OnGUI()
        {
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

        
        public void Show() => _isVisible = true;
        public void Hide() => _isVisible = false;
        public void Toggle() => _isVisible = !_isVisible;
        public void NextStyle()
        {
            _currentStyle = (MonitorStyle)(((int)_currentStyle + 1) % _stylePalette.Length);
            _stylesBuilt = false;
            _cachedWindowStyle = null;
        }

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
        /// 检测当前语言是否变更，并在变更后的首帧重建全部缓存化文案。
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
        /// 使用既有数值和状态缓存重建监视器文案，不改变轮询计算和刷新节奏。
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
