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

        [GameParameters.CustomFloatParameterUI("#KH_FS_Assigned", toolTip = "#KH_FS_Assigned_desc", minValue = -20, maxValue = 0, displayFormat = "F1", stepCount = 41)]//Assigned""HP change per day when the kerbal is assigned
        public float AssignedFactor = -0.5f;

        [GameParameters.CustomFloatParameterUI("#KH_FS_Confinement", toolTip = "#KH_FS_Confinement_desc", minValue = -10, maxValue = 0, stepCount = 41)]//Confinement""HP change per day in a vessel with 1 living space per kerbal
        public float ConfinementBaseFactor = -3;

        [GameParameters.CustomFloatParameterUI("#KH_FS_Loneliness", toolTip = "#KH_FS_Loneliness_desc", minValue = -20, maxValue = 0, stepCount = 41)]//Loneliness""HP change per day when the kerbal has no crewmates
        public float LonelinessFactor = -1;

        [GameParameters.CustomFloatParameterUI("#KH_FS_Microgravity", toolTip = "#KH_FS_Microgravity_desc", minValue = -20, maxValue = 0, displayFormat = "F1", stepCount = 41)]//Microgravity""HP change per day when in orbital/suborbital flight or g-force < 0.1
        public float MicrogravityFactor = -1;

        [GameParameters.CustomFloatParameterUI("#KH_FS_EVA", toolTip = "#KH_FS_EVA_desc", minValue = -50, maxValue = 0, stepCount = 26)]//EVA""HP change per day when on EVA
        public float EVAFactor = -10;

        [GameParameters.CustomFloatParameterUI("#KH_FS_Connected", toolTip = "#KH_FS_Connected_desc", minValue = 0, maxValue = 20, displayFormat = "F1", stepCount = 41)]//Connected""HP change per day when connected to Kerbin
        public float ConnectedFactor = 0.5f;

        [GameParameters.CustomFloatParameterUI("#KH_FS_Home", toolTip = "#KH_FS_Home_desc", minValue = 0, maxValue = 20, stepCount = 41)]//Home""HP change per day when in Kerbin atmosphere
        public float HomeFactor = 2;

        [GameParameters.CustomFloatParameterUI("#KH_FS_KSC", toolTip = "#KH_FS_KSC_desc", minValue = 0, maxValue = 20, stepCount = 41)]//At KSC""HP change per day when the kerbal is at KSC (available)
        public float KSCFactor = 5;
    }
}
