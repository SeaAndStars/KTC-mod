using System;
using UnityEngine;
#if IL2CPP
using KingdomEnhanced.Shared.Attributes;
#endif

namespace KingdomEnhanced.Features
{
#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    /// <summary>
    /// Per-object cached baseline values used by patches to apply multipliers without compounding.
    /// </summary>
    public class ModData : MonoBehaviour
    {
#if IL2CPP
        public ModData(IntPtr ptr) : base(ptr) { }
#endif

        // Archer — base fire rate (shoot cooldown) cached at spawn
        public float baseFireRate;
        
        // Berserker / Ninja — base run speed cached at spawn
        public float baseSpeed;
        
        // Farm — base coin yield cached at spawn
        public float baseCoinYield;
        
        // Bolt — base damage and launch force cached at spawn
        public int baseDamage;
        public float baseForce;
        
        // Portal — base spawn interval cached at spawn
        public float baseSpawnInterval;
        
        // Mover — base move speed cached on first update
        public float moverBaseSpeed;
        
        /// <summary>Last speed value actually written to the Mover, used to skip duplicate reflection writes.</summary>
        public float lastAppliedMoverSpeed = float.MinValue;
        
        // ArtemisBow — base arrow count, range, and damage cached before modification
        public int artemisBaseArrows;
        public float artemisBaseRange;
        public int artemisBaseDamage;
        
        // Knight — base HP
        public int knightBaseHp;
        
        // Worker — base speed and work time
        public float workerBaseSpeed;
        public float workerBaseWorkTime;

        // Catapult — base crank rates cached before modification
        public float baseCrankRate;
        public float baseCrankRateFormation;

        /// <summary>Last crank rate actually written to the Catapult, used to skip duplicate reflection writes.</summary>
        public float lastAppliedCrankRate = float.MinValue;

        /// <summary>Last formation crank rate actually written to the Catapult, used to skip duplicate reflection writes.</summary>
        public float lastAppliedCrankRateFormation = float.MinValue;

        // Archer — cached tower membership check
        /// <summary>Whether the Archer's tower membership check has been cached.</summary>
        public bool towerCheckCached = false;

        /// <summary>Whether the Archer is inside a tower (cached to avoid per-frame GetComponentInParent).</summary>
        public bool cachedInTower = false;

        // Ballista — accumulated fractional reload work
        public float ballistaFractionalWork;

        // Guards one-time caching of base values
        public bool isInitialized = false;

        /// <summary>Gets the ModData component on the object, adding it if missing.</summary>
        public static ModData GetOrAdd(GameObject obj)
        {
            var data = obj.GetComponent<ModData>();
            if (data == null)
            {
                data = obj.AddComponent<ModData>();
            }
            return data;
        }
    }
}
