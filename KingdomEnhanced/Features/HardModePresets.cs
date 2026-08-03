using System.Collections.Generic;
using UnityEngine;
using KingdomEnhanced.Core;

namespace KingdomEnhanced.Features
{
    public enum HardModePreset
    {
        None = 0,
        Nightmare = 100,
        Relentless = 101,
        Oblivion = 102,
        NoEscape = 103
    }

    public static class HardModePresets
    {
        public static readonly Dictionary<HardModePreset, string> PresetNames = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, "hard_mode.preset.nightmare.name" },
            { HardModePreset.Relentless, "hard_mode.preset.relentless.name" },
            { HardModePreset.Oblivion, "hard_mode.preset.oblivion.name" },
            { HardModePreset.NoEscape, "hard_mode.preset.no_escape.name" }
        };

        public static readonly Dictionary<HardModePreset, string> PresetDescriptions = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, "hard_mode.preset.nightmare.description" },
            { HardModePreset.Relentless, "hard_mode.preset.relentless.description" },
            { HardModePreset.Oblivion, "hard_mode.preset.oblivion.description" },
            { HardModePreset.NoEscape, "hard_mode.preset.no_escape.description" }
        };

        public static DifficultyData CreateDifficultyData(HardModePreset preset)
        {
            var data = new DifficultyData();

            data.difficultyName = LocalizationService.Get(PresetNames[preset]);
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
