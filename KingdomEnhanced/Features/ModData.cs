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
        /// <summary>IL2CPP constructor required by Unity's Il2Cpp interop.</summary>
        public ModData(IntPtr ptr) : base(ptr) { }
#endif

        /// <summary>
        /// Archer — base fire rate (shoot cooldown) cached at spawn.
        /// </summary>
        public float baseFireRate;
        
        /// <summary>
        /// Berserker / Ninja — base run speed cached at spawn.
        /// </summary>
        public float baseSpeed;
        
        /// <summary>
        /// Farm — base coin yield cached at spawn.
        /// </summary>
        public float baseCoinYield;
        
        /// <summary>
        /// Bolt — base damage and launch force cached at spawn.
        /// </summary>
        public int baseDamage;
        /// <summary>
        /// Base launch force cached at spawn.
        /// </summary>
        public float baseForce;
        
        /// <summary>
        /// Portal — base spawn interval cached at spawn.
        /// </summary>
        public float baseSpawnInterval;
        
        /// <summary>
        /// Mover — base move speed cached on first update.
        /// </summary>
        public float moverBaseSpeed;
        
        /// <summary>Last speed value actually written to the Mover, used to skip duplicate reflection writes.</summary>
        public float lastAppliedMoverSpeed = float.MinValue;
        
        /// <summary>
        /// ArtemisBow — base arrow count, range, and damage cached before modification.
        /// </summary>
        public int artemisBaseArrows;
        /// <summary>
        /// Base arrow range cached before modification.
        /// </summary>
        public float artemisBaseRange;
        /// <summary>
        /// Base arrow damage cached before modification.
        /// </summary>
        public int artemisBaseDamage;
        
        /// <summary>
        /// Knight — base HP.
        /// </summary>
        public int knightBaseHp;
        
        /// <summary>
        /// Worker — base speed and work time.
        /// </summary>
        public float workerBaseSpeed;
        /// <summary>
        /// Base work time cached before modification.
        /// </summary>
        public float workerBaseWorkTime;

        /// <summary>
        /// Catapult — base crank rates cached before modification.
        /// </summary>
        public float baseCrankRate;
        /// <summary>
        /// Base formation crank rate cached before modification.
        /// </summary>
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

        /// <summary>
        /// Ballista — accumulated fractional reload work.
        /// </summary>
        public float ballistaFractionalWork;

        /// <summary>
        /// Guards one-time caching of base values.
        /// </summary>
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
