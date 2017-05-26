using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class SicknessFactor : HealthFactor
    {
        public override string Name
        { get { return "Sickness"; } }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().SicknessFactor; } }

        public override bool Cachable
        { get { return false; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            KerbalHealthStatus khs = Core.KerbalHealthList.Find(pcm);
            if (khs == null) return 0;
            if (khs.HasCondition("Sick"))
                return BaseChangePerDay;
            return 0;
        }
    }
}
