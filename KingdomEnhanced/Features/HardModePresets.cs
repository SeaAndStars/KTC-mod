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
        /// 硬模式预设的稳定英文名称。
        /// </summary>
        public static readonly Dictionary<HardModePreset, string> PresetNames = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, "Nightmare" },
            { HardModePreset.Relentless, "Relentless" },
            { HardModePreset.Oblivion, "Oblivion" },
            { HardModePreset.NoEscape, "No Escape" }
        };

        /// <summary>
        /// 硬模式预设的稳定英文描述。
        /// </summary>
        public static readonly Dictionary<HardModePreset, string> PresetDescriptions = new Dictionary<HardModePreset, string>
        {
            { HardModePreset.Nightmare, "Massive waves of enemies. Prepare your defenses." },
            { HardModePreset.Relentless, "Nights are longer. Enemies never retreat." },
            { HardModePreset.Oblivion, "The Blood Moon rises often. Retaliation is swift." },
            { HardModePreset.NoEscape, "Enemies are stronger, faster, and deadlier." }
        };

        /// <summary>
        /// 获取指定硬模式预设在当前语言下的最终展示名称。
        /// </summary>
        /// <param name="preset">硬模式预设。</param>
        /// <returns>当前语言下的预设展示名称。</returns>
        public static string GetPresetName(HardModePreset preset)
        {
            string name = LocalizationService.Get(PresetNameKeys[preset]);
            return name;
        }

        /// <summary>
        /// 获取指定硬模式预设在当前语言下的最终展示描述。
        /// </summary>
        /// <param name="preset">硬模式预设。</param>
        /// <returns>当前语言下的预设展示描述。</returns>
        public static string GetPresetDescription(HardModePreset preset)
        {
            string description = LocalizationService.Get(PresetDescriptionKeys[preset]);
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
