using KSP.Localization;

namespace KerbalHealth
{
    class KerbalHealthRadiationSettings : GameParameters.CustomParameterNode
    {
        public override string Title => Localizer.Format("#KH_RS_title");//"Radiation"
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
                    RadStormsEnabled = false;
                    RequireUpgradedFacilityForDecontamination = false;
                    break;

                case GameParameters.Preset.Normal:
                case GameParameters.Preset.Moderate:
                case GameParameters.Preset.Hard:
                    RadiationEnabled = true;
                    ShieldingEffect = 1;
                    RadStormsEnabled = true;
                    RequireUpgradedFacilityForDecontamination = true;
                    break;
            }
        }

        /// <summary>
        /// Reverts all settings to their mod-default values
        /// </summary>
        internal void Reset()
        {
            RadiationEnabled = true;
            RadiationEffect = 0.1f;
            UseKerbalismRadiation = false;
            KerbalismRadiationRatio = 0.25f;
            ShieldingEffect = 1;
            InSpaceHighCoefficient = 0.40f;
            InSpaceLowCoefficient = 0.20f;
            StratoCoefficient = 0.2f;
            TroposphereCoefficient = 0.01f;
            EVAExposure = 5;
            SolarRadiation = 2500;
            GalacticRadiation = 12500;
            RadStormsEnabled = true;
            RadStormFrequence = 1;
            RadStormMagnitude = 1;
            DecontaminationRate = 100000;
            DecontaminationHealthLoss = 0.75f;
            DecontaminationFundsCost = 100000;
            DecontaminationScienceCost = 1000;
            RequireUpgradedFacilityForDecontamination = true;
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
            Core.Log($"Applying KerbalHealthGeneralSettings settings: {settingsNode}");
            settingsNode.TryGetValue("RadiationEnabled", ref RadiationEnabled);
            settingsNode.TryGetValue("RadiationEffect", ref RadiationEffect);
            settingsNode.TryGetValue("UseKerbalismRadiation", ref UseKerbalismRadiation);
            settingsNode.TryGetValue("KerbalismRadiationRatio", ref KerbalismRadiationRatio);
            settingsNode.TryGetValue("ShieldingEffect", ref ShieldingEffect);
            settingsNode.TryGetValue("InSpaceHighCoefficient", ref InSpaceHighCoefficient);
            settingsNode.TryGetValue("InSpaceLowCoefficient", ref InSpaceLowCoefficient);
            settingsNode.TryGetValue("StratoCoefficient", ref StratoCoefficient);
            settingsNode.TryGetValue("TroposphereCoefficient", ref TroposphereCoefficient);
            settingsNode.TryGetValue("EVAExposure", ref EVAExposure);
            settingsNode.TryGetValue("SolarRadiation", ref SolarRadiation);
            settingsNode.TryGetValue("GalacticRadiation", ref GalacticRadiation);
            settingsNode.TryGetValue("RadStormsEnabled", ref RadStormsEnabled);
            settingsNode.TryGetValue("RadStormFrequency", ref RadStormFrequence);
            settingsNode.TryGetValue("RadStormMagnitude", ref RadStormMagnitude);
            settingsNode.TryGetValue("DecontaminationRate", ref DecontaminationRate);
            settingsNode.TryGetValue("DecontaminationHealthLoss", ref DecontaminationHealthLoss);
            settingsNode.TryGetValue("DecontaminationFundsCost", ref DecontaminationFundsCost);
            settingsNode.TryGetValue("DecontaminationScienceCost", ref DecontaminationScienceCost);
            settingsNode.TryGetValue("RequireUpgradedFacilityForDecontamination", ref RequireUpgradedFacilityForDecontamination);
            settingsNode.TryGetValue("DecontaminationAstronautComplexLevel", ref DecontaminationAstronautComplexLevel);
            settingsNode.TryGetValue("DecontaminationRNDLevel", ref DecontaminationRNDLevel);
        }

        public static KerbalHealthRadiationSettings Instance => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>();

        [GameParameters.CustomParameterUI("#KH_RS_RadiationEnabled", toolTip = "#KH_RS_RadiationEnabled_desc")]//Radiation Enabled""Degrade max health based on accumulated dose
        public bool RadiationEnabled = true;

        [GameParameters.CustomFloatParameterUI("#KH_RS_RadiationEffect", toolTip = "#KH_RS_RadiationEffect_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 41)]//Radiation Effect""Percentage of max health drained by 1e7 (10M) doses. 0 to disable effect
        public float RadiationEffect = 0.1f;

        [GameParameters.CustomParameterUI("#KH_RS_UseKerbalismRadiation", toolTip = "#KH_RS_UseKerbalismRadiation_desc")]//Use Kerbalism for Radiation""Use radiation values provided by Kerbalism (if installed) instead of Kerbal Health's own. Kerbalism uses a more complex and realistic radiation model
        public bool UseKerbalismRadiation = false;

        [GameParameters.CustomFloatParameterUI("#KH_RS_KerbalismRadiationRatio", toolTip = "#KH_RS_KerbalismRadiationRatio_desc", minValue = 0.01f, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 101)]//Kerbalism Radiation Ratio""Multiplier for translating Kerbalism's radiation units into Kerbal Health radiation units
        public float KerbalismRadiationRatio = 0.25f;

        [GameParameters.CustomFloatParameterUI("#KH_RS_ShieldingEffect", toolTip = "#KH_RS_ShieldingEffect_desc", minValue = 0, maxValue = 2, displayFormat = "N1", asPercentage = true, stepCount = 41)]//Shielding Multiplier""Efficiency of radiation shielding provided by parts and resources
        public float ShieldingEffect = 1;

        [GameParameters.CustomFloatParameterUI("#KH_RS_InSpaceHighCoefficient", toolTip = "#KH_RS_InSpaceHighCoefficient_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]//In Space High Coefficient""How much cosmic radiation reaches vessels in high planetary orbit or on moons
        public float InSpaceHighCoefficient = 0.40f;

        [GameParameters.CustomFloatParameterUI("#KH_RS_InSpaceLowCoefficient", toolTip = "#KH_RS_InSpaceLowCoefficient_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]//In Space Low Coefficient""How much cosmic radiation reaches vessels in low planetary orbits
        public float InSpaceLowCoefficient = 0.20f;

        [GameParameters.CustomFloatParameterUI("#KH_RS_StratoCoefficient", toolTip = "#KH_RS_StratoCoefficient_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]//Stratosphere Transparency""How much cosmic radiation reaches outer layers of the atmosphere from space
        public float StratoCoefficient = 0.2f;

        [GameParameters.CustomFloatParameterUI("#KH_RS_TroposphereCoefficient", toolTip = "#KH_RS_TroposphereCoefficient_desc", minValue = 0, maxValue = 0.05f, displayFormat = "N4", asPercentage = true, stepCount = 51)]//Troposphere Transparency""How much cosmic radiation reaches the ground and lower layers of the atmosphere from space
        public float TroposphereCoefficient = 0.01f;

        [GameParameters.CustomFloatParameterUI("#KH_RS_EVAExposure", toolTip = "#KH_RS_EVAExposure_desc", minValue = 0, maxValue = 10, displayFormat = "N0", stepCount = 21)]//EVA Exposure Multiplier""How much more radiaiton kerbals receive when on EVA
        public float EVAExposure = 5;

        [GameParameters.CustomFloatParameterUI("#KH_RS_SolarRadiation", toolTip = "#KH_RS_SolarRadiation_desc", minValue = 0, maxValue = 10000, displayFormat = "N0", stepCount = 21)]//Solar Radiation (Nominal)""Solar radiation and base radiation storm intensity in interplanetary space at 1 AU, banana doses/day
        public float SolarRadiation = 2500;

        [GameParameters.CustomFloatParameterUI("#KH_RS_GalacticRadiation", toolTip = "#KH_RS_GalacticRadiation_desc", minValue = 0, maxValue = 30000, displayFormat = "N0", stepCount = 31)]//Galactic Radiation""Galactic cosmic radiation in interplanetary space, banana doses/day
        public float GalacticRadiation = 12500;

        [GameParameters.CustomParameterUI("#KH_RS_RadStormsEnabled", toolTip = "#KH_RS_RadStormsEnabled_desc")]//Radiation Storms""Enable solar radiation storms (CMEs). Must have radiation enabled to work
        public bool RadStormsEnabled = true;

        [GameParameters.CustomFloatParameterUI("#KH_RS_RadStormFrequence", toolTip = "#KH_RS_RadStormFrequence_desc", minValue = 0, maxValue = 2, displayFormat = "N2", asPercentage = true, stepCount = 41)]//RadStorm Frequency""How often radiation storms happen, relative to default values
        public float RadStormFrequence = 1;

        [GameParameters.CustomFloatParameterUI("#KH_RS_RadStormMagnitude", toolTip = "#KH_RS_RadStormMagnitude_desc", minValue = 0, maxValue = 2, displayFormat = "N2", asPercentage = true, stepCount = 41)]//RadStorm Magnitude""How strong radstorms are, relative to default values
        public float RadStormMagnitude = 1;

        [GameParameters.CustomFloatParameterUI("#KH_RS_DecontaminationRate", toolTip = "#KH_RS_DecontaminationRate_desc", minValue = 1000, maxValue = 1000000, displayFormat = "N0", logBase = 10)]//Decontamination Rate per Day""How much radiation is lost per day during decontamination
        public float DecontaminationRate = 100000;

        [GameParameters.CustomFloatParameterUI("#KH_RS_DecontaminationHealthLoss", toolTip = "#KH_RS_DecontaminationHealthLoss_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true)]//Decontamination Health Loss""How much health is lost while the kerbal is decontaminating
        public float DecontaminationHealthLoss = 0.75f;

        [GameParameters.CustomFloatParameterUI("#KH_RS_DecontaminationFundsCost", toolTip = "#KH_RS_DecontaminationFundsCost_desc", minValue = 0, maxValue = 1000000, displayFormat = "N0")]//Decontamination Funds Cost""How much Funds each decontamination procedure costs (Career only)
        public float DecontaminationFundsCost = 100000;

        [GameParameters.CustomFloatParameterUI("#KH_RS_DecontaminationScienceCost", toolTip = "#KH_RS_DecontaminationScienceCost_desc", minValue = 0, maxValue = 10000, displayFormat = "N0")]//Decontamination Science Cost""How much Science each decontamination procedure costs (Career & Science modes)
        public float DecontaminationScienceCost = 1000;

        [GameParameters.CustomParameterUI("#KH_RS_DecontaminationUpgrades", toolTip = "#KH_RS_DecontaminationUpgrades_desc")]//Require Upgraded Facilities for Decontamination
        public bool RequireUpgradedFacilityForDecontamination = true;

        public int DecontaminationAstronautComplexLevel = 3;

        public int DecontaminationRNDLevel = 3;

        [GameParameters.CustomFloatParameterUI("#KH_RS_AnomalyDecontaminationChance", toolTip = "#KH_RS_AnomalyDecontaminationChance_desc", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float AnomalyDecontaminationChance = 1;
    }
}
