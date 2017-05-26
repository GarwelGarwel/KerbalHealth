using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class HomeFactor : HealthFactor
    {
        public override string Name
        { get { return "Home"; } }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().HomeFactor; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (pcm == null)
            {
                Core.Log("HomeFactor.ChangePerDay: pcm is null!", Core.LogLevel.Error);
                return 0;
            }
            if (Core.IsInEditor)
            {
                Core.Log("Home factor is always off in Editor.");
                return 0;
            }
            if (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
            {
                Core.Log("Home factor is off when kerbal is not assigned.");
                return 0;
            }
            if (((Core.KerbalVessel(pcm).mainBody.name == "Kerbin") || (Core.KerbalVessel(pcm).mainBody.name == "Earth")) && (Core.KerbalVessel(pcm).altitude < 18000))
            {
                Core.Log("Home factor is on.");
                return BaseChangePerDay;
            }
            Core.Log("Home factor is off. Main body: " + Core.KerbalVessel(pcm).mainBody.name + "; altitude: " + Core.KerbalVessel(pcm).altitude + ".");
            return 0;
        }
    }
}
