using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class KSCFactor : HealthFactor
    {
        public override string Name
        { get { return "KSC"; } }

        public override string Title
        { get { return "KSC"; } }

        public override bool Cachable
        { get { return false; } }

        public override void ResetEnabledInEditor() { SetEnabledInEditor(false); }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().KSCFactor; } }
        
        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor) return IsEnabledInEditor() ? BaseChangePerDay : 0;
            return (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available) ? BaseChangePerDay : 0;
        }
    }
}
