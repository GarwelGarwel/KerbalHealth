using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class KerbalHealthEventsSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Health Events"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "Kerbal Health"; } }
        public override string DisplaySection { get { return Section; } }
        public override int SectionOrder { get { return 3; } }

        [GameParameters.CustomParameterUI("Events Enabled", toolTip = "If checked, random health events can happen")]
        public bool EventsEnabled = true;

        [GameParameters.CustomFloatParameterUI("Accident chance", toolTip = "Chance per day of an Accident happening", minValue = 0, maxValue = 0.002f, displayFormat = "F4", stepCount = 21)]
        public float AccidentChance = 0.001f;

        [GameParameters.CustomFloatParameterUI("Accident min damage", toolTip = "Min % of HP lost in an Accident event", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float AccidentMinDamage = 0.2f;

        [GameParameters.CustomFloatParameterUI("Accident max damage", toolTip = "Max % of HP lost in an Accident event", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float AccidentMaxDamage = 0.5f;

        [GameParameters.CustomFloatParameterUI("Panic Attack chance", toolTip = "Max chance per day of a Panic Attack", minValue = 0, maxValue = 0.02f, displayFormat = "F3", stepCount = 21)]
        public float PanicAttackChance = 0.01f;

        [GameParameters.CustomFloatParameterUI("Panic Attack max duration", toolTip = "Maximum duration of a Panic Attack in seconds", minValue = 0, maxValue = 6 * 3600, stepCount = 7)]
        public float PanicAttackMaxDuration = 3 * 3600;

        [GameParameters.CustomFloatParameterUI("Sickness chance", toolTip = "Chance per day of a kerbal getting sick", minValue = 0, maxValue = 0.02f, displayFormat = "F3", stepCount = 21)]
        public float GetSickChance = 0.01f;

        [GameParameters.CustomFloatParameterUI("Cure chance", toolTip = "Base chance per day of a sickness cured", minValue = 0, maxValue = 0.3f, displayFormat = "F2", stepCount = 21)]
        public float CureChance = 0.1f;
    }
}
