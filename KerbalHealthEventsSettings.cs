namespace KerbalHealth
{
    class KerbalHealthEventsSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "Health Events";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override bool HasPresets => true;
        public override string Section => "Kerbal Health";
        public override string DisplaySection => Section;
        public override int SectionOrder => 3;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                case GameParameters.Preset.Normal:
                    EventsEnabled = false;
                    SicknessEnabled = false;
                    break;
                case GameParameters.Preset.Moderate:
                    EventsEnabled = true;
                    SicknessEnabled = false;
                    break;
                case GameParameters.Preset.Hard:
                    EventsEnabled = true;
                    SicknessEnabled = true;
                    break;
            }
        }

        [GameParameters.CustomParameterUI("Events Enabled", toolTip = "If checked, random health events can happen")]
        public bool EventsEnabled = true;

        [GameParameters.CustomParameterUI("Notify of Events in KSC", toolTip = "If checked, notifications will be given of events with kerbals not on mission")]
        public bool KSCNotificationsEnabled = false;

        [GameParameters.CustomFloatParameterUI("Avg. Time between Accidents", toolTip = "Average # of days between health accidents happening to a kerbal with 50% stupidity. 0 to disable accidents", minValue = 0, maxValue = 2000, stepCount = 41)]
        public float AccidentPeriod = 1000;

        [GameParameters.CustomFloatParameterUI("Accident Min Damage", toolTip = "Min % of HP lost in an Accident event", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float AccidentMinDamage = 0.2f;

        [GameParameters.CustomFloatParameterUI("Accident Max Damage", toolTip = "Max % of HP lost in an Accident event", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float AccidentMaxDamage = 0.5f;

        [GameParameters.CustomFloatParameterUI("Min/Avg. Time between Panic Attacks", toolTip = "Average # of days between Panic Attacks for a kerbal in the worst-case scenario. 0 to disable panic attacks", minValue = 0, maxValue = 200, stepCount = 41)]
        public float PanicAttackPeriod = 100;

        [GameParameters.CustomFloatParameterUI("Max Panic Attack Duration", toolTip = "Maximum duration of a Panic Attack in hours", minValue = 0, maxValue = 6, stepCount = 7)]
        public float PanicAttackMaxDuration = 3;

        [GameParameters.CustomParameterUI("Sickness Enabled", toolTip = "If checked, kebals can become sick and infect each other")]
        public bool SicknessEnabled = true;

        [GameParameters.CustomFloatParameterUI("Avg. KSC Sickness Period", toolTip = "Average # of days before a kerbal gets sick while in KSC", minValue = 1, maxValue = 500, stepCount = 26)]
        public float KSCGetSickPeriod = 200;

        [GameParameters.CustomFloatParameterUI("Avg. Contagion Period", toolTip = "Average # of days before a kerbal gets infected by another sick kerbal. 0 to disable contagion", minValue = 0, maxValue = 40, stepCount = 21)]
        public float ContagionPeriod = 20;

        [GameParameters.CustomFloatParameterUI("Avg. Incubation Duration", toolTip = "Average # of days between getting infection and showing symptoms. 0 to disable incubation", minValue = 0, maxValue = 40, stepCount = 21)]
        public float IncubationDuration = 20;

        [GameParameters.CustomFloatParameterUI("Avg. Untreated Sickness Duration", toolTip = "Average # of days before a sickness is self-cured, without external help. 0 to disable self-curing sickness", minValue = 0, maxValue = 30, stepCount = 31)]
        public float SicknessDuration = 20;

        [GameParameters.CustomFloatParameterUI("Avg. Treated Sickness Duration", toolTip = "Average # of days before a sickness is cured if enough medics are present. 0 to disable treatment of sickness", minValue = 0, maxValue = 30, stepCount = 31)]
        public float TreatmentDuration = 10;

        [GameParameters.CustomFloatParameterUI("Avg. Immunity Duration", toolTip = "Average # of days after curing a sickness when the kerbal is immune. 0 to disable immunity", minValue = 0, maxValue = 40, stepCount = 21)]
        public float ImmunityDuration = 20;
    }
}
