namespace KerbalHealth
{
    class KerbalHealthFactorsSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "Health Factors";
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
                    AssignedFactor = -0.5f;
                    LonelinessFactor = 0;
                    MicrogravityFactor = 0;
                    EVAFactor = 0;
                    ConnectedFactor = 0;
                    HomeFactor = 5;
                    KSCFactor = 10;
                    break;
                case GameParameters.Preset.Normal:
                    AssignedFactor = -0.5f;
                    LonelinessFactor = -1;
                    MicrogravityFactor = -1;
                    EVAFactor = -5;
                    ConnectedFactor = 0.5f;
                    HomeFactor = 2;
                    KSCFactor = 4;
                    break;
                case GameParameters.Preset.Moderate:
                    AssignedFactor = -0.5f;
                    LonelinessFactor = -1;
                    MicrogravityFactor = -1;
                    EVAFactor = -10;
                    ConnectedFactor = 0.5f;
                    HomeFactor = 2;
                    KSCFactor = 4;
                    break;
                case GameParameters.Preset.Hard:
                    AssignedFactor = -0.5f;
                    LonelinessFactor = -1;
                    MicrogravityFactor = -1;
                    EVAFactor = -10;
                    ConnectedFactor = 0.5f;
                    HomeFactor = 2;
                    KSCFactor = 4;
                    break;
            }
        }

        [GameParameters.CustomFloatParameterUI("Assigned", toolTip = "HP change per day when the kerbal is assigned", minValue = -20, maxValue = 0, displayFormat = "F1", stepCount = 41)]
        public float AssignedFactor = -0.5f;

        [GameParameters.CustomFloatParameterUI("Confinement", toolTip = "HP change per day in a vessel with 1 living space per kerbal", minValue = -10, maxValue = 0, stepCount = 41)]
        public float ConfinementBaseFactor = -3;

        [GameParameters.CustomFloatParameterUI("Loneliness", toolTip = "HP change per day when the kerbal has no crewmates", minValue = -20, maxValue = 0, stepCount = 41)]
        public float LonelinessFactor = -1;

        [GameParameters.CustomFloatParameterUI("Microgravity", toolTip = "HP change per day when in orbital/suborbital flight or g-force < 0.1", minValue = -20, maxValue = 0, displayFormat = "F1", stepCount = 41)]
        public float MicrogravityFactor = -1;

        [GameParameters.CustomFloatParameterUI("EVA", toolTip = "HP change per day when on EVA", minValue = -50, maxValue = 0, stepCount = 26)]
        public float EVAFactor = -10;

        [GameParameters.CustomFloatParameterUI("Connected", toolTip = "HP change per day when connected to Kerbin", minValue = 0, maxValue = 20, displayFormat = "F1", stepCount = 41)]
        public float ConnectedFactor = 0.5f;

        [GameParameters.CustomFloatParameterUI("Home", toolTip = "HP change per day when in Kerbin atmosphere", minValue = 0, maxValue = 20, stepCount = 41)]
        public float HomeFactor = 2;

        [GameParameters.CustomFloatParameterUI("At KSC", toolTip = "HP change per day when the kerbal is at KSC (available)", minValue = 0, maxValue = 20, stepCount = 41)]
        public float KSCFactor = 5;

        [GameParameters.CustomParameterUI("Training Enabled", toolTip = "Turn on/off the need to train kerbals to reduce stress")]
        public bool TrainingEnabled = true;


    }
}
