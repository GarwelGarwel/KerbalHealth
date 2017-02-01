using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class EVAFactor : HealthFactor
    {
        public override string Id
        { get { return "EVA"; } }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().EVAFactor; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
            {
                Core.Log("EVA factor is always off in Editor.");
                return 0;
            }
            if (Core.KerbalHealthList.Find(pcm).IsOnEVA)
            {
                Core.Log("EVA factor is on.");
                return BaseChangePerDay;
            }
            return 0;
        }
    }
}
