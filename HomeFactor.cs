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
            if (Core.KerbalHealthList.Find(pcm).IsOnEVA)
            {
                Core.Log(pcm.name + " is on EVA, " + Id + " factor is unapplicable.");
                return 0;
            }
            if (!Core.KerbalHealthList.Find(pcm).IsOnEVA && ((pcm.seat?.vessel.mainBody.name == "Kerbin") || (pcm.seat?.vessel.mainBody.name == "Earth")) && (pcm.seat?.vessel.altitude < 70000))
            {
                Core.Log("Home factor is on.");
                return BaseChangePerDay;
            }
            Core.Log("Home factor is off. Main body: " + pcm.seat?.vessel.mainBody.name + "; altitude: " + pcm.seat?.vessel.altitude + ".");
            return 0;
        }
    }
}
