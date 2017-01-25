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

        [GameParameters.CustomFloatParameterUI("Assigned Factor", toolTip = "HP change per day when the kerbal is assigned", minValue = -100, maxValue = 100, stepCount = 201)]
        public float AssignedFactor = -1;

        [GameParameters.CustomFloatParameterUI("Living Space Factor", toolTip = "HP change per day in a crammed vessel", minValue = -100, maxValue = 100, stepCount = 201)]
        public float LivingSpaceBaseFactor = -7;

        [GameParameters.CustomFloatParameterUI("Not Alone Factor", toolTip = "HP change per day when the kerbal has crewmates", minValue = -100, maxValue = 100, stepCount = 201)]
        public float NotAloneFactor = 1;

        [GameParameters.CustomFloatParameterUI("KSC Factor", toolTip = "HP change per day when the kerbal is at KSC (available)", minValue = -100, maxValue = 100, stepCount = 201)]
        public float KSCFactor = 5;
    }
}
