using KSP.Localization;

namespace KerbalHealth
{
    class KerbalHealthFactorsSettings : GameParameters.CustomParameterNode
    {
        public override string Title => Localizer.Format("#KH_FS_title");//"Health Factors"

        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;

        public override bool HasPresets => true;

        public override string Section => "Kerbal Health (1)";

        public override string DisplaySection => Section;

        public override int SectionOrder => 2;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    LonelinessEffect = 0;
                    MicrogravityEffect = 0;
                    EVAEffect = 0;
                    IsolationEffect = 0;
                    break;

                case GameParameters.Preset.Normal:
                case GameParameters.Preset.Moderate:
                case GameParameters.Preset.Hard:
                    LonelinessEffect = 1;
                    MicrogravityEffect = 1;
                    EVAEffect = 1;
                    IsolationEffect = 1;
                    break;
            }
        }

        /// <summary>
        /// Reverts all settings to their mod-default values
        /// </summary>
        internal void Reset()
        {
            StressEffect = 1;
            ConfinementEffect = 1;
            LonelinessEffect = 1;
            MicrogravityEffect = 1;
            EVAEffect = 1;
            IsolationEffect = 1;
            HomeEffect = 1;
            KSCEffect = 1;
            TrainingEnabled = true;
            TrainingTime = 60;
            StupidityPenalty = 1;
            SetDifficultyPreset(HighLogic.CurrentGame.Parameters.preset);
        }

        /// <summary>
        /// Assigns settings defined in settingsNode
        /// </summary>
        /// <param name="settingsNode"></param>
        internal void ApplyConfig(ConfigNode settingsNode)
        {
            if (settingsNode == null)
                return;
            Core.Log($"Applying KerbalHealthFactorsSettings settings: {settingsNode}");
            settingsNode.TryGetValue("StressEffect", ref StressEffect);
            settingsNode.TryGetValue("ConfinementEffect", ref ConfinementEffect);
            settingsNode.TryGetValue("LonelinessEffect", ref LonelinessEffect);
            settingsNode.TryGetValue("EVAEffect", ref EVAEffect);
            settingsNode.TryGetValue("MicrogravityEffect", ref MicrogravityEffect);
            settingsNode.TryGetValue("IsolationEffect", ref IsolationEffect);
            settingsNode.TryGetValue("HomeEffect", ref HomeEffect);
            settingsNode.TryGetValue("KSCEffect", ref KSCEffect);
            settingsNode.TryGetValue("TrainingEnabled", ref TrainingEnabled);
            settingsNode.TryGetValue("TrainingTime", ref TrainingTime);
            settingsNode.TryGetValue("StupidityPenalty", ref StupidityPenalty);
        }

        public static KerbalHealthFactorsSettings Instance => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>();

        [GameParameters.CustomFloatParameterUI("#KH_Factor_Stress", toolTip = "#KH_FS_Stress_desc", minValue = 0, maxValue = 2, displayFormat = "N1", asPercentage = true, stepCount = 21)]
        public float StressEffect = 1;

        [GameParameters.CustomFloatParameterUI("#KH_Factor_Confinement", toolTip = "#KH_FS_Confinement_desc", minValue = 0, maxValue = 2, displayFormat = "N1", asPercentage = true, stepCount = 21)]
        public float ConfinementEffect = 1;

        [GameParameters.CustomFloatParameterUI("#KH_Factor_Loneliness", toolTip = "#KH_FS_Loneliness_desc", minValue = 0, maxValue = 2, displayFormat = "N1", asPercentage = true, stepCount = 21)]
        public float LonelinessEffect = 1;

        [GameParameters.CustomFloatParameterUI("#KH_Factor_Microgravity", toolTip = "#KH_FS_Microgravity_desc", minValue = 0, maxValue = 2, displayFormat = "N1", asPercentage = true, stepCount = 21)]
        public float MicrogravityEffect = 1;

        [GameParameters.CustomFloatParameterUI("#KH_Factor_EVA", toolTip = "#KH_FS_EVA_desc", minValue = 0, maxValue = 2, displayFormat = "N1", asPercentage = true, stepCount = 21)]
        public float EVAEffect = 1;

        [GameParameters.CustomFloatParameterUI("#KH_Factor_Isolation", toolTip = "#KH_Factor_Isolation_desc", minValue = 0, maxValue = 2, displayFormat = "N1", asPercentage = true, stepCount = 21)]
        public float IsolationEffect = 1;

        [GameParameters.CustomFloatParameterUI("#KH_Factor_Home", toolTip = "#KH_FS_Home_desc", minValue = 0, maxValue = 2, displayFormat = "N1", asPercentage = true, stepCount = 21)]
        public float HomeEffect = 1;

        [GameParameters.CustomFloatParameterUI("#KH_Factor_KSC", toolTip = "#KH_FS_KSC_desc", minValue = 0, maxValue = 2, displayFormat = "N1", asPercentage = true, stepCount = 21)]
        public float KSCEffect = 1;

        [GameParameters.CustomParameterUI("#KH_FS_TrainingEnabled", toolTip = "#KH_FS_TrainingEnabled_desc")]//Training Enabled""Turn on/off the need to train kerbals to reduce stress
        public bool TrainingEnabled = true;

        [GameParameters.CustomIntParameterUI("#KH_FS_TrainingTime", toolTip = "#KH_FS_TrainingTime_desc", minValue = 5, maxValue = 200, stepSize = 5)]//KSC Training Time""Min # of days it takes to train kerbal to max level at KSC
        public int TrainingTime = 60;

        [GameParameters.CustomFloatParameterUI("#KH_FS_StupidityPenalty", toolTip = "#KH_FS_StupidityPenalty_desc", displayFormat = "N1", asPercentage = true, minValue = 0, maxValue = 2, stepCount = 21)]//Stupidity Penalty""How much longer it takes to train a stupid kerbal compared to a smart one
        public float StupidityPenalty = 0;
    }
}
