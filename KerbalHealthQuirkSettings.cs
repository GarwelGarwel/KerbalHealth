namespace KerbalHealth
{
    class KerbalHealthQuirkSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "Conditions & Quirks";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => false;
        public override string Section => "Kerbal Health";
        public override string DisplaySection => Section;
        public override int SectionOrder => 3;

        [GameParameters.CustomParameterUI("Conditions Enabled", toolTip = "If checked, special health conditions affect health and can randomly appear in kerbals")]
        public bool ConditionsEnabled = true;

        [GameParameters.CustomParameterUI("Notify of Events in KSC", toolTip = "If checked, notifications will be given of health condition-related events with kerbals not on mission")]
        public bool KSCNotificationsEnabled = false;

        [GameParameters.CustomFloatParameterUI("Relative Conditions Chances", toolTip = "Relative chance of acquiring a random condition", minValue = 0, maxValue = 3, displayFormat = "N2", asPercentage = true, stepCount = 31)]
        public float ConditionsChance = 1;

        [GameParameters.CustomParameterUI("Quirks Enabled", toolTip = "Quirks can be awarded to kerbals and affect their health stats")]
        public bool QuirksEnabled = true;

        [GameParameters.CustomIntParameterUI("Max Quirks", toolTip = "Maximum number of level-up quirks for a kerbal", minValue = 0, maxValue = 5, displayFormat = "N0", stepSize = 1)]
        public int MaxQuirks = 2;

        [GameParameters.CustomFloatParameterUI("Level-Up Quirk Chance", toolTip = "Chance of a kerbal being awarded a quirk when he/she levels up", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float QuirkChance = 0.25f;

        [GameParameters.CustomParameterUI("Award during Missions", toolTip = "Level-up quirks can be awarded when the kerbal is assigned, otherwise only at KSC")]
        public bool AwardQuirksOnMissions = false;

        [GameParameters.CustomFloatParameterUI("Anomaly Quirk Chance", toolTip = "Chance of a kerbal being awarded a quirk when he/she discovers an anomaly", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float AnomalyQuirkChance = 1;

        [GameParameters.CustomParameterUI("Kerbal Stats Affect Quirk Weights", toolTip = "Chances of getting some quirks depend on Courage and Stupidity of the kerbal")]
        public bool StatsAffectQuirkWeights = true;
    }
}
