using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class HomeFactor : HealthFactor
    {
        public override string Id
        { get { return "Home"; } }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().HomeFactor; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
            {
                Core.Log("Home factor is always off in Editor.");
                return 0;
            }
            if (((pcm.KerbalRef.InVessel.mainBody.name == "Kerbin") || (pcm.KerbalRef.InVessel.mainBody.name == "Earth")) && (pcm.KerbalRef.InVessel.altitude < 25000))
            {
                Core.Log("Home factor is on.");
                return BaseChangePerDay;
            }
            Core.Log("Home factor is off. Main body: " + pcm.KerbalRef.InVessel.mainBody.name + "; altitude: " + pcm.KerbalRef.InVessel.altitude + ".");
            return 0;
        }
    }
}
