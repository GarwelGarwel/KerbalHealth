using KSP.Localization;
namespace KerbalHealth
{
    class KerbalHealthQuirkSettings : GameParameters.CustomParameterNode
    {
        public override string Title => Localizer.Format("#KH_QS_title");//"Conditions & Quirks"
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => true;
        public override string Section => "Kerbal Health";
        public override string DisplaySection => Section;
        public override int SectionOrder => 3;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    ConditionsEnabled = false;
                    break;
                case GameParameters.Preset.Normal:
                    ConditionsEnabled = true;
                    ConditionsChance = 0.5f;
                    ConditionsEffect = 0.5f;
                    break;
                case GameParameters.Preset.Moderate:
                case GameParameters.Preset.Hard:
                    ConditionsEnabled = true;
                    ConditionsChance = 1;
                    ConditionsEffect = 1;
                    break;
            }
        }

        [GameParameters.CustomParameterUI("#KH_QS_ConditionsEnable", toolTip = "#KH_QS_ConditionsEnabled_desc")]//Conditions Enabled""If checked, special health conditions affect health and can randomly appear in kerbals
        public bool ConditionsEnabled = true;

        [GameParameters.CustomParameterUI("#KH_QS_KSCNotificationsEnabled", toolTip = "#KH_QS_KSCNotificationsEnabled_desc")]//Notify of Events in KSC""If checked, notifications will be given of health condition-related events with kerbals not on mission
        public bool KSCNotificationsEnabled = false;

        [GameParameters.CustomFloatParameterUI("#KH_QS_ConditionsChance", toolTip = "#KH_QS_ConditionsChance_desc", minValue = 0, maxValue = 3, displayFormat = "N2", asPercentage = true, stepCount = 31)]//Relative Conditions Chances""Relative chance of acquiring a random condition
        public float ConditionsChance = 1;

        [GameParameters.CustomFloatParameterUI("#KH_QS_ConditionsEffect", toolTip = "#KH_QS_ConditionsEffect_desc", minValue = 0, maxValue = 3, displayFormat = "N2", asPercentage = true, stepCount = 31)]//Conditions Health Effect""Relative effect of conditions on health (the lower the easier)
        public float ConditionsEffect = 1;

        [GameParameters.CustomParameterUI("#KH_QS_QuirksEnabled", toolTip = "#KH_QS_QuirksEnabled_desc")]//Quirks Enabled""Quirks can be awarded to kerbals and affect their health stats
        public bool QuirksEnabled = true;

        [GameParameters.CustomIntParameterUI("#KH_QS_MaxQuirks", toolTip = "#KH_QS_MaxQuirks_desc", minValue = 0, maxValue = 5, displayFormat = "N0", stepSize = 1)]//Max Quirks""Maximum number of level-up quirks for a kerbal
        public int MaxQuirks = 2;

        [GameParameters.CustomFloatParameterUI("#KH_QS_QuirkChance", toolTip = "#KH_QS_QuirkChance_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]//Level-Up Quirk Chance""Chance of a kerbal being awarded a quirk when he/she levels up
        public float QuirkChance = 0.25f;

        [GameParameters.CustomParameterUI("#KH_QS_AwardQuirksOnMissions", toolTip = "#KH_QS_AwardQuirksOnMissions_desc")]//Award during Missions""Level-up quirks can be awarded when the kerbal is assigned, otherwise only at KSC
        public bool AwardQuirksOnMissions = false;

        [GameParameters.CustomFloatParameterUI("#KH_QS_AnomalyQuirkChance", toolTip = "#KH_QS_AnomalyQuirkChance_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]//Anomaly Quirk Chance""Chance of a kerbal being awarded a quirk when he/she discovers an anomaly
        public float AnomalyQuirkChance = 1;

        [GameParameters.CustomParameterUI("#KH_QS_StatsAffectQuirkWeights", toolTip = "#KH_QS_StatsAffectQuirkWeights_desc")]//Kerbal Stats Affect Quirk Weights""Chances of getting some quirks depend on Courage and Stupidity of the kerbal
        public bool StatsAffectQuirkWeights = true;
    }
}
