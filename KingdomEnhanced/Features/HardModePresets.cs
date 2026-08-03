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
        /// <summary>
        /// 硬模式预设名称对应的本地化资源键。
        /// </summary>
        private static readonly Dictionary<HardModePreset, string> PresetNameKeys = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, "hard_mode.preset.nightmare.name" },
            { HardModePreset.Relentless, "hard_mode.preset.relentless.name" },
            { HardModePreset.Oblivion, "hard_mode.preset.oblivion.name" },
            { HardModePreset.NoEscape, "hard_mode.preset.no_escape.name" }
        };

        /// <summary>
        /// 硬模式预设描述对应的本地化资源键。
        /// </summary>
        private static readonly Dictionary<HardModePreset, string> PresetDescriptionKeys = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, "hard_mode.preset.nightmare.description" },
            { HardModePreset.Relentless, "hard_mode.preset.relentless.description" },
            { HardModePreset.Oblivion, "hard_mode.preset.oblivion.description" },
            { HardModePreset.NoEscape, "hard_mode.preset.no_escape.description" }
        };

        /// <summary>
        /// 硬模式预设的最终展示名称。
        /// </summary>
        public static readonly Dictionary<HardModePreset, string> PresetNames = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, LocalizationService.Get(PresetNameKeys[HardModePreset.Nightmare]) },
            { HardModePreset.Relentless, LocalizationService.Get(PresetNameKeys[HardModePreset.Relentless]) },
            { HardModePreset.Oblivion, LocalizationService.Get(PresetNameKeys[HardModePreset.Oblivion]) },
            { HardModePreset.NoEscape, LocalizationService.Get(PresetNameKeys[HardModePreset.NoEscape]) }
        };

        /// <summary>
        /// 硬模式预设的最终展示描述。
        /// </summary>
        public static readonly Dictionary<HardModePreset, string> PresetDescriptions = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, LocalizationService.Get(PresetDescriptionKeys[HardModePreset.Nightmare]) },
            { HardModePreset.Relentless, LocalizationService.Get(PresetDescriptionKeys[HardModePreset.Relentless]) },
            { HardModePreset.Oblivion, LocalizationService.Get(PresetDescriptionKeys[HardModePreset.Oblivion]) },
            { HardModePreset.NoEscape, LocalizationService.Get(PresetDescriptionKeys[HardModePreset.NoEscape]) }
        };

        /// <summary>
        /// 获取指定硬模式预设在当前语言下的最终展示名称，并同步公开名称缓存。
        /// </summary>
        /// <param name="preset">硬模式预设。</param>
        /// <returns>当前语言下的预设展示名称。</returns>
        public static string GetPresetName(HardModePreset preset)
        {
            string name = LocalizationService.Get(PresetNameKeys[preset]);
            PresetNames[preset] = name;
            return name;
        }

        /// <summary>
        /// 获取指定硬模式预设在当前语言下的最终展示描述，并同步公开描述缓存。
        /// </summary>
        /// <param name="preset">硬模式预设。</param>
        /// <returns>当前语言下的预设展示描述。</returns>
        public static string GetPresetDescription(HardModePreset preset)
        {
            string description = LocalizationService.Get(PresetDescriptionKeys[preset]);
            PresetDescriptions[preset] = description;
            return description;
        }

        /// <summary>
        /// 创建指定硬模式预设对应的难度数据。
        /// </summary>
        /// <param name="preset">硬模式预设。</param>
        /// <returns>已配置倍率和展示名称的难度数据。</returns>
        public static DifficultyData CreateDifficultyData(HardModePreset preset)
        {
            var data = new DifficultyData();

            data.difficultyName = GetPresetName(preset);
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
