using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class FactorsSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Health Factors"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "KerbalHealth"; } }
        public override int SectionOrder { get { return 2; } }

        [GameParameters.CustomFloatParameterUI("Assigned", toolTip = "HP change per day when the kerbal is assigned", minValue = -50, maxValue = 0, stepCount = 51)]
        public float AssignedFactor = 0;

        [GameParameters.CustomFloatParameterUI("Overpopulation", toolTip = "HP change per day in a crammed vessel", minValue = -50, maxValue = 0, stepCount = 51)]
        public float OverpopulationBaseFactor = -7;

        [GameParameters.CustomFloatParameterUI("Loneliness", toolTip = "HP change per day when the kerbal has no crewmates", minValue = -50, maxValue = 0, stepCount = 51)]
        public float LonelinessFactor = -1;

        [GameParameters.CustomFloatParameterUI("Microgravity", toolTip = "HP change per day when in orbit or suborbital flight", minValue = -50, maxValue = 0, stepCount = 101)]
        public float Microgravity = -0.5f;

        [GameParameters.CustomFloatParameterUI("At KSC", toolTip = "HP change per day when the kerbal is at KSC (available)", minValue = 0, maxValue = 50, stepCount = 51)]
        public float KSCFactor = 5;
    }
}
