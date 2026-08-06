using BepInEx.Configuration;
using UnityEngine;

namespace KingdomEnhanced.Core
{
    /// <summary>
    /// Holds all configurable settings of the mod and binds them to the BepInEx configuration file.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// The active BepInEx configuration file.
        /// </summary>
        public static ConfigFile Config;

        /// <summary>
        /// Stores the currently active UI language code.
        /// </summary>
        public static ConfigEntry<string> Language;

        
        /// <summary>
        /// Use the new Beta UI style
        /// </summary>
        public static ConfigEntry<bool> UseBetaUI;
        /// <summary>
        /// Show the energy/stamina bar
        /// </summary>
        public static ConfigEntry<bool> ShowStaminaBar;
        /// <summary>
        /// Show the day/night and coins HUD
        /// </summary>
        public static ConfigEntry<bool> DisplayTimes;
        /// <summary>
        /// Show the greed counter HUD
        /// </summary>
        public static ConfigEntry<bool> ShowGreedCounter;
        /// <summary>
        /// Use a 12-hour clock (AM/PM) instead of a 24-hour one in the HUD
        /// </summary>
        public static ConfigEntry<bool> Use12HourClock;
        
        
        /// <summary>
        /// Enable the hover tracking logic
        /// </summary>
        public static ConfigEntry<bool> EnableAccessibility;
        /// <summary>
        /// Enable text-to-speech output
        /// </summary>
        public static ConfigEntry<bool> EnableTTS;
        /// <summary>
        /// Queue messages instead of interrupting current speech
        /// </summary>
        public static ConfigEntry<bool> NarratorQueueMode;
        /// <summary>
        /// Simplify object names for TTS
        /// </summary>
        public static ConfigEntry<bool> SimplifyNames;
        /// <summary>
        /// Announce entering/leaving the castle
        /// </summary>
        public static ConfigEntry<bool> EnableCastleAnnouncer;
        /// <summary>
        /// Show debug boxes for announcer zones
        /// </summary>
        public static ConfigEntry<bool> DebugZones;

        
        /// <summary>
        /// Unlock the cheat menu
        /// </summary>
        public static ConfigEntry<bool> CheatsUnlocked;
        /// <summary>
        /// Travel speed multiplier
        /// </summary>
        public static ConfigEntry<float> SpeedMultiplier;
        /// <summary>
        /// Infinite mount stamina
        /// </summary>
        public static ConfigEntry<bool> InfiniteStamina;
        /// <summary>
        /// Walls repair instantly
        /// </summary>
        public static ConfigEntry<bool> InvincibleWalls;
        /// <summary>
        /// Disable item ability cooldowns
        /// </summary>
        public static ConfigEntry<bool> NoToolCooldowns;
        /// <summary>
        /// Artemis Bow arrows fired per cast
        /// </summary>
        public static ConfigEntry<float> ArtemisArrowCount;
        /// <summary>
        /// Artemis Bow ability range multiplier
        /// </summary>
        public static ConfigEntry<float> ArtemisRangeMult;
        /// <summary>
        /// Artemis Bow arrow damage multiplier
        /// </summary>
        public static ConfigEntry<float> ArtemisArrowDamageMult;
        /// <summary>
        /// Coin income multiplier
        /// </summary>
        public static ConfigEntry<float> CoinIncomeMult;
        /// <summary>
        /// Bag drop multiplier
        /// </summary>
        public static ConfigEntry<float> BagDropMult;
        /// <summary>
        /// Number of units to spawn
        /// </summary>
        public static ConfigEntry<float> SpawnUnitCount;
        
        
        /// <summary>
        /// Instant construction
        /// </summary>
        public static ConfigEntry<bool> HyperBuilders;
        /// <summary>
        /// Expand vagrant camps
        /// </summary>
        public static ConfigEntry<bool> LargerCamps;
        /// <summary>
        /// Elite knight buffs
        /// </summary>
        public static ConfigEntry<bool> BetterKnight;
        /// <summary>
        /// Rapid housing
        /// </summary>
        public static ConfigEntry<bool> BetterCitizenHouses;
        /// <summary>
        /// Lock the season to summer
        /// </summary>
        public static ConfigEntry<bool> LockSummer;
        /// <summary>
        /// Force clear weather
        /// </summary>
        public static ConfigEntry<bool> ClearWeather;
        /// <summary>
        /// Disable blood moon events
        /// </summary>
        public static ConfigEntry<bool> NoBloodMoons;
        /// <summary>
        /// Coins don't sink in water
        /// </summary>
        public static ConfigEntry<bool> CoinsStayDry;
        /// <summary>
        /// Archer fire rate boost x2
        /// </summary>
        public static ConfigEntry<bool> ArcherFireBoost;
        /// <summary>
        /// Berserker rage mode (earlier)
        /// </summary>
        public static ConfigEntry<bool> BerserkerRage;
        /// <summary>
        /// Ninja speed boost x2
        /// </summary>
        public static ConfigEntry<bool> NinjaSpeedBoost;
        /// <summary>
        /// Recruit cap override (0 = default)
        /// </summary>
        public static ConfigEntry<int> RecruitCapOverride;
        /// <summary>
        /// Tree regrowth speed multiplier
        /// </summary>
        public static ConfigEntry<float> TreeRegrowthMult;
        /// <summary>
        /// Animal spawn boost
        /// </summary>
        public static ConfigEntry<bool> AnimalSpawnBoost;
        /// <summary>
        /// Instant day skip
        /// </summary>
        public static ConfigEntry<bool> InstantDaySkip;
        /// <summary>
        /// Farm output boost x2
        /// </summary>
        public static ConfigEntry<bool> FarmOutputBoost;
        /// <summary>
        /// Tower fire rate boost x2
        /// </summary>
        public static ConfigEntry<bool> TowerFireBoost;
        /// <summary>
        /// Ballista power boost x2
        /// </summary>
        public static ConfigEntry<bool> BallistaBoost;
        /// <summary>
        /// Ballista reload speed multiplier
        /// </summary>
        public static ConfigEntry<float> BallistaReloadMult;
        /// <summary>
        /// Ballista projectile speed multiplier
        /// </summary>
        public static ConfigEntry<float> BallistaFlightMult;
        /// <summary>
        /// Catapult reload speed boost x3
        /// </summary>
        public static ConfigEntry<bool> CatapultBoost;
        /// <summary>
        /// Catapult reload speed multiplier
        /// </summary>
        public static ConfigEntry<float> CatapultReloadMult;
        /// <summary>
        /// Catapult projectile speed multiplier
        /// </summary>
        public static ConfigEntry<float> CatapultFlightMult;
        /// <summary>
        /// Instant castle upgrade completion
        /// </summary>
        public static ConfigEntry<bool> InstantCastle;

