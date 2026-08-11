using System;

namespace KingdomEnhanced.Features
{
    /// <summary>
    /// Provides access to the currently active hard mode preset for the campaign.
    /// </summary>
    public static class HardModeFeature
    {
        /// <summary>Returns the hard mode preset mapped from the current campaign's difficulty level.</summary>
        public static HardModePreset GetActivePreset()
        {
            if (CampaignSaveData.current != null)
            {
                var diff = CampaignSaveData.current.difficultyLevel;
                var preset = (HardModePreset)(int)diff;
                if (Enum.IsDefined(typeof(HardModePreset), preset))
                {
                    return preset;
                }
            }
            return HardModePreset.None;
        }
    }
}
