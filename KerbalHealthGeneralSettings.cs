using KSP.Localization;
namespace KerbalHealth
{
    class KerbalHealthGeneralSettings : GameParameters.CustomParameterNode
    {
        public override string Title => Localizer.Format("#KH_GS_title");//"General Settings"
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
                    DeathEnabled = false;
                    ExhaustionStartHealth = 0.1f;
                    ExhaustionEndHealth = 0.15f;
                    break;
                case GameParameters.Preset.Normal:
                    DeathEnabled = false;
                    ExhaustionStartHealth = 0.2f;
                    ExhaustionEndHealth = 0.25f;
                    break;
                case GameParameters.Preset.Moderate:
                case GameParameters.Preset.Hard:
                    DeathEnabled = true;
                    ExhaustionStartHealth = 0.2f;
                    ExhaustionEndHealth = 0.25f;
                    break;
            }
        }

        [GameParameters.CustomParameterUI("#KH_GS_modEnabled", toolTip = "#KH_GS_modEnabled_desc")]//Mod Enabled""Turn Kerbal Health mechanics on/off
        public bool modEnabled = true;

        [GameParameters.CustomParameterUI("#KH_GS_UseToolbar", toolTip = "#KH_GS_UseToolbar_desc")]//Use Blizzy's Toolbar""Use Blizzy's Toolbar mod (is installed) instead of stock app launcher. May need a scene change
        public bool UseBlizzysToolbar = true;

        [GameParameters.CustomIntParameterUI("#KH_GS_SortByLocation", toolTip = "#KH_GS_SortByLocation_desc")]//Sort Kerbals by Location""Kerbals in Health Monitor will be displayed depending on their current location, otherwise sort by name
        public bool SortByLocation = true;

        [GameParameters.CustomIntParameterUI("#KH_GS_LinesPerPage", toolTip = "#KH_GS_LinesPerPage_desc", minValue = 5, maxValue = 20, stepSize = 5)]//Lines per Page in Health Monitor""How many kerbals to show on one page of Health Monitor
        public int LinesPerPage = 10;

        [GameParameters.CustomFloatParameterUI("#KH_GS_UpdateInterval", toolTip = "#KH_GS_UpdateInterval_desc", minValue = 0.04f, maxValue = 60)]//Update Interval""Number of GAME seconds between health updates\nDoesn't affect health rates. Increase if performance too slow
        public float UpdateInterval = 10;

        [GameParameters.CustomFloatParameterUI("#KH_GS_MinUpdateInterval", toolTip = "#KH_GS_MinUpdateInterval_desc", minValue = 0.04f, maxValue = 60)]//Minimum Update Interval""Minimum number of REAL seconds between updated on high time warp\nMust be <= Update Interval
        public float MinUpdateInterval = 1;

        [GameParameters.CustomFloatParameterUI("#KH_GS_BaseMaxHP", toolTip = "#KH_GS_BaseMaxHP_desc", minValue = 10, maxValue = 200, stepCount = 20)]//Base Max HP""Max number of Health Points for 0-star kerbals
        public float BaseMaxHP = 100;

        [GameParameters.CustomFloatParameterUI("#KH_GS_HPPerLevel", toolTip = "#KH_GS_HPPerLevel_desc", minValue = 0, maxValue = 50, stepCount = 51)]//HP per Level""Health Points increase per level (star) of a kerbal
        public float HPPerLevel = 10;

        [GameParameters.CustomFloatParameterUI("#KH_GS_LowHealthAlert", toolTip = "#KH_GS_LowHealthAlert_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]//Low Health Alert""Health level when a low health alert is shown
        public float LowHealthAlert = 0.3f;

        [GameParameters.CustomParameterUI("#KH_GS_DeathEnabled", toolTip = "#KH_GS_DeathEnabled_desc")]//Death Enabled""Allow kerbals to die of poor health
        public bool DeathEnabled = true;

        [GameParameters.CustomFloatParameterUI("#KH_GS_ExhaustionStartHealth", toolTip = "#KH_GS_ExhaustionStartHealth_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]//Exhaustion Start Health""Health level when kerbals turn Exhausted (become Tourists)
        public float ExhaustionStartHealth = 0.2f;

        [GameParameters.CustomFloatParameterUI("#KH_GS_ExhaustionEndHealth", toolTip = "#KH_GS_ExhaustionEndHealth_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]//Exhaustion End Health""Health level when kerbals leave Exhausted state (must be greater than or equal to Exhaustion start)
        public float ExhaustionEndHealth = 0.25f;

        [GameParameters.CustomParameterUI("#KH_GS_DebugMode", toolTip = "#KH_GS_DebugMode_desc")]//Debug Mode""Controls amount of logging
        public bool DebugMode = false;
    }
}
