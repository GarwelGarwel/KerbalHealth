using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class CrowdedFactor : HealthFactor
    {
        public override string Name
        { get { return "Crowded"; } }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().CrowdedBaseFactor; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        { return BaseChangePerDay * Core.GetCrewCount(pcm) / Core.GetCrewCapacity(pcm); }
    }
}
