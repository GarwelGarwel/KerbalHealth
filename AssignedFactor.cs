using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class AssignedFactor : HealthFactor
    {
        public override string Id
        { get { return "Assigned"; } }

        public override string Name
        { get { return "Assigned"; } }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().AssignedFactor; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            return Core.IsInEditor || (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) ? BaseChangePerDay : 0;
        }
    }
}
