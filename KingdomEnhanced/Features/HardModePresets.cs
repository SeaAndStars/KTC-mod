using System.Collections.Generic;
using UnityEngine;
using KingdomEnhanced.Core;

namespace KingdomEnhanced.Features
{
    /// <summary>Available hard mode difficulty presets.</summary>
    public enum HardModePreset
    {
        // No preset; default difficulty
        None = 0,
        // Balanced default preset
        Nightmare = 100,
        // Preset with increased enemy spawns
        Relentless = 101,
        // Preset with heavier difficulty and retaliation multipliers
        Oblivion = 102,
        // Preset with stronger and faster enemies
        NoEscape = 103
    }

    public static class HardModePresets
    {
        /// <summary>
        /// Localization resource keys for the hard mode preset names.
        /// </summary>
        private static readonly Dictionary<HardModePreset, string> PresetNameKeys = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, "hard_mode.preset.nightmare.name" },
            { HardModePreset.Relentless, "hard_mode.preset.relentless.name" },
            { HardModePreset.Oblivion, "hard_mode.preset.oblivion.name" },
            { HardModePreset.NoEscape, "hard_mode.preset.no_escape.name" }
        };

        /// <summary>
        /// Localization resource keys for the hard mode preset descriptions.
        /// </summary>
        private static readonly Dictionary<HardModePreset, string> PresetDescriptionKeys = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, "hard_mode.preset.nightmare.description" },
            { HardModePreset.Relentless, "hard_mode.preset.relentless.description" },
            { HardModePreset.Oblivion, "hard_mode.preset.oblivion.description" },
            { HardModePreset.NoEscape, "hard_mode.preset.no_escape.description" }
        };

        /// <summary>
        /// Stable English names for the hard mode presets.
        /// </summary>
        public static readonly Dictionary<HardModePreset, string> PresetNames = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, "Nightmare" },
            { HardModePreset.Relentless, "Relentless" },
            { HardModePreset.Oblivion, "Oblivion" },
            { HardModePreset.NoEscape, "No Escape" }
        };

        /// <summary>
        /// Stable English descriptions for the hard mode presets.
        /// </summary>
        public static readonly Dictionary<HardModePreset, string> PresetDescriptions = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, "Massive waves of enemies. Prepare your defenses." },
            { HardModePreset.Relentless, "Nights are longer. Enemies never retreat." },
            { HardModePreset.Oblivion, "The Blood Moon rises often. Retaliation is swift." },
            { HardModePreset.NoEscape, "Enemies are stronger, faster, and deadlier." }
        };

        /// <summary>
        /// Gets the final display name of the specified hard mode preset in the current language.
        /// </summary>
        /// <param name="preset">The hard mode preset.</param>
        /// <returns>The preset display name in the current language.</returns>
        public static string GetPresetName(HardModePreset preset)
        {
            string name = LocalizationService.Get(PresetNameKeys[preset]);
            return name;
        }

        /// <summary>
        /// Gets the final display description of the specified hard mode preset in the current language.
        /// </summary>
        /// <param name="preset">The hard mode preset.</param>
        /// <returns>The preset display description in the current language.</returns>
        public static string GetPresetDescription(HardModePreset preset)
        {
            string description = LocalizationService.Get(PresetDescriptionKeys[preset]);
            return description;
        }

        /// <summary>
        /// Creates the difficulty data configured for the specified hard mode preset.
        /// </summary>
        /// <param name="preset">The hard mode preset.</param>
        /// <returns>Difficulty data with the configured multipliers and display name.</returns>
        public static DifficultyData CreateDifficultyData(HardModePreset preset)
        {
            var data = new DifficultyData();

            data.difficultyName = PresetNames[preset];
            data.difficultyLevel = (DifficultyData.DifficultyLevel)(int)preset;

            data.difficultyMultiplier = 1.0f;
            data.difficultyMultiplierBosses = 1.0f;
            data.retaliationMultiplier = 1.0f;

            switch (preset)
            {
                case HardModePreset.Nightmare:
                    break;
                case HardModePreset.Oblivion:
                    data.difficultyMultiplier = 5.0f;
                    data.retaliationMultiplier = 2.0f;
                    break;
                case HardModePreset.NoEscape:
                    data.difficultyMultiplierBosses = 2.0f;
                    break;
            }

            return data;
        }
    }
}
