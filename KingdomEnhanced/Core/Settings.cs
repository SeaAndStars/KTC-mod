using BepInEx.Configuration;
using UnityEngine;

namespace KingdomEnhanced.Core
{
    public static class Settings
    {
        // The active BepInEx configuration file
        public static ConfigFile Config;

        /// <summary>
        /// Stores the currently active UI language code.
        /// </summary>
        public static ConfigEntry<string> Language;

        
        // Use the new Beta UI style
        public static ConfigEntry<bool> UseBetaUI;
        // Show the energy/stamina bar
        public static ConfigEntry<bool> ShowStaminaBar;
        // Show the day/night and coins HUD
        public static ConfigEntry<bool> DisplayTimes;
        // Show the greed counter HUD
        public static ConfigEntry<bool> ShowGreedCounter;
        // Use a 12-hour clock (AM/PM) instead of a 24-hour one in the HUD
        public static ConfigEntry<bool> Use12HourClock;
        
        
        // Enable the hover tracking logic
        public static ConfigEntry<bool> EnableAccessibility;
        // Enable text-to-speech output
        public static ConfigEntry<bool> EnableTTS;
        // Queue messages instead of interrupting current speech
        public static ConfigEntry<bool> NarratorQueueMode;
        // Simplify object names for TTS
        public static ConfigEntry<bool> SimplifyNames;
        // Announce entering/leaving the castle
        public static ConfigEntry<bool> EnableCastleAnnouncer;
        // Show debug boxes for announcer zones
        public static ConfigEntry<bool> DebugZones;

        
        // Unlock the cheat menu
        public static ConfigEntry<bool> CheatsUnlocked;
        // Travel speed multiplier
        public static ConfigEntry<float> SpeedMultiplier;
        // Infinite mount stamina
        public static ConfigEntry<bool> InfiniteStamina;
        // Walls repair instantly
        public static ConfigEntry<bool> InvincibleWalls;
        // Disable item ability cooldowns
        public static ConfigEntry<bool> NoToolCooldowns;
        // Artemis Bow arrows fired per cast
        public static ConfigEntry<float> ArtemisArrowCount;
        // Artemis Bow ability range multiplier
        public static ConfigEntry<float> ArtemisRangeMult;
        // Artemis Bow arrow damage multiplier
        public static ConfigEntry<float> ArtemisArrowDamageMult;
        // Coin income multiplier
        public static ConfigEntry<float> CoinIncomeMult;
        // Bag drop multiplier
        public static ConfigEntry<float> BagDropMult;
        // Number of units to spawn
        public static ConfigEntry<float> SpawnUnitCount;
        
        
        // Instant construction
        public static ConfigEntry<bool> HyperBuilders;
        // Expand vagrant camps
        public static ConfigEntry<bool> LargerCamps;
        // Elite knight buffs
        public static ConfigEntry<bool> BetterKnight;
        // Rapid housing
        public static ConfigEntry<bool> BetterCitizenHouses;
        // Lock the season to summer
        public static ConfigEntry<bool> LockSummer;
        // Force clear weather
        public static ConfigEntry<bool> ClearWeather;
        // Disable blood moon events
        public static ConfigEntry<bool> NoBloodMoons;
        // Coins don't sink in water
        public static ConfigEntry<bool> CoinsStayDry;
        // Archer fire rate boost x2
        public static ConfigEntry<bool> ArcherFireBoost;
        // Berserker rage mode (earlier)
        public static ConfigEntry<bool> BerserkerRage;
        // Ninja speed boost x2
        public static ConfigEntry<bool> NinjaSpeedBoost;
        // Recruit cap override (0 = default)
        public static ConfigEntry<int> RecruitCapOverride;
        // Tree regrowth speed multiplier
        public static ConfigEntry<float> TreeRegrowthMult;
        // Animal spawn boost
        public static ConfigEntry<bool> AnimalSpawnBoost;
        // Instant day skip
        public static ConfigEntry<bool> InstantDaySkip;
        // Farm output boost x2
        public static ConfigEntry<bool> FarmOutputBoost;
        // Tower fire rate boost x2
        public static ConfigEntry<bool> TowerFireBoost;
        // Ballista power boost x2
        public static ConfigEntry<bool> BallistaBoost;
        // Ballista reload speed multiplier
        public static ConfigEntry<float> BallistaReloadMult;
        // Ballista projectile speed multiplier
        public static ConfigEntry<float> BallistaFlightMult;
        // Catapult reload speed boost x3
        public static ConfigEntry<bool> CatapultBoost;
        // Catapult reload speed multiplier
        public static ConfigEntry<float> CatapultReloadMult;
        // Catapult projectile speed multiplier
        public static ConfigEntry<float> CatapultFlightMult;
        // Instant castle upgrade completion
        public static ConfigEntry<bool> InstantCastle;

        // Builder movement speed multiplier
        public static ConfigEntry<float> BuilderSpeedMult;
        // Builder efficiency multiplier (lower is faster)
        public static ConfigEntry<float> BuilderWorkMult;

        
        // Enable player scaling
        public static ConfigEntry<bool> EnableSizeHack;
        // Player scale multiplier
        public static ConfigEntry<float> TargetSize;

        
        // Steed run speed scale
        public static ConfigEntry<float> SteedSpeedMult;
        // Steed charge damage x2
        public static ConfigEntry<bool> ChargeDmgBoost;
        // Steed buff aura duration multiplier
        public static ConfigEntry<float> BuffAuraDuration;

        
        // Wave size multiplier
        public static ConfigEntry<float> WaveSizeMult;
        // Enemy speed multiplier
        public static ConfigEntry<float> EnemySpeedMult;
        // Portal spawn rate multiplier
        public static ConfigEntry<float> PortalSpawnRate;
        // Disable crown stealing
        public static ConfigEntry<bool> NoCrownStealing;
        // Greed Queen HP scale
        public static ConfigEntry<float> GreedQueenHPScale;
        // Director threat ramp multiplier
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
