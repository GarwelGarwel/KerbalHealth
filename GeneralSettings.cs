using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class GeneralSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "General Settings"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "KerbalHealth"; } }
        public override int SectionOrder { get { return 1; } }

        [GameParameters.CustomFloatParameterUI("Update Interval", toolTip = "Number of seconds between health updates\nDoesn't affect health rates. Increase if performance too slow", minValue = 0.04f, maxValue = 60)]
        public float UpdateInterval = 1;

        //[GameParameters.CustomFloatParameterUI("Min HP", toolTip = "Minimum number of Health Points", minValue = -100, maxValue = 0)]
        public float MinHP = 0;

        [GameParameters.CustomFloatParameterUI("Base Max HP", toolTip = "Max number of Health Points for 0-star kerbals", minValue = 10, maxValue = 200, stepCount = 20)]
        public float BaseMaxHP = 100;

        [GameParameters.CustomFloatParameterUI("HP per Level", toolTip = "Health Points increase per level (star) of a kerbal", minValue = 0, maxValue = 50, stepCount = 51)]
        public float HPPerLevel = 10;

        [GameParameters.CustomFloatParameterUI("Exhaustion Start", toolTip = "Health level when kerbals turn Exhausted (becomes Tourist)", minValue = 0, maxValue = 1, asPercentage = true, stepCount = 21)]
        public float ExhaustionStartHealth = 0.2f;

        [GameParameters.CustomFloatParameterUI("Exhaustion End Level", toolTip = "Health level when kerbals leave Exhausted state (must be greater than or equal to Exhaustion start)", minValue = 0, maxValue = 1, asPercentage = true, stepCount = 21)]
        public float ExhaustionEndHealth = 0.25f;

        [GameParameters.CustomFloatParameterUI("Death Level", toolTip = "Health level when kerbals die. Make negative to disable", minValue = -0.05f, maxValue = 1, asPercentage = true, stepCount = 21)]
        public float DeathHealth = 0;

        [GameParameters.CustomParameterUI("Debug Mode", toolTip = "Controls amount of logging")]
        public bool debugMode = false;
    }
}
