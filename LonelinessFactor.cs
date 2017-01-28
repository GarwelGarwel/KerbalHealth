using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class LonelinessFactor : HealthFactor
    {
        public override string Id
        { get { return "Loneliness"; } }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().LonelinessFactor; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        { if ((Core.GetCrewCount(pcm) <= 1) && !pcm.isBadass) return BaseChangePerDay; else return 0; }
    }
}
