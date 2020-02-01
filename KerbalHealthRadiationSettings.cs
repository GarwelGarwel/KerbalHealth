namespace KerbalHealth
{
    class KerbalHealthRadiationSettings : GameParameters.CustomParameterNode
    {
    public override string Title => "Radiation";
    public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
    public override bool HasPresets => true;
    public override string Section => "Kerbal Health (2)";
    public override string DisplaySection => Section;
    public override int SectionOrder => 1;

    public override void SetDifficultyPreset(GameParameters.Preset preset)
    {
        switch (preset)
        {
            case GameParameters.Preset.Easy:
                    RadiationEnabled = false;
                    ShieldingEffect = 2;
                    break;
            case GameParameters.Preset.Normal:
                    RadiationEnabled = true;
                    ShieldingEffect = 1;
                    break;
            case GameParameters.Preset.Moderate:
                    RadiationEnabled = true;
                    ShieldingEffect = 1;
                    break;
            case GameParameters.Preset.Hard:
                    RadiationEnabled = true;
                    ShieldingEffect = 1;
                    break;
        }
    }

        [GameParameters.CustomParameterUI("Radiation Enabled", toolTip = "Degrade max health based on accumulated dose")]
        public bool RadiationEnabled = true;

        [GameParameters.CustomFloatParameterUI("Radiation Effect", toolTip = "Percentage of max health drained by 1e7 (10M) doses. 0 to disable effect", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 41)]
        public float RadiationEffect = 0.1f;

        [GameParameters.CustomFloatParameterUI("Shielding Multiplier", toolTip = "Efficiency of radiation shielding provided by parts and resources", minValue = 0, maxValue = 2, displayFormat = "N2", asPercentage = true, stepCount = 41)]
        public float ShieldingEffect = 1;

        [GameParameters.CustomFloatParameterUI("In Space High Coefficient", toolTip = "How much cosmic radiation reaches vessels in high planetary orbit or on moons", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float InSpaceHighCoefficient = 0.30f;

        [GameParameters.CustomFloatParameterUI("In Space Low Coefficient", toolTip = "How much cosmic radiation reaches vessels in low planetary orbits", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float InSpaceLowCoefficient = 0.20f;

        [GameParameters.CustomFloatParameterUI("Stratosphere Transparency", toolTip = "How much cosmic radiation reaches outer layers of the atmosphere from space", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float StratoCoefficient = 0.2f;

        [GameParameters.CustomFloatParameterUI("Troposphere Transparency", toolTip = "How much cosmic radiation reaches the ground and lower layers of the atmosphere from space", minValue = 0, maxValue = 0.05f, displayFormat = "N4", asPercentage = true, stepCount = 51)]
        public float TroposphereCoefficient = 0.01f;

        [GameParameters.CustomFloatParameterUI("EVA Exposure Multiplier", toolTip = "How much more radiaiton kerbals receive when on EVA", minValue = 0, maxValue = 10, displayFormat = "N0", stepCount = 21)]
        public float EVAExposure = 5;

        [GameParameters.CustomFloatParameterUI("Solar Radiation (Nominal)", toolTip = "Solar radiation and base radiation storm intensity in interplanetary space at 1 AU, banana doses/day", minValue = 0, maxValue = 5000, displayFormat = "N0", stepCount = 21)]
        public float SolarRadiation = 2500;

        [GameParameters.CustomFloatParameterUI("Galactic Radiation", toolTip = "Galactic cosmic radiation in interplanetary space, banana doses/day", minValue = 0, maxValue = 30000, displayFormat = "N0", stepCount = 31)]
        public float GalacticRadiation = 12500;

        [GameParameters.CustomFloatParameterUI("Decontamination Rate per Day", toolTip = "How much radiation is lost per day during decontamination", minValue = 1000, maxValue = 1000000, displayFormat = "N0", logBase = 10)]
        public float DecontaminationRate = 100000;

        [GameParameters.CustomFloatParameterUI("Decontamination Health Loss", toolTip = "How much health is lost while the kerbal is decontaminating", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true)]
        public float DecontaminationHealthLoss = 0.75f;

        [GameParameters.CustomFloatParameterUI("Decontamination Funds Cost", toolTip = "How much Funds each decontamination procedure costs (Career only)", minValue = 0, maxValue = 1000000, displayFormat = "N0")]
        public float DecontaminationFundsCost = 100000;

        [GameParameters.CustomFloatParameterUI("Decontamination Science Cost", toolTip = "How much Science each decontamination procedure costs (Career & Science modes)", minValue = 0, maxValue = 10000, displayFormat = "N0")]
        public float DecontaminationScienceCost = 1000;

        [GameParameters.CustomIntParameterUI("Astronaut Complex Level for Decon", toolTip = "Min level of the Astronaut Complex for Decontamination", minValue = 0, maxValue = 3)]
        public int DecontaminationAstronautComplexLevel = 3;

        [GameParameters.CustomIntParameterUI("R&D Level for Decon", toolTip = "Min level of the Research & Development Facility for Decontamination", minValue = 0, maxValue = 3)]
        public int DecontaminationRNDLevel = 3;
    }
}
