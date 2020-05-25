using KSP.Localization;

namespace KerbalHealth
{
    class KerbalHealthFactorsSettings : GameParameters.CustomParameterNode
    {
        public override string Title => Localizer.Format("#KH_FS_title");//"Health Factors"
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => true;
        public override string Section => "Kerbal Health";
        public override string DisplaySection => Section;
        public override int SectionOrder => 2;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    LonelinessFactor = 0;
                    MicrogravityFactor = 0;
                    EVAFactor = 0;
                    ConnectedFactor = 0;
                    break;

                case GameParameters.Preset.Normal:
                    LonelinessFactor = -1;
                    MicrogravityFactor = -1;
                    EVAFactor = -10;
                    ConnectedFactor = 0.5f;
                    break;

                case GameParameters.Preset.Moderate:
                    LonelinessFactor = -1;
                    MicrogravityFactor = -1;
                    EVAFactor = -10;
                    ConnectedFactor = 0.5f;
                    break;

                case GameParameters.Preset.Hard:
                    LonelinessFactor = -1;
                    MicrogravityFactor = -1;
                    EVAFactor = -10;
                    ConnectedFactor = 0.5f;
                    break;
            }
        }

        /// <summary>
        /// Reverts all settings to their mod-default values
        /// </summary>
        internal void Reset()
        {
            StressFactor = -2;
            ConfinementBaseFactor = -3;
            LonelinessFactor = -1;
            MicrogravityFactor = -1;
            EVAFactor = -10;
            ConnectedFactor = 0.5f;
            HomeFactor = 2;
            KSCFactor = 4;
            TrainingEnabled = true;
            KSCTrainingTime = 40;
            InFlightTrainingTime = 100;
            FamiliarityBonus = 0.5f;
            StupidityPenalty = 0;
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
            Core.Log("Applying KerbalHealthFactorsSettings settings: " + settingsNode);
            settingsNode.TryGetValue("StressFactor", ref StressFactor);
            settingsNode.TryGetValue("ConfinementBaseFactor", ref ConfinementBaseFactor);
            settingsNode.TryGetValue("LonelinessFactor", ref LonelinessFactor);
            settingsNode.TryGetValue("EVAFactor", ref EVAFactor);
            settingsNode.TryGetValue("MicrogravityFactor", ref MicrogravityFactor);
            settingsNode.TryGetValue("ConnectedFactor", ref ConnectedFactor);
            settingsNode.TryGetValue("HomeFactor", ref HomeFactor);
            settingsNode.TryGetValue("KSCFactor", ref KSCFactor);
            settingsNode.TryGetValue("TrainingEnabled", ref TrainingEnabled);
            settingsNode.TryGetValue("KSCTrainingTime", ref KSCTrainingTime);
            settingsNode.TryGetValue("InFlightTrainingTime", ref InFlightTrainingTime);
            settingsNode.TryGetValue("FamiliarityBonus", ref FamiliarityBonus);
            settingsNode.TryGetValue("StupidityPenalty", ref StupidityPenalty);
        }

        public static KerbalHealthFactorsSettings Instance => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>();

        [GameParameters.CustomFloatParameterUI("#KH_Stress", toolTip = "#KH_FS_Stress_desc", minValue = -20, maxValue = 0, displayFormat = "F1", stepCount = 41)] //Stress""HP change per day when the kerbal is assigned; can be lowered through training and/or upgrading Astronaut Complex
        public float StressFactor = -2;

        [GameParameters.CustomFloatParameterUI("#KH_Confinement", toolTip = "#KH_FS_Confinement_desc", minValue = -10, maxValue = 0, stepCount = 41)]//Confinement""HP change per day in a vessel with 1 living space per kerbal
        public float ConfinementBaseFactor = -3;

        [GameParameters.CustomFloatParameterUI("#KH_Loneliness", toolTip = "#KH_FS_Loneliness_desc", minValue = -20, maxValue = 0, stepCount = 41)]//Loneliness""HP change per day when the kerbal has no crewmates
        public float LonelinessFactor = -1;

        [GameParameters.CustomFloatParameterUI("#KH_Microgravity", toolTip = "#KH_FS_Microgravity_desc", minValue = -20, maxValue = 0, displayFormat = "F1", stepCount = 41)]//Microgravity""HP change per day when in orbital/suborbital flight or g-force < 0.1
        public float MicrogravityFactor = -1;

        [GameParameters.CustomFloatParameterUI("#KH_EVA", toolTip = "#KH_FS_EVA_desc", minValue = -50, maxValue = 0, stepCount = 26)]//EVA""HP change per day when on EVA
        public float EVAFactor = -10;

        [GameParameters.CustomFloatParameterUI("#KH_Connected", toolTip = "#KH_FS_Connected_desc", minValue = 0, maxValue = 20, displayFormat = "F1", stepCount = 41)]//Connected""HP change per day when connected to Kerbin
        public float ConnectedFactor = 0.5f;

        [GameParameters.CustomFloatParameterUI("#KH_Home", toolTip = "#KH_FS_Home_desc", minValue = 0, maxValue = 20, stepCount = 41)]//Home""HP change per day when in Kerbin atmosphere
        public float HomeFactor = 2;

        [GameParameters.CustomFloatParameterUI("#KH_KSC", toolTip = "#KH_FS_KSC_desc", minValue = 0, maxValue = 20, stepCount = 41)]//At KSC""HP change per day when the kerbal is at KSC (available)
        public float KSCFactor = 4;

        [GameParameters.CustomParameterUI("#KH_FS_TrainingEnabled", toolTip = "#KH_FS_TrainingEnabled_desc")]//Training Enabled""Turn on/off the need to train kerbals to reduce stress
        public bool TrainingEnabled = true;

        [GameParameters.CustomIntParameterUI("#KH_FS_KSCTrainingTime", toolTip = "#KH_FS_KSCTrainingTime_desc", minValue = 1, maxValue = 50)]//KSC Training Time""Min # of days it takes to train kerbal to max level at KSC
        public int KSCTrainingTime = 40;

        [GameParameters.CustomIntParameterUI("#KH_FS_InFlightTrainingTime", toolTip = "#KH_FS_InFlightTrainingTime_desc", minValue = 1, maxValue = 100)] //In-Flight Training Time""Min # of days it takes to train kerbal to max level during a mission
        public int InFlightTrainingTime = 100;

        [GameParameters.CustomFloatParameterUI("#KH_FS_FamiliarityBonus", toolTip = "KH_FS_FamiliarityBonus_desc", displayFormat = "N2", asPercentage = true, minValue = 0, maxValue = 1, stepCount = 11)]//Stupidity Penalty""How much longer it takes to train a stupid kerbal compared to a smart one
        public float FamiliarityBonus = 0.5f;

        [GameParameters.CustomFloatParameterUI("#KH_FS_StupidityPenalty", toolTip = "#KH_FS_StupidityPenalty_desc", displayFormat = "N2", asPercentage = true, minValue = 0, maxValue = 2, stepCount = 21)]//Stupidity Penalty""How much longer it takes to train a stupid kerbal compared to a smart one
        public float StupidityPenalty = 0;
    }
}
