using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class KerbalHealthFactorsSettings : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Health Factors"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "KerbalHealth"; } }
        public override int SectionOrder { get { return 2; } }

        [GameParameters.CustomFloatParameterUI("Assigned", toolTip = "HP change per day when the kerbal is assigned", minValue = -20, maxValue = 0, stepCount = 41)]
        public float AssignedFactor = -0.5f;

        [GameParameters.CustomFloatParameterUI("Overpopulation", toolTip = "HP change per day in a crammed vessel", minValue = -50, maxValue = 0, stepCount = 51)]
        public float OverpopulationBaseFactor = -6;

        [GameParameters.CustomFloatParameterUI("Loneliness", toolTip = "HP change per day when the kerbal has no crewmates", minValue = -50, maxValue = 0, stepCount = 51)]
        public float LonelinessFactor = -1;

        [GameParameters.CustomFloatParameterUI("Microgravity", toolTip = "HP change per day when in orbital/suborbital flight or g-force < 0.1", minValue = -20, maxValue = 0, stepCount = 41)]
        public float MicrogravityFactor = -0.5f;

        [GameParameters.CustomFloatParameterUI("EVA", toolTip = "HP change per day when on EVA", minValue = -200, maxValue = 0, stepCount = 41)]
        public float EVAFactor = -50;

        [GameParameters.CustomFloatParameterUI("Connected", toolTip = "HP change per day when connected to Kerbin", minValue = 0, maxValue = 20, stepCount = 41)]
        public float ConnectedFactor = 0.5f;

        [GameParameters.CustomFloatParameterUI("Home", toolTip = "HP change per day when under 25 km at Kerbin", minValue = 0, maxValue = 20, stepCount = 41)]
        public float HomeFactor = 1;

        [GameParameters.CustomFloatParameterUI("At KSC", toolTip = "HP change per day when the kerbal is at KSC (available)", minValue = 0, maxValue = 50, stepCount = 51)]
        public float KSCFactor = 5;
    }
}
