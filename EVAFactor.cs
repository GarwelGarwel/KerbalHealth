using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class EVAFactor : HealthFactor
    {
        public override string Name
        { get { return "EVA"; } }

        public override void ResetEnabledInEditor() { SetEnabledInEditor (false); }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().EVAFactor; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor) return IsEnabledInEditor() ? BaseChangePerDay : 0;
            if (Core.KerbalHealthList.Find(pcm).IsOnEVA)
            {
                Core.Log("EVA factor is on.");
                return BaseChangePerDay;
            }
            return 0;
        }
    }
}
