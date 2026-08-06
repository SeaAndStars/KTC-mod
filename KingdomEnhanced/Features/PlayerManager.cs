using System;
using UnityEngine;
using KingdomEnhanced.UI;
#if IL2CPP
using KingdomEnhanced.Shared.Attributes;
#endif

namespace KingdomEnhanced.Features
{
#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    /// <summary>Per-frame manager that applies size hacks, infinite stamina and the F2 wallet-refill cheat to the player.</summary>
    public class PlayerManager : MonoBehaviour
    {
#if IL2CPP
        /// <summary>IL2CPP interop constructor.</summary>
        public PlayerManager(IntPtr ptr) : base(ptr) { }
#endif
        /// <summary>Cached reference to the player instance.</summary>
        private Player _player;
        /// <summary>Cached default steed run speed; -1f means not captured yet.</summary>
        private float _defaultSpeed = -1f;

        /// <summary>Unity Start callback; intentionally empty because PlayerManager is not on the Player's GameObject.</summary>
        void Start() { } // PlayerManager is NOT on the Player's GameObject

        /// <summary>Per-frame update: applies size hacks and handles the F2 wallet-refill cheat.</summary>
        void Update()
        {
            // Find player if not yet cached
            if (_player == null)
            {
                _player = FindFirstObjectByType<Player>();
                if (_player == null) return;
            }

            if (_player.steed != null && _defaultSpeed == -1f)
                _defaultSpeed = _player.steed.runSpeed;

            if (ModMenu.EnableSizeHack)
            {
                float dir = 1f;
                if (_player.mover != null && _player.mover.GetDirection() == Side.Left) dir = -1f;
                _player.transform.localScale = new Vector3(ModMenu.TargetSize * dir, ModMenu.TargetSize, 1f);
            }
            else
            {
                float dir = 1f;
                if (_player.mover != null && _player.mover.GetDirection() == Side.Left) dir = -1f;
                _player.transform.localScale = new Vector3(dir, 1f, 1f);
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (!DifficultyRules.CanAddCoins())
                {
                    ModMenu.Speak($"<color=red>🔒 Wallet Refill locked in {HardModeFeature.GetActivePreset()}</color>");
                }
                else if (_player.wallet != null)
                {
                    _player.wallet.SetCurrency(CurrencyType.Coins, 50);
                    ModMenu.Speak("Cheat: Wallet Refilled.");
                }
            }
        }

        /// <summary>Applies infinite stamina to the player's steed when enabled.</summary>
        void LateUpdate()
        {
            if (ModMenu.InfiniteStamina && _player != null && _player.steed != null)
            {
                var s = _player.steed;
                s.Stamina = 100f;
                if (s.IsTired) s._tiredTimer = -1f;
            }
        }

        /// <summary>Triggers the banker to pay out 100 coins via SendMessage.</summary>
        public static void ForceBankerPayout()
        {
            var banker = FindFirstObjectByType<Banker>();
            if (banker != null)
            {
                banker.SendMessage("AddCoins", 100, SendMessageOptions.DontRequireReceiver);
                banker.SendMessage("DropCoins", SendMessageOptions.DontRequireReceiver);
                ModMenu.Speak("Banker Payout Triggered!");
            }
            else ModMenu.Speak("Banker not found.");
        }

        /// <summary>Resets the cached default speed when destroyed.</summary>
        void OnDestroy()
        {
            _defaultSpeed = -1f;
        }
    }
}