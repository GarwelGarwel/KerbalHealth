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
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().MicrogravityFactor; } }

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
            {
                Core.Log("Microgravity factor always on in Editor.");
                return BaseChangePerDay;
            }
            if (pcm == null)
            {
                Core.Log("MicrogravityFactor.ChangePerDay: pcm is null!", Core.LogLevel.Error);
                return 0;
            }
            if (Core.KerbalHealthList.Find(pcm) == null)
            {
                Core.Log(pcm.name + " not found in KerbalHealthList. The list has " + Core.KerbalHealthList.Count + " records.", Core.LogLevel.Error);
                return 0;
            }
            if (pcm.KerbalRef == null)
                Core.Log("MicrogravityFactor.ChangePerDay: pcm.KerbalRef is null for " + pcm.name + "!", Core.LogLevel.Important);
            else if (pcm.KerbalRef.InVessel == null)
                Core.Log("MicrogravityFactor.ChangePerDay: pcm.KerbalRef.InVessel is null for " + pcm.name + ", he/she is " + (Core.KerbalHealthList.Find(pcm).IsOnEVA ? "" : "NOT") + " on EVA!", Core.KerbalHealthList.Find(pcm).IsOnEVA ? Core.LogLevel.Debug : Core.LogLevel.Error);
            if (!Core.KerbalHealthList.Find(pcm).IsOnEVA && (pcm.KerbalRef.InVessel.situation & (Vessel.Situations.ORBITING | Vessel.Situations.SUB_ORBITAL)) != 0)
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
        }
    }
}
