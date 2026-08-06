namespace KingdomEnhanced.Features
{
    /// <summary>
    /// Central gate for difficulty-restricted features and per-preset gameplay multipliers.
    /// </summary>
    public static class DifficultyRules
    {
        /// <summary>Returns whether any hard mode preset is currently active.</summary>
        public static bool IsHardModeActive()
        {
            return HardModeFeature.GetActivePreset() != HardModePreset.None;
        }

        /// <summary>Returns whether the coin cheat is permitted under the active rules.</summary>
        public static bool CanAddCoins()
        {
            return true;
        }

        /// <summary>Returns whether the gem cheat is permitted under the active rules.</summary>
        public static bool CanAddGems()
        {
            return true;
        }

        /// <summary>Returns whether invincible walls are permitted under the active rules.</summary>
        public static bool CanUseInvincibleWalls()
        {
            return true;
        }

        /// <summary>Returns whether disabling blood moons is permitted under the active rules.</summary>
        public static bool CanUseNoBloodMoons()
        {
            return true;
        }


        /// <summary>Returns whether instant construction is permitted; disabled while hard mode is active.</summary>
        public static bool CanUseInstantConstruction()
        {
            return KingdomEnhanced.UI.ModMenu.HyperBuilders && !IsHardModeActive();
        }


        /// <summary>Returns the builder movement speed multiplier for the active preset.</summary>
        public static float GetBuilderSpeedMultiplier()
        {
            return 1.0f;
        }

        /// <summary>Returns the builder work time multiplier for the active preset.</summary>
        public static float GetBuilderWorkTime()
        {
            var preset = HardModeFeature.GetActivePreset();
            switch (preset)
            {
                case HardModePreset.NoEscape:   return 1.0f;
                case HardModePreset.Nightmare:  return 0.3f;
                case HardModePreset.Relentless: return 0.001f;
                default: return 0.001f;
            }
        }

        /// <summary>Returns the maximum number of vagrants recruitable per use under the active preset.</summary>
        public static int GetMaxRecruitPerUse()
        {
            var preset = HardModeFeature.GetActivePreset();
            switch (preset)
            {
                case HardModePreset.NoEscape:  return 5;
                case HardModePreset.Nightmare: return 10;
                default: return int.MaxValue;
            }
        }

        /// <summary>Display name of the active hard mode preset, or null when no preset is active.</summary>
        public static string ActivePresetName
        {
            get
            {
                switch (HardModeFeature.GetActivePreset())
                {
                    case HardModePreset.Nightmare:  return "NIGHTMARE";
                    case HardModePreset.Relentless: return "RELENTLESS";
                    case HardModePreset.Oblivion:   return "OBLIVION";
                    case HardModePreset.NoEscape:   return "NO ESCAPE";
                    default: return null;
                }
            }
        }

        /// <summary>Number of restrictions imposed by the active preset, or zero when no preset is active.</summary>
        public static int ActiveRestrictionCount
        {
            get
            {
                if (!IsHardModeActive()) return 0;
                int n = 4;
                if (GetMaxRecruitPerUse() < int.MaxValue) n++;
                return n;
            }
        }

        /// <summary>Builds the banner label describing the active preset and its restriction count.</summary>
        public static string GetDifficultyBannerLabel()
        {
            string name = ActivePresetName;
            if (name == null) return null;
            return $"[!] {name} -- {ActiveRestrictionCount} Restrictions Active";
        }
    }
}
