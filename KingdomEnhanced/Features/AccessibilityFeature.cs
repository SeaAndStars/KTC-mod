using System;
using UnityEngine;
using KingdomEnhanced.Core;
using KingdomEnhanced.UI;
using KingdomEnhanced.Systems;
using KingdomEnhanced.Utils; 
using KingdomEnhanced.Systems.Accessibility; 
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if IL2CPP
using KingdomEnhanced.Shared.Attributes;
#endif

namespace KingdomEnhanced.Features
{
#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    /// <summary>
    /// Main accessibility driver: hover announcements, radar, castle proximity, and debug zones.
    /// </summary>
    public class AccessibilityFeature : MonoBehaviour
    {
#if IL2CPP
        public AccessibilityFeature(IntPtr ptr) : base(ptr) { }
#endif
        // Cached reference to the player
        private Player _player;
        // The payable last hovered, used to detect hover changes
        private MonoBehaviour _lastPayable = null;

        // Radar helper created once the player is found
        private RadarSystem _radarSystem; 

        /// <summary>Resets the base camp announcement flag when the scene starts.</summary>
        void Start()
        {
            _baseCampAnnounced = false;
        }

        // Whether the player was inside the castle on the last check
        private bool _wasInCastle = false;
        
        // Guards the one-time base camp direction announcement
        private bool _baseCampAnnounced = false;
        // Whether the player was inside a vagrant camp on the last check
        private bool _wasInVillage = false; 
        
        // Last announcement timestamp, used for spam throttling
        private float _spamTimer = 0f;
        // Last announced message, used to detect hover changes
        private string _lastSpokenMsg = "";
        // Last announced canonical name
        private string _lastName = "";
        // Last announced price
        private int _lastPrice = -1;


        // Timestamp of the last closest-payable scan
        private float _lastPayableCheckTime = 0f;
        // Minimum interval between closest-payable scans
        private const float PAYABLE_CHECK_INTERVAL = 0.15f; 

        // Matches names ending in a digit (upgrade candidates)
        private static readonly Regex _endsWithDigitRegex = new Regex(@"\d$", RegexOptions.Compiled);
        // Matches names ending in a capital letter (upgrade candidates)
        private static readonly Regex _endsWithUpperRegex = new Regex(@"[A-Z]$", RegexOptions.Compiled);

        /// <summary>Drains the TTS queue and handles input, hover, and proximity announcements each frame.</summary>
        void Update()
        {
            // TTS queue must always drain, even if Accessibility is off
            TTSManager.Update();

            if (!ModMenu.EnableAccessibility) return;

            if (_player == null)
            {
                _player = FindFirstObjectByType<Player>();
                if (_player != null) _radarSystem = new RadarSystem(_player);
            }
            if (_player == null) return;

            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    AccessibilityReportHandler.ReportDetailedInfo(_player);
                else
                    _radarSystem.Pulse(); 
            }
            if (Input.GetKeyDown(KeyCode.F6)) AccessibilityReportHandler.CheckCompassAndSafety(_player);
            if (Input.GetKeyDown(KeyCode.F7)) AccessibilityReportHandler.ReportWallet(_player);
            if (Input.GetKeyDown(KeyCode.F8)) AccessibilityReportHandler.ReportWorld();
            if (Input.GetKeyDown(KeyCode.F9)) AccessibilityReportHandler.ReportMount(_player);
            if (Input.GetKeyDown(KeyCode.F10)) AccessibilityReportHandler.ReportCompanions();
            
            if (Input.GetKeyDown(KeyCode.F11))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    TTSManager.ReadPreviousMessage();
                else
                    TTSManager.RepeatLast();
            }

            if (Input.GetKeyDown(KeyCode.F3) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                AccessibilityReportHandler.DumpPayableInfo(_player);
            }

