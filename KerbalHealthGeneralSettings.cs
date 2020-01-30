namespace KerbalHealth
{
    class KerbalHealthGeneralSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "General Settings";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => true;
        public override string Section => "Kerbal Health";
        public override string DisplaySection => Section;
        public override int SectionOrder => 1;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                case GameParameters.Preset.Normal:
                    DeathEnabled = false;
                    break;
                case GameParameters.Preset.Moderate:
                case GameParameters.Preset.Hard:
                    DeathEnabled = true;
                    break;
            }
        }

        [GameParameters.CustomParameterUI("Mod Enabled", toolTip = "Turn Kerbal Health mechanics on/off")]
        public bool modEnabled = true;

        [GameParameters.CustomParameterUI("Use Blizzy's Toolbar", toolTip = "Use Blizzy's Toolbar mod (is installed) instead of stock app launcher. May need a scene change")]
        public bool UseBlizzysToolbar = true;

        [GameParameters.CustomIntParameterUI("Sort Kerbals by Location", toolTip = "Kerbals in Health Monitor will be displayed depending on their current location, otherwise sort by name")]
        public bool SortByLocation = true;

        [GameParameters.CustomIntParameterUI("Lines per Page in Health Monitor", toolTip = "How many kerbals to show on one page of Health Monitor", minValue = 5, maxValue = 20, stepSize = 5)]
        public int LinesPerPage = 10;

        [GameParameters.CustomFloatParameterUI("Update Interval", toolTip = "Number of GAME seconds between health updates\nDoesn't affect health rates. Increase if performance too slow", minValue = 0.04f, maxValue = 60)]
        public float UpdateInterval = 10;

        [GameParameters.CustomFloatParameterUI("Minimum Update Interval", toolTip = "Minimum number of REAL seconds between updated on high time warp\nMust be <= Update Interval", minValue = 0.04f, maxValue = 60)]
        public float MinUpdateInterval = 1;

        [GameParameters.CustomFloatParameterUI("Base Max HP", toolTip = "Max number of Health Points for 0-star kerbals", minValue = 10, maxValue = 200, stepCount = 20)]
        public float BaseMaxHP = 100;

        [GameParameters.CustomFloatParameterUI("HP per Level", toolTip = "Health Points increase per level (star) of a kerbal", minValue = 0, maxValue = 50, stepCount = 51)]
        public float HPPerLevel = 10;

        [GameParameters.CustomFloatParameterUI("Low Health Alert", toolTip = "Health level when a low health alert is shown", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float LowHealthAlert = 0.3f;

        [GameParameters.CustomParameterUI("Death Enabled", toolTip = "Allow kerbals to die of poor health")]
        public bool DeathEnabled = true;

        [GameParameters.CustomFloatParameterUI("Exhaustion Start Health", toolTip = "Health level when kerbals turn Exhausted (become Tourists)", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float ExhaustionStartHealth = 0.2f;

        [GameParameters.CustomFloatParameterUI("Exhaustion End Health", toolTip = "Health level when kerbals leave Exhausted state (must be greater than or equal to Exhaustion start)", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float ExhaustionEndHealth = 0.25f;

        [GameParameters.CustomParameterUI("Debug Logging", toolTip = "Controls amount of logging")]
        public bool DebugMode = false;
    }
}
