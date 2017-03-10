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
        public override int SectionOrder { get { return 3; } }

        [GameParameters.CustomParameterUI("Events Enabled", toolTip = "If checked, random health events can happen")]
        public bool EventsEnabled = true;

        [GameParameters.CustomFloatParameterUI("Feeling Bad chance", toolTip = "Chance per day of Feeling Bad event happening", minValue = 0, maxValue = 0.002f, displayFormat = "F4", stepCount = 21)]
        public float FeelBadChance = 0.001f;

        [GameParameters.CustomFloatParameterUI("Feeling Bad min damage", toolTip = "Min % of HP lost in a Feeling Bad event", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float FeelBadMinDamage = 0.2f;

        [GameParameters.CustomFloatParameterUI("Feeling Bad max damage", toolTip = "Max % of HP lost in a Feeling Bad event", minValue = 0, maxValue = 1, displayFormat = "N2", asPercentage = true, stepCount = 21)]
        public float FeelBadMaxDamage = 0.5f;

        [GameParameters.CustomFloatParameterUI("Panic Attack chance", toolTip = "Max chance per day of a Panic Attack", minValue = 0, maxValue = 0.02f, displayFormat = "F3", stepCount = 21)]
        public float PanicAttackChance = 0.01f;

        [GameParameters.CustomFloatParameterUI("Panic Attack max duration", toolTip = "Maximum duration of a Panic Attack in seconds", minValue = 0, maxValue = 6 * 3600, stepCount = 7)]
        public float PanicAttackMaxDuration = 3 * 3600;
    }
}