        /// <summary>
        /// Builder movement speed multiplier
        /// </summary>
        public static ConfigEntry<float> BuilderSpeedMult;
        /// <summary>
        /// Builder efficiency multiplier (lower is faster)
        /// </summary>
        public static ConfigEntry<float> BuilderWorkMult;

        
        /// <summary>
        /// Enable player scaling
        /// </summary>
        public static ConfigEntry<bool> EnableSizeHack;
        /// <summary>
        /// Player scale multiplier
        /// </summary>
        public static ConfigEntry<float> TargetSize;

        
        /// <summary>
        /// Steed run speed scale
        /// </summary>
        public static ConfigEntry<float> SteedSpeedMult;
        /// <summary>
        /// Steed charge damage x2
        /// </summary>
        public static ConfigEntry<bool> ChargeDmgBoost;
        /// <summary>
        /// Steed buff aura duration multiplier
        /// </summary>
        public static ConfigEntry<float> BuffAuraDuration;

        
        /// <summary>
        /// Wave size multiplier
        /// </summary>
        public static ConfigEntry<float> WaveSizeMult;
        /// <summary>
        /// Enemy speed multiplier
        /// </summary>
        public static ConfigEntry<float> EnemySpeedMult;
        /// <summary>
        /// Portal spawn rate multiplier
        /// </summary>
        public static ConfigEntry<float> PortalSpawnRate;
        /// <summary>
        /// Disable crown stealing
        /// </summary>
        public static ConfigEntry<bool> NoCrownStealing;
        /// <summary>
        /// Greed Queen HP scale
        /// </summary>
        public static ConfigEntry<float> GreedQueenHPScale;
        /// <summary>
        /// Director threat ramp multiplier
        /// </summary>
        public static ConfigEntry<float> DirectorThreatMult;


