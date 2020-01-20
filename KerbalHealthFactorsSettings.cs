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

        [GameParameters.CustomFloatParameterUI("Stress", toolTip = "HP change per day when the kerbal is assigned; can be lowered through training and/or upgrading Astronaut Complex", minValue = -20, maxValue = 0, displayFormat = "F1", stepCount = 41)]
        public float StressFactor = -2;

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
        public float KSCFactor = 4;

        [GameParameters.CustomParameterUI("Training Enabled", toolTip = "Turn on/off the need to train kerbals to reduce stress")]
        public bool TrainingEnabled = true;

        [GameParameters.CustomIntParameterUI("KSC Training Time", toolTip = "Min # of days it takes to train kerbal to max level at KSC", minValue = 1, maxValue = 50)]
        public int KSCTrainingTime = 20;

        [GameParameters.CustomIntParameterUI("In-Flight Training Time", toolTip = "Min # of days it takes to train kerbal to max level during a mission", minValue = 1, maxValue = 100)]
        public int InFlightTrainingTime = 50;

        [GameParameters.CustomFloatParameterUI("Stupidity Penalty", toolTip = "How much longer it takes to train a stupid kerbal compared to a smart one", displayFormat = "N2", asPercentage = true, minValue = 0, maxValue = 2, stepCount = 21)]
        public float StupidityPenalty = 0;
    }
}