            HandleHover();
            HandleCastleProximity();
            HandleBaseCampOrientation();
        }

        void HandleHover()
        {
            var current = _player.selectedPayable as MonoBehaviour; 
            if (current == null) current = GetClosestPayable();

            if (current != null)
            {
                var payable = current.GetComponent<Payable>();
                if (payable == null)
                {
                    // Missing Payable means no target; prevent stale _lastPayable from re-announcing next frame
                    ResetHoverState();
                    return;
                }

                
                if (current.GetComponent<Player>() != null || current.gameObject == _player.gameObject) return;
                
                if (_player.steed != null && current.gameObject == _player.steed.gameObject) return;

                try
                {
                    string rawName = PayableNameResolver.GetCanonicalName(current.name);
                    string displayName = PayableNameResolver.GetLocalizedDisplayName(current.name);
                    
                    
                    int price = payable.Price;
                    bool isGemCurrency = payable.Currency == CurrencyType.Gems;
                    string currency = LocalizationService.Get(isGemCurrency ? "accessibility.currency.gems" : "accessibility.currency.coins");



                    
                    if (string.IsNullOrEmpty(rawName))
                    {
                        rawName = current.name.Replace("(Clone)", "").Trim();
                        displayName = rawName;
                    }

                    
                    bool isBoat = current.GetComponent<Boat>() != null || current.name.ToLower().Contains("boat");
                    bool isShipwreck = !isBoat && current.name.ToLower().Contains("wreck");
                    if (isBoat)
                    {
                        displayName = LocalizationService.Get("payable.name.boat");
                    }
                    else if (isShipwreck)
                    {
                        displayName = LocalizationService.Get("accessibility.name.shipwreck");
                    }
                    else if (current.name.ToLower().Contains("wharf"))
                    {
                        displayName = LocalizationService.Get("accessibility.name.wharf");
                    }

                    
                    string techWarning = "";
                    if (payable.IsLocked(_player, out LockIndicator.LockReason reason))
                    {
                        
                        switch (reason)
                        {
                            case LockIndicator.LockReason.StoneTechRequired: techWarning = LocalizationService.Get("accessibility.lock.stone_tech_required"); break;
                            case LockIndicator.LockReason.IronTechRequired: techWarning = LocalizationService.Get("accessibility.lock.iron_tech_required"); break;
                            case LockIndicator.LockReason.HermitLocked: techWarning = LocalizationService.Get("accessibility.lock.hermit_locked"); break;
                            case LockIndicator.LockReason.NoUpgrade: techWarning = LocalizationService.Get("accessibility.lock.fully_upgraded"); break;
                            case LockIndicator.LockReason.Base: techWarning = LocalizationService.Get("accessibility.lock.base_upgrade_required"); break;
                            default: techWarning = LocalizationService.Get("accessibility.lock.locked"); break;
                        }
                        if (reason == LockIndicator.LockReason.NotLocked) techWarning = ""; 
                    }

                    
                    bool isProtecting = false;
                    if (rawName.Contains("Tree") && (isProtecting = IsTreeProtectingVillage(current.transform.position.x)))
                    {
                        techWarning = LocalizationService.Get("accessibility.lock.destroys_village");
                    }

                    
                    string actionKey = "accessibility.action.build";
                    
                    
                    if (isBoat || rawName.Contains("Boat") || rawName.Contains("Ship"))
                    {
                         if (price <= 3) actionKey = "accessibility.action.add_parts";
                         else if (price >= 10) actionKey = "accessibility.action.sail";
                         else actionKey = "accessibility.action.repair_hull";
                         
                         if (isShipwreck || rawName.Contains("Wreck") || rawName.Contains("Ruin")) actionKey = "accessibility.action.repair_hull";
                    }
                    
                    if (rawName.Contains("Statue") || rawName.Contains("Idol"))
                    {
                        actionKey = isGemCurrency ? "accessibility.action.pay" : "accessibility.action.activate";
                    }
                    
                    if (rawName.Contains("Bank") || rawName.Contains("Chest")) actionKey = "accessibility.action.deposit";
                    else if (rawName.Contains("Portal") || rawName.Contains("Border")) actionKey = "accessibility.action.destroy_portal";
                    else if (rawName.Contains("Beggar") || rawName.Contains("Citizen") || rawName.Contains("Hermit")) actionKey = "accessibility.action.hire";
                    else if (rawName.Contains("Shop") || rawName.Contains("Merchant")) actionKey = rawName.Contains("Merchant") ? "accessibility.action.invest" : "accessibility.action.buy";
                    else if (rawName.Contains("Teleporter")) actionKey = "accessibility.action.teleport";
                    else if (rawName.Contains("Bell")) actionKey = "accessibility.action.call";
                    else if (rawName.Contains("Gem Guard") || rawName.Contains("GemKeeper")) actionKey = "accessibility.action.withdraw";
                    
                    else if (rawName.Contains("Tree") && !rawName.Contains("Close")) actionKey = "accessibility.action.chop";
                    else if (rawName.Contains("Mount") || rawName.Contains("Chimera") || current.name.Contains("Steed") || current.name.Contains("Horse")) actionKey = "accessibility.action.switch";
                    else if (rawName.Contains("Banner")) actionKey = "accessibility.action.expedition";
                    
                    
                    if (actionKey == "accessibility.action.build")
                    {
                        
                        
                        if (_endsWithDigitRegex.IsMatch(rawName) || _endsWithUpperRegex.IsMatch(rawName)) actionKey = "accessibility.action.upgrade";
                        
                        var wall = current.GetComponent<Wall>();
                        if (wall != null && wall.level > 0) actionKey = "accessibility.action.upgrade_wall";
                        
                        var castle = current.GetComponent<Castle>();
                        
                        if (castle != null)
                        {
                             actionKey = castle.level == 0 ? "accessibility.action.build" : "accessibility.action.upgrade";
                        }
                        
                        var farm = current.GetComponent<Farmhouse>();
                        if (farm != null && price >= 3) actionKey = "accessibility.action.upgrade_farm";
                    }

                    
                    string message = LocalizationService.Format("accessibility.hover.name_only", displayName);
                    
                    if (!string.IsNullOrEmpty(techWarning))
                    {
                        message = LocalizationService.Format("accessibility.hover.with_warning", displayName, techWarning);
                    }
                    else
                    {
                        
                        if (price > 0 || actionKey == "accessibility.action.withdraw" || actionKey == "accessibility.action.deposit")
                            message = LocalizationService.Format("accessibility.hover.with_price", displayName, price, currency, LocalizationService.Get(actionKey));
                        else
                            message = LocalizationService.Format("accessibility.hover.with_action", displayName, LocalizationService.Get(actionKey));
                    }

                    
                    bool changed = (current != _lastPayable || message != _lastSpokenMsg);
                    if (changed)
                    {
                        ModMenu.Speak(message, interrupt: false);
                        _lastSpokenMsg = message;
                        _spamTimer = Time.time;
                    }

                    _lastPayable = current;
                    _lastName = rawName;
                    _lastPrice = price;
                }
                catch (Exception ex)
                {
                    // Any exception resets the hover state to prevent repeated announcements from the exception path
                    ResetHoverState();
                    Debug.LogWarning($"[Accessibility] HandleHover error: {ex.Message}");
                }
            }
            else
            {
                ResetHoverState();
            }
        }

        /// <summary>
        /// Clears the hover announcement state to prevent stale state from re-announcing next frame.
        /// </summary>
        private void ResetHoverState()
        {
            if (_lastPayable == null && string.IsNullOrEmpty(_lastSpokenMsg)) return;
            _lastPayable = null;
            _lastSpokenMsg = "";
            _lastName = "";
            _lastPrice = -1;
        }

        /// <summary>Returns the nearest payable within range, throttled to every 0.3 seconds.</summary>
        MonoBehaviour GetClosestPayable()
        {
            if (Time.time < _lastPayableCheckTime + 0.3f) return _lastPayable as MonoBehaviour; 
            _lastPayableCheckTime = Time.time;

            if (Managers.Inst == null || Managers.Inst.payables == null) return null;
            
            float searchRange = 18.0f; 
            float playerX = _player.transform.position.x;
            
            MonoBehaviour closest = null;
            float closestDist = float.MaxValue;

            var allPayables = Managers.Inst.payables.AllPayables;
            int count = allPayables.Length;
            for (int i = 0; i < count; i++)
            {
                var p = allPayables[i];
                if (p == null) continue;
                
                float dist = Mathf.Abs(p.transform.position.x - playerX);
                if (dist < searchRange && dist < closestDist)
                {
                    var mb = p as MonoBehaviour;
                    if (mb == null || !mb.gameObject.activeInHierarchy) continue;
                    if (_player.steed != null && mb.gameObject == _player.steed.gameObject) continue;

                    closest = mb;
                    closestDist = dist;
                }
            }
            return closest;
        }

        /// <summary>Returns the castle nearest to the player's X position.</summary>
        Castle FindClosestCastle()
        {
            Castle closest = null;
            float closestDist = float.MaxValue;
            float playerX = _player.transform.position.x;

            foreach (var c in UnitCacheManager.Castles)
            {
                if (c == null) continue;
                float dist = Mathf.Abs(c.transform.position.x - playerX);
                if (dist < closestDist)
                {
                    closest = c;
                    closestDist = dist;
                }
            }
            return closest;
        }

        /// <summary>Announces the base camp direction once shortly after scene load.</summary>
        void HandleBaseCampOrientation()
        {
            if (_baseCampAnnounced) return;
            
            if (Time.timeSinceLevelLoad < 3.0f) return;

            var castle = FindClosestCastle();
            if (castle != null)
            {
                float playerX = _player.transform.position.x;
                float castleX = castle.transform.position.x;
                string direction = LocalizationService.Get(castleX > playerX ? "accessibility.camp.direction.right" : "accessibility.camp.direction.left");
                ModMenu.Speak(LocalizationService.Format("accessibility.base_camp.direction", direction), interrupt: false);
                _baseCampAnnounced = true;
            }
        }

        // Timer controlling how often the trigger zones are refreshed
        private float _zoneUpdateTimer = 0f;
        // Leftmost and rightmost wall X positions defining the castle zone
        private float _castleMinX = 0f;
        private float _castleMaxX = 0f;
        // X intervals between the trees bordering each vagrant camp
        private List<Vector2> _campIntervals = new List<Vector2>();
        
        // Debug visualization box: rect, color, and label
        private struct TriggerZone {
            public Rect Box;
            public Color Color;
            public string Label;
        }
        // Debug zones drawn in OnGUI when DebugZones is enabled
        private List<TriggerZone> _debugZones = new List<TriggerZone>();
        
        // Cooldown between castle/camp enter-leave announcements
        private float _announcerCooldown = 0f;

        /// <summary>Rebuilds the castle and camp trigger zones plus their debug boxes.</summary>
        void UpdateZones()
        {
            if (_player == null) return;
            
            _debugZones.Clear();
            float y = _player.transform.position.y;
            float h = 4.0f; 
            float wBox = 0.5f;

            float minWallX = float.MaxValue, maxWallX = float.MinValue;
            
            if (UnitCacheManager.Walls.Count > 0)
            {
                foreach (var w in UnitCacheManager.Walls)
                {
                    if (w == null) continue;
                    float wx = w.transform.position.x;
                    if (wx < minWallX) minWallX = wx;
                    if (wx > maxWallX) maxWallX = wx;
                }
            }
            else
            {
                var k = Managers.Inst?.kingdom;
                if (k != null) { minWallX = k.transform.position.x - 20f; maxWallX = k.transform.position.x + 20f; }
            }

            _castleMinX = minWallX;
            _castleMaxX = maxWallX;

            if (minWallX != float.MaxValue)
            {
                _debugZones.Add(new TriggerZone { Box = new Rect(minWallX, y - 2f, 0.1f, h), Color = Color.cyan, Label = LocalizationService.Get("accessibility.zone.last_wall") });
                _debugZones.Add(new TriggerZone { Box = new Rect(maxWallX, y - 2f, 0.1f, h), Color = Color.cyan, Label = LocalizationService.Get("accessibility.zone.last_wall") });
                
                _debugZones.Add(new TriggerZone { Box = new Rect(minWallX - wBox, y - 1f, wBox, h - 1f), Color = Color.red, Label = LocalizationService.Get("accessibility.zone.castle_trigger") });
                _debugZones.Add(new TriggerZone { Box = new Rect(maxWallX,        y - 1f, wBox, h - 1f), Color = Color.red, Label = LocalizationService.Get("accessibility.zone.castle_trigger") });
            }

            _campIntervals.Clear();

            if (UnitCacheManager.BeggarCamps.Count > 0)
            {
                foreach (var camp in UnitCacheManager.BeggarCamps)
                {
                    if (camp == null) continue;
                    
                    float cx = camp.transform.position.x;
                    float treeL1 = cx - 15f; 
                    float treeR1 = cx + 15f;
                    float treeL_N = cx - 30f;
                    float treeR_N = cx + 30f;

                    if (Managers.Inst != null && Managers.Inst.payables != null)
                    {
                        float minL = float.MaxValue, minR = float.MaxValue;
                        float maxL = float.MinValue, maxR = float.MinValue;

                        var allPayables = Managers.Inst.payables.AllPayables;
                        int count = allPayables.Length;
                        for (int j = 0; j < count; j++)
                        {
                            var p = allPayables[j];
                            if (p == null) continue;
                            var mb = p as MonoBehaviour;
                            if (mb == null || !mb.name.Contains("Tree") || mb.name.Contains("Close")) continue;
                            
                            float tx = mb.transform.position.x;

                            if (tx < cx && (cx - tx) < minL) { minL = cx - tx; treeL1 = tx; }
                            if (tx > cx && (tx - cx) < minR) { minR = tx - cx; treeR1 = tx; }
                            
                            if (tx < cx && (cx - tx) < 100f && (cx - tx) > maxL) { maxL = cx - tx; treeL_N = tx; }
                            if (tx > cx && (tx - cx) < 100f && (tx - cx) > maxR) { maxR = tx - cx; treeR_N = tx; }
                        }
                    }

                    _campIntervals.Add(new Vector2(treeL1, treeR1));

                    _debugZones.Add(new TriggerZone { Box = new Rect(treeL1, y - 2f, 0.1f, h), Color = Color.cyan, Label = LocalizationService.Get("accessibility.zone.inner_tree_line") });
                    _debugZones.Add(new TriggerZone { Box = new Rect(treeR1, y - 2f, 0.1f, h), Color = Color.cyan, Label = LocalizationService.Get("accessibility.zone.inner_tree_line") });

                    _debugZones.Add(new TriggerZone { Box = new Rect(treeL1 - wBox, y - 1f, wBox, h - 1f), Color = Color.red, Label = LocalizationService.Get("accessibility.zone.camp_trigger") });
                    _debugZones.Add(new TriggerZone { Box = new Rect(treeR1,        y - 1f, wBox, h - 1f), Color = Color.red, Label = LocalizationService.Get("accessibility.zone.camp_trigger") });

                    _debugZones.Add(new TriggerZone { Box = new Rect(treeL_N, y - 0.5f, Mathf.Max(0.1f, treeL1 - treeL_N - wBox), h - 2f), Color = new Color(0.3f, 0.3f, 0.3f, 0.6f), Label = LocalizationService.Get("accessibility.zone.exclusion") });
                    _debugZones.Add(new TriggerZone { Box = new Rect(treeR1 + wBox, y - 0.5f, Mathf.Max(0.1f, treeR_N - treeR1 - wBox), h - 2f), Color = new Color(0.3f, 0.3f, 0.3f, 0.6f), Label = LocalizationService.Get("accessibility.zone.exclusion") });
                }
            }
        }

        /// <summary>Announces entering or leaving the castle and vagrant camps.</summary>
        void HandleCastleProximity()
        {
            if (_announcerCooldown > 0f) _announcerCooldown -= Time.deltaTime;

            _zoneUpdateTimer -= Time.deltaTime;
            if (_zoneUpdateTimer <= 0f)
            {
                UpdateZones();
                _zoneUpdateTimer = 10.0f; // Increased to 10 seconds to drastically reduce stutters
            }

            if (_player == null) return;
            float playerX = _player.transform.position.x;
            
            if (ModMenu.EnableCastleAnnouncer)
            {
                bool insideCastle = playerX >= _castleMinX && playerX <= _castleMaxX;
                if (insideCastle != _wasInCastle)
                {
                    _wasInCastle = insideCastle;
                    if (_announcerCooldown <= 0f)
                    {
                        ModMenu.Speak(LocalizationService.Get(insideCastle ? "accessibility.castle.entering" : "accessibility.castle.leaving"));
                        _announcerCooldown = 0.5f;
                    }
                }
            }

            bool insideAnyCamp = false;
            foreach (var interval in _campIntervals)
            {
                if (playerX >= interval.x && playerX <= interval.y)
                {
                    insideAnyCamp = true;
                    break;
                }
            }

            if (insideAnyCamp != _wasInVillage)
            {
                _wasInVillage = insideAnyCamp;
                if (_announcerCooldown <= 0f)
                {
                    ModMenu.Speak(LocalizationService.Get(insideAnyCamp ? "accessibility.camp.entering" : "accessibility.camp.leaving"));
                    _announcerCooldown = 0.5f;
                }
            }
        }

        // Cached label style for debug zone boxes
        private GUIStyle _debugLabelStyle;

        /// <summary>Draws the debug zone boxes when the DebugZones option is enabled.</summary>
        void OnGUI()
        {
            if (!ModMenu.DebugZones) return;

            Camera cam = Camera.main;
            if (cam == null)
                cam = UnityEngine.Object.FindFirstObjectByType<Camera>();

            if (cam == null) return;

            if (_debugLabelStyle == null)
            {
                _debugLabelStyle = new GUIStyle(GUI.skin.label) { 
                    fontSize = 11, alignment = TextAnchor.MiddleCenter, 
                    normal = { textColor = Color.white }, fontStyle = FontStyle.Bold 
                };
            }

            foreach (var zone in _debugZones)
            {
                
                Vector3 screenPointBL = cam.WorldToScreenPoint(new Vector3(zone.Box.xMin, zone.Box.yMin, 0));
                Vector3 screenPointTR = cam.WorldToScreenPoint(new Vector3(zone.Box.xMax, zone.Box.yMax, 0));
                
                
                float width = Mathf.Abs(screenPointTR.x - screenPointBL.x);
                float height = Mathf.Abs(screenPointTR.y - screenPointBL.y);
                
                Rect screenRect = new Rect(Mathf.Min(screenPointBL.x, screenPointTR.x), Screen.height - Mathf.Max(screenPointBL.y, screenPointTR.y), width, height);
                
                
                Color prevC = GUI.color;
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = zone.Color;
                GUI.color = Color.white;
                GUI.Box(screenRect, GUIContent.none, GUI.skin.box);
                
                
                GUI.color = Color.black;
                GUI.Label(new Rect(screenRect.x - 49, screenRect.yMax + 1, width + 100, 20), zone.Label, _debugLabelStyle);
                
                
                GUI.color = zone.Color;
                GUI.Label(new Rect(screenRect.x - 50, screenRect.yMax, width + 100, 20), zone.Label, _debugLabelStyle);
                
                GUI.backgroundColor = prevBg;
                GUI.color = prevC;
            }
        }

        /// <summary>Returns the vagrant camp nearest to the player's X position.</summary>
        private BeggarCamp FindClosestCamp()
        {
            BeggarCamp closest = null;
            float minDst = float.MaxValue;
            foreach (var c in UnitCacheManager.BeggarCamps)
            {
                if (c == null) continue;
                float d = Mathf.Abs(c.transform.position.x - _player.transform.position.x);
                if (d < minDst) { minDst = d; closest = c; }
            }
            return closest;
        }

        /// <summary>Returns true when the tree at the given X position borders a camp and protects the village.</summary>
        private bool IsTreeProtectingVillage(float treeX)
        {
            
            
            foreach (var interval in _campIntervals)
            {
                
                if (Mathf.Abs(treeX - interval.x) < 0.1f || Mathf.Abs(treeX - interval.y) < 0.1f)
                {
                    return true; 
                }
            }
            return false;
        }


        /// <summary>Clears the cached hover state when the component is destroyed.</summary>
        void OnDestroy()
        {
            _lastPayable = null;
            _lastSpokenMsg = "";
        }
    }
}
