using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class OverpopulationFactor : HealthFactor
    {
        public override string Id
        { get { return "Overpopulation"; } }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().OverpopulationBaseFactor; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        { return BaseChangePerDay * Core.GetCrewCount(pcm) / Core.GetCrewCapacity(pcm); }
    }
}
