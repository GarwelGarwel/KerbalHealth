using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class KerbalHealthRadiationSettings : GameParameters.CustomParameterNode
    {
    public override string Title { get { return "Radiation"; } }
    public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
    public override bool HasPresets { get { return true; } }
    public override string Section { get { return "Kerbal Health (2)"; } }
    public override string DisplaySection { get { return Section; } }
    public override int SectionOrder { get { return 1; } }

    public override void SetDifficultyPreset(GameParameters.Preset preset)
    {
        switch (preset)
        {
            case GameParameters.Preset.Easy:
                    RadiationEnabled = false;
                    RadiationEffect = 0.1f;
                    EVAExposure = 1;
                    break;
            case GameParameters.Preset.Normal:
                    RadiationEnabled = true;
                    RadiationEffect = 0.1f;
                    EVAExposure = 1;
                    break;
            case GameParameters.Preset.Moderate:
                    RadiationEnabled = true;
                    RadiationEffect = 0.1f;
                    EVAExposure = 3;
                    break;
            case GameParameters.Preset.Hard:
                    RadiationEnabled = true;
                    RadiationEffect = 0.25f;
                    EVAExposure = 10;
                    break;
        }
    }

        [GameParameters.CustomParameterUI("Radiation Enabled", toolTip = "Degrade max health based on accumulated dose")]
        public bool RadiationEnabled = true;

        [GameParameters.CustomFloatParameterUI("Radiation Effect", toolTip = "Percentage of max health drained by 1e7 (10M) doses. 0 to disable effect", minValue = 0, maxValue = 2, displayFormat = "N2", asPercentage = true, stepCount = 41)]
        public float RadiationEffect = 0.25f;

        [GameParameters.CustomFloatParameterUI("Landed Coefficient", toolTip = "How much cosmic radiation reaches the planetary surface (discounting atmospheric effects)", minValue = 0, maxValue = 0.2f, displayFormat = "N3", asPercentage = true, stepCount = 41)]
        public float LandedCoefficient = 0.05f;

        [GameParameters.CustomFloatParameterUI("Atmosphere Transparency", toolTip = "How much cosmic radiation penetrates atmospheres, only affects kerbals on the surface", minValue = 0, maxValue = 0.02f, displayFormat = "N3", asPercentage = true, stepCount = 21)]
        public float AtmoCoefficient = 0.01f;

        [GameParameters.CustomFloatParameterUI("Flying Coefficient", toolTip = "How much cosmic radiation reaches vessels flying in the air", minValue = 0, maxValue = 0.1f, displayFormat = "N4", asPercentage = true, stepCount = 41)]
        public float FlyingCoefficient = 0.03f;

        [GameParameters.CustomFloatParameterUI("In Space Low Coefficient", toolTip = "How much cosmic radiation reaches vessels in low planetary orbits", minValue = 0, maxValue = 0.5f, displayFormat = "N2", asPercentage = true, stepCount = 11)]
        public float InSpaceLowCoefficient = 0.10f;

        [GameParameters.CustomFloatParameterUI("In Space High Coefficient", toolTip = "", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float InSpaceHighCoefficient = 0.50f;

        [GameParameters.CustomFloatParameterUI("EVA Exposure Multiplier", toolTip = "How much more radiaiton kerbals receive when on EVA", minValue = 0, maxValue = 20, displayFormat = "N0", stepCount = 21)]
        public float EVAExposure = 10;

        [GameParameters.CustomFloatParameterUI("Solar Radiation (Nominal)", toolTip = "Solar radiation in interplanetary space at 1 AU, banana doses/day", minValue = 0, maxValue = 20000, displayFormat = "N0", stepCount = 21)]
        public float SolarRadiation = 5000;

        [GameParameters.CustomFloatParameterUI("Galactic Radiation", toolTip = "Galactic cosmic radiation in interplanetary space, banana doses/day", minValue = 0, maxValue = 20000, displayFormat = "N0", stepCount = 21)]
        public float GalacticRadiation = 5000;
    }
}
