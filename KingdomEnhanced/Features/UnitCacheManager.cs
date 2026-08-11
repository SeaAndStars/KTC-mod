using System.Collections.Generic;
using UnityEngine;

namespace KingdomEnhanced.Features
{
    /// <summary>Static caches of active in-game units and structures, kept in sync by UnitCacheRegistrar and seeded once on cold boot.</summary>
    public static class UnitCacheManager
    {
        /// <summary>All active Archer units.</summary>
        public static HashSet<Archer> Archers = new HashSet<Archer>();
        /// <summary>All active Worker units.</summary>
        public static HashSet<Worker> Workers = new HashSet<Worker>();
        /// <summary>All active Knight units.</summary>
        public static HashSet<Knight> Knights = new HashSet<Knight>();
        /// <summary>All active Ninja units.</summary>
        public static HashSet<Ninja> Ninjas = new HashSet<Ninja>();
        /// <summary>All active Berserker units.</summary>
        public static HashSet<Berserker> Berserkers = new HashSet<Berserker>();
        /// <summary>All active Castle structures.</summary>
        public static HashSet<Castle> Castles = new HashSet<Castle>();
        /// <summary>All active BeggarCamp structures.</summary>
        public static HashSet<BeggarCamp> BeggarCamps = new HashSet<BeggarCamp>();
        /// <summary>All active Enemy units.</summary>
        public static HashSet<Enemy> Enemies = new HashSet<Enemy>();
        /// <summary>All active Peasant units.</summary>
        public static HashSet<Peasant> Peasants = new HashSet<Peasant>();
        /// <summary>All active Farmer units.</summary>
        public static HashSet<Farmer> Farmers = new HashSet<Farmer>();
        /// <summary>All active Pikeman units.</summary>
        public static HashSet<Pikeman> Pikemen = new HashSet<Pikeman>();
        /// <summary>All active Beggar units.</summary>
        public static HashSet<Beggar> Beggars = new HashSet<Beggar>();
        /// <summary>All active Ballista structures.</summary>
        public static HashSet<Ballista> Ballistas = new HashSet<Ballista>();
        /// <summary>All active Catapult structures.</summary>
        public static HashSet<Catapult> Catapults = new HashSet<Catapult>();
        /// <summary>All active Wall structures.</summary>
        public static HashSet<Wall> Walls = new HashSet<Wall>();
        /// <summary>All active Portal structures.</summary>
        public static HashSet<Portal> Portals = new HashSet<Portal>();

        /// <summary>True once the one-time cold boot seeding has run.</summary>
        private static bool _coldBootDone = false;

        /// <summary>Seeds all caches with currently active entities once, on cold boot.</summary>
        public static void CheckColdBoot()
        {
            if (_coldBootDone) return;
            _coldBootDone = true;

            Archers = new HashSet<Archer>(Object.FindObjectsByType<Archer>(FindObjectsSortMode.None));
            Workers = new HashSet<Worker>(Object.FindObjectsByType<Worker>(FindObjectsSortMode.None));
            Knights = new HashSet<Knight>(Object.FindObjectsByType<Knight>(FindObjectsSortMode.None));
            Ninjas = new HashSet<Ninja>(Object.FindObjectsByType<Ninja>(FindObjectsSortMode.None));
            Berserkers = new HashSet<Berserker>(Object.FindObjectsByType<Berserker>(FindObjectsSortMode.None));
            Castles = new HashSet<Castle>(Object.FindObjectsByType<Castle>(FindObjectsSortMode.None));
            BeggarCamps = new HashSet<BeggarCamp>(Object.FindObjectsByType<BeggarCamp>(FindObjectsSortMode.None));
            Enemies = new HashSet<Enemy>(Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None));
            Peasants = new HashSet<Peasant>(Object.FindObjectsByType<Peasant>(FindObjectsSortMode.None));
            Farmers = new HashSet<Farmer>(Object.FindObjectsByType<Farmer>(FindObjectsSortMode.None));
            Pikemen = new HashSet<Pikeman>(Object.FindObjectsByType<Pikeman>(FindObjectsSortMode.None));
            Beggars = new HashSet<Beggar>(Object.FindObjectsByType<Beggar>(FindObjectsSortMode.None));
            Ballistas = new HashSet<Ballista>(Object.FindObjectsByType<Ballista>(FindObjectsSortMode.None));
            Catapults = new HashSet<Catapult>(Object.FindObjectsByType<Catapult>(FindObjectsSortMode.None));
            Walls = new HashSet<Wall>(Object.FindObjectsByType<Wall>(FindObjectsSortMode.None));
            Portals = new HashSet<Portal>(Object.FindObjectsByType<Portal>(FindObjectsSortMode.None));
            
            KingdomEnhanced.Core.Plugin.Instance.LogSource.LogInfo("[UnitCacheManager] Cold boot complete. Cached active entities.");
        }
    }
}
