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
    public class ModData : MonoBehaviour
    {
#if IL2CPP
        public ModData(IntPtr ptr) : base(ptr) { }
#endif

        // Archer
        public float baseFireRate;
        
        // Berserker, Ninja
        public float baseSpeed;
        
        // Farm
        public float baseCoinYield;
        
        // Bolt
        public int baseDamage;
        public float baseForce;
        
        // Portal
        public float baseSpawnInterval;
        
        // Mover
        public float moverBaseSpeed;
        
        /// <summary>Mover 上次实际写入的速度值,用于跳过值未变化的重复反射写入</summary>
        public float lastAppliedMoverSpeed = float.MinValue;
        
        // ArtemisBow
        public int artemisBaseArrows;
        public float artemisBaseRange;
        public int artemisBaseDamage;
        
        // Knight
        public int knightBaseHp;
        
        // Worker
        public float workerBaseSpeed;
        public float workerBaseWorkTime;

        // Catapult
        public float baseCrankRate;
        public float baseCrankRateFormation;

        /// <summary>Catapult 上次实际写入的曲柄速率,用于跳过值未变化的重复反射写入</summary>
        public float lastAppliedCrankRate = float.MinValue;

        /// <summary>Catapult 上次实际写入的集结曲柄速率,用于跳过值未变化的重复反射写入</summary>
        public float lastAppliedCrankRateFormation = float.MinValue;

        // Archer
        /// <summary>Archer 是否已缓存过塔归属检查结果</summary>
        public bool towerCheckCached = false;

        /// <summary>Archer 是否位于塔内(缓存结果,避免每帧 GetComponentInParent)</summary>
        public bool cachedInTower = false;

        // Ballista
        public float ballistaFractionalWork;

        public bool isInitialized = false;

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
