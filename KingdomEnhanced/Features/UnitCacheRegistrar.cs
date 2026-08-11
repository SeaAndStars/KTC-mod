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
    /// <summary>Registers its GameObject's unit into the matching UnitCacheManager cache on enable and removes it on disable.</summary>
    public class UnitCacheRegistrar : MonoBehaviour
    {
#if IL2CPP
        /// <summary>IL2CPP interop constructor.</summary>
        public UnitCacheRegistrar(IntPtr ptr) : base(ptr) { }
#endif

        /// <summary>Cached Archer component of this GameObject.</summary>
        private Archer _archer;
        /// <summary>Cached Worker component of this GameObject.</summary>
        private Worker _worker;
        /// <summary>Cached Knight component of this GameObject.</summary>
        private Knight _knight;
        /// <summary>Cached Ninja component of this GameObject.</summary>
        private Ninja _ninja;
        /// <summary>Cached Berserker component of this GameObject.</summary>
        private Berserker _berserker;
        /// <summary>Cached Castle component of this GameObject.</summary>
        private Castle _castle;
        /// <summary>Cached BeggarCamp component of this GameObject.</summary>
        private BeggarCamp _beggarCamp;
        /// <summary>Cached Enemy component of this GameObject.</summary>
        private Enemy _enemy;
        /// <summary>Cached Peasant component of this GameObject.</summary>
        private Peasant _peasant;
        /// <summary>Cached Farmer component of this GameObject.</summary>
        private Farmer _farmer;
        /// <summary>Cached Pikeman component of this GameObject.</summary>
        private Pikeman _pikeman;
        /// <summary>Cached Beggar component of this GameObject.</summary>
        private Beggar _beggar;
        /// <summary>Cached Ballista component of this GameObject.</summary>
        private Ballista _ballista;
        /// <summary>Cached Catapult component of this GameObject.</summary>
        private Catapult _catapult;
        /// <summary>Cached Wall component of this GameObject.</summary>
        private Wall _wall;
        /// <summary>Cached Portal component of this GameObject.</summary>
        private Portal _portal;

        /// <summary>Caches all relevant unit components on this GameObject.</summary>
        private void Awake()
        {
            _archer = GetComponent<Archer>();
            _worker = GetComponent<Worker>();
            _knight = GetComponent<Knight>();
            _ninja = GetComponent<Ninja>();
            _berserker = GetComponent<Berserker>();
            _castle = GetComponent<Castle>();
            _beggarCamp = GetComponent<BeggarCamp>();
            _enemy = GetComponent<Enemy>();
            _peasant = GetComponent<Peasant>();
            _farmer = GetComponent<Farmer>();
            _pikeman = GetComponent<Pikeman>();
            _beggar = GetComponent<Beggar>();
            _ballista = GetComponent<Ballista>();
            _catapult = GetComponent<Catapult>();
            _wall = GetComponent<Wall>();
            _portal = GetComponent<Portal>();
        }

        /// <summary>Registers this GameObject's cached unit into the matching UnitCacheManager caches.</summary>
        private void OnEnable()
        {
            if (_archer != null) UnitCacheManager.Archers.Add(_archer);
            if (_worker != null) UnitCacheManager.Workers.Add(_worker);
            if (_knight != null) UnitCacheManager.Knights.Add(_knight);
            if (_ninja != null) UnitCacheManager.Ninjas.Add(_ninja);
            if (_berserker != null) UnitCacheManager.Berserkers.Add(_berserker);
            if (_castle != null) UnitCacheManager.Castles.Add(_castle);
            if (_beggarCamp != null) UnitCacheManager.BeggarCamps.Add(_beggarCamp);
            if (_enemy != null) UnitCacheManager.Enemies.Add(_enemy);
            if (_peasant != null) UnitCacheManager.Peasants.Add(_peasant);
            if (_farmer != null) UnitCacheManager.Farmers.Add(_farmer);
            if (_pikeman != null) UnitCacheManager.Pikemen.Add(_pikeman);
            if (_beggar != null) UnitCacheManager.Beggars.Add(_beggar);
            if (_ballista != null) UnitCacheManager.Ballistas.Add(_ballista);
            if (_catapult != null) UnitCacheManager.Catapults.Add(_catapult);
            if (_wall != null) UnitCacheManager.Walls.Add(_wall);
            if (_portal != null) UnitCacheManager.Portals.Add(_portal);
        }

        /// <summary>Removes this GameObject's cached unit from the matching UnitCacheManager caches.</summary>
        private void OnDisable()
        {
            if (_archer != null) UnitCacheManager.Archers.Remove(_archer);
            if (_worker != null) UnitCacheManager.Workers.Remove(_worker);
            if (_knight != null) UnitCacheManager.Knights.Remove(_knight);
            if (_ninja != null) UnitCacheManager.Ninjas.Remove(_ninja);
            if (_berserker != null) UnitCacheManager.Berserkers.Remove(_berserker);
            if (_castle != null) UnitCacheManager.Castles.Remove(_castle);
            if (_beggarCamp != null) UnitCacheManager.BeggarCamps.Remove(_beggarCamp);
            if (_enemy != null) UnitCacheManager.Enemies.Remove(_enemy);
            if (_peasant != null) UnitCacheManager.Peasants.Remove(_peasant);
            if (_farmer != null) UnitCacheManager.Farmers.Remove(_farmer);
            if (_pikeman != null) UnitCacheManager.Pikemen.Remove(_pikeman);
            if (_beggar != null) UnitCacheManager.Beggars.Remove(_beggar);
            if (_ballista != null) UnitCacheManager.Ballistas.Remove(_ballista);
            if (_catapult != null) UnitCacheManager.Catapults.Remove(_catapult);
            if (_wall != null) UnitCacheManager.Walls.Remove(_wall);
            if (_portal != null) UnitCacheManager.Portals.Remove(_portal);
        }

        /// <summary>Ensures a UnitCacheRegistrar component is attached to the given GameObject.</summary>
        public static void EnsureAttached(GameObject obj)
        {
            if (obj != null && obj.GetComponent<UnitCacheRegistrar>() == null)
            {
                obj.AddComponent<UnitCacheRegistrar>();
            }
        }
    }
}