        /// <summary>Binds all configuration entries to the given config file.</summary>
        public static void Init(ConfigFile config)
        {
            Config = config;

            Language          = Config.Bind("0. General", "Language", "en-US", "Current UI language code");

            
            UseBetaUI         = Config.Bind("1. Visuals", "UseBetaUI", false, "Use the new Beta UI style");
            ShowStaminaBar    = Config.Bind("1. Visuals", "ShowStaminaBar", false, "Show the energy/stamina bar");
            DisplayTimes      = Config.Bind("1. Visuals", "DisplayTimes", false, "Show day/night and coins HUD");
            ShowGreedCounter  = Config.Bind("1. Visuals", "ShowGreedCounter", false, "Show greed counter HUD");
            Use12HourClock    = Config.Bind("1. Visuals", "Use12HourClock", false, "Use 12-hour clock (AM/PM) instead of 24-hour in the HUD");

            
            EnableAccessibility   = Config.Bind("2. Accessibility", "EnableAccessibility", true, "Enable logic for hover tracking");
            EnableTTS             = Config.Bind("2. Accessibility", "EnableTTS", true, "Enable text-to-speech output");
            NarratorQueueMode     = Config.Bind("2. Accessibility", "NarratorQueueMode", false, "Queue messages instead of interrupting");
            SimplifyNames         = Config.Bind("2. Accessibility", "SimplifyNames", true, "Simplify object names for TTS");
            EnableCastleAnnouncer = Config.Bind("2. Accessibility", "CastleAnnouncer", false, "Announce entering/leaving castle");
            DebugZones            = Config.Bind("2. Accessibility", "DebugZones", false, "Show visual debug boxes for announcer zones");

            
            CheatsUnlocked    = Config.Bind("3. Cheats", "CheatsUnlocked", false, "Unlock the cheat menu");
            SpeedMultiplier   = Config.Bind("3. Cheats", "SpeedMultiplier", 1.0f, "Travel speed multiplier");
            InfiniteStamina   = Config.Bind("3. Cheats", "InfiniteStamina", false, "Infinite mount stamina");
            InvincibleWalls   = Config.Bind("3. Cheats", "InvincibleWalls", false, "Walls repair instantly");
            NoToolCooldowns      = Config.Bind("3. Cheats", "NoToolCooldowns", false, "Disable item ability cooldowns");
            ArtemisArrowCount    = Config.Bind("3. Cheats", "ArtemisArrowCount", 6f, "Artemis Bow arrows fired per cast");
            ArtemisRangeMult     = Config.Bind("3. Cheats", "ArtemisRangeMult", 1.0f, "Artemis Bow ability range multiplier");
            ArtemisArrowDamageMult = Config.Bind("3. Cheats", "ArtemisArrowDamageMult", 1.0f, "Artemis Bow arrow damage multiplier");
            CoinIncomeMult    = Config.Bind("3. Cheats", "CoinIncomeMult", 1.0f, "Coin income multiplier (0.25-4.0)");
            BagDropMult       = Config.Bind("3. Cheats", "BagDropMult", 1.0f, "Bag drop multiplier (1-10)");
            SpawnUnitCount    = Config.Bind("3. Cheats", "SpawnUnitCount", 1.0f, "Number of units to spawn");

            
            HyperBuilders     = Config.Bind("4. Development", "HyperBuilders", false, "Instant construction");
            LargerCamps       = Config.Bind("4. Development", "LargerCamps", false, "Expand vagrant camps");
            BetterKnight      = Config.Bind("4. Development", "BetterKnight", false, "Elite knights buffs");
            BetterCitizenHouses = Config.Bind("4. Development", "BetterCitizenHouses", false, "Rapid housing");
            LockSummer        = Config.Bind("4. Development", "LockSummer", false, "Lock season to summer");
            ClearWeather      = Config.Bind("4. Development", "ClearWeather", false, "Force clear weather");
            NoBloodMoons      = Config.Bind("4. Development", "NoBloodMoons", false, "Disable blood moon events");
            CoinsStayDry      = Config.Bind("4. Development", "CoinsStayDry", false, "Coins don't sink in water");
            
            ArcherFireBoost    = Config.Bind("4. Development", "ArcherFireBoost", false, "Archer fire rate boost x2");
            BerserkerRage      = Config.Bind("4. Development", "BerserkerRage", false, "Berserker rage mode (earlier)");
            NinjaSpeedBoost    = Config.Bind("4. Development", "NinjaSpeedBoost", false, "Ninja speed boost x2");
            RecruitCapOverride = Config.Bind("4. Development", "RecruitCapOverride", 0, "Recruit cap override (0 = default)");
            TreeRegrowthMult   = Config.Bind("4. Development", "TreeRegrowthMult", 1.0f, "Tree regrowth speed multiplier");
            AnimalSpawnBoost   = Config.Bind("4. Development", "AnimalSpawnBoost", false, "Animal spawn boost");
            InstantDaySkip     = Config.Bind("4. Development", "InstantDaySkip", false, "Instant day skip");
            FarmOutputBoost    = Config.Bind("4. Development", "FarmOutputBoost", false, "Farm output boost x2");
            TowerFireBoost     = Config.Bind("4. Development", "TowerFireBoost", false, "Tower fire rate boost x2");
            BallistaBoost      = Config.Bind("4. Development", "BallistaBoost", false, "Ballista power boost x2");
            BallistaReloadMult = Config.Bind("4. Development", "BallistaReloadMult", 1.0f, "Ballista reload speed multiplier");
            BallistaFlightMult = Config.Bind("4. Development", "BallistaFlightMult", 1.0f, "Ballista projectile speed multiplier");
            CatapultBoost      = Config.Bind("4. Development", "CatapultBoost", false, "Catapult reload speed boost x3");
            CatapultReloadMult = Config.Bind("4. Development", "CatapultReloadMult", 1.0f, "Catapult reload speed multiplier");
            CatapultFlightMult = Config.Bind("4. Development", "CatapultFlightMult", 1.0f, "Catapult projectile speed multiplier");
            InstantCastle      = Config.Bind("4. Development", "InstantCastle", false, "Instant castle upgrade completion");

            BuilderSpeedMult   = Config.Bind("4. Development", "BuilderSpeedMult", 1.0f, "Builder movement speed multiplier");
            BuilderWorkMult    = Config.Bind("4. Development", "BuilderWorkMult", 1.0f, "Builder efficiency multiplier (lower is faster)");

            
            EnableSizeHack    = Config.Bind("5. Player Hack", "EnableSizeHack", false, "Enable player scaling");
            TargetSize        = Config.Bind("5. Player Hack", "TargetSize", 1.0f, "Player scale multiplier");

            
            SteedSpeedMult    = Config.Bind("6. Steed", "SteedSpeedMult", 1.0f, "Steed run speed scale");
            ChargeDmgBoost    = Config.Bind("6. Steed", "ChargeDmgBoost", false, "Steed charge damage x2");
            BuffAuraDuration  = Config.Bind("6. Steed", "BuffAuraDuration", 1.0f, "Steed buff aura duration multiplier");

            
            WaveSizeMult       = Config.Bind("7. Enemies", "WaveSizeMult", 1.0f, "Wave size multiplier");
            EnemySpeedMult     = Config.Bind("7. Enemies", "EnemySpeedMult", 1.0f, "Enemy speed multiplier");
            PortalSpawnRate    = Config.Bind("7. Enemies", "PortalSpawnRate", 1.0f, "Portal spawn rate multiplier");
            NoCrownStealing    = Config.Bind("7. Enemies", "NoCrownStealing", false, "Disable crown stealing");
            GreedQueenHPScale  = Config.Bind("7. Enemies", "GreedQueenHPScale", 1.0f, "Greed Queen HP scale");
            DirectorThreatMult = Config.Bind("7. Enemies", "DirectorThreatMult", 1.0f, "Director threat ramp multiplier");
        }
    }
}
