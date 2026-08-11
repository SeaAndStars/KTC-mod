using UnityEngine;
using System;
#if IL2CPP
using KingdomEnhanced.Shared.Attributes;
#endif

namespace KingdomEnhanced.Features
{
    /// <summary>
    /// Per-unit component that tracks the original max HP and buff state for hard mode scaling.
    /// </summary>
#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    public class HardModeBuffData : MonoBehaviour
    {
        /// <summary>The unit's original maximum hit points before hard mode buffs were applied.</summary>
        public int OriginalMaxHp;
        /// <summary>Whether the unit is currently buffed by hard mode.</summary>
        public bool IsBuffed;

#if IL2CPP
        /// <summary>IL2CPP interop constructor.</summary>
        public HardModeBuffData(IntPtr ptr) : base(ptr) { }
#endif
    }
}
