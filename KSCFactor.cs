using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class KSCFactor : HealthFactor
    {
        public override string Id
        { get { return "KSC"; } }

        public override string Name
        { get { return "KSC"; } }

        public override bool LoadedOnly
        { get { return false; } }

        // Not available in editor
        //public override GameScenes ValidScenes
        //{ get { return base.ValidScenes ^ GameScenes.EDITOR; } }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().KSCFactor; } }
        
        public override double ChangePerDay(ProtoCrewMember pcm)
        { return (!Core.IsInEditor && (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available)) ? BaseChangePerDay : 0; }
    }
}
