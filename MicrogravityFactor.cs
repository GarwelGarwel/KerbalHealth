using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class MicrogravityFactor : HealthFactor
    {
        public override string Id
        { get { return "Microgravity"; } }

        public override double BaseChangePerDay
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().MicrogravityFactor; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
            {
                Core.Log("Microgravity factor always on in Editor.");
                return BaseChangePerDay;
            }
            if ((pcm.KerbalRef.InVessel.situation & (Vessel.Situations.ORBITING | Vessel.Situations.SUB_ORBITAL)) != 0)
            {
                Core.Log("Microgravity is on due to being in " + pcm.KerbalRef.InVessel.situation);
                return BaseChangePerDay;
            }
            if (pcm.geeForce < 0.1)
            {
                Core.Log("Microgravity is on due to g = " + pcm.geeForce);
                return BaseChangePerDay;
            }
            Core.Log("Microgravity is off, g = " + pcm.geeForce);
            return 0;
            //Core.IsInEditor || (pcm.KerbalRef.InVessel.situation & (Vessel.Situations.ORBITING | Vessel.Situations.SUB_ORBITAL)) != 0 || (pcm.geeForce < 0.1)
        }
    }
}
