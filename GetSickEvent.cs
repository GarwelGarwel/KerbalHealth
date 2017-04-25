using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class GetSickEvent : Event
    {
        public override string Name
        { get { return "GetSick"; } }

        public override string Message()
        { return khs.Name + " has fallen sick."; }

        // Cannot become sick twice
        public override bool Condition()
        { return !khs.HasCondition("Sickness"); }

        public override double ChancePerDay()
        {
            return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().GetSickChance;  // TODO: make chance depend on kerbal's crewmates
        }

        public override void Run()
        {
            khs.AddCondition(new KerbalHealth.HealthCondition("Sickness"));
        }
    }
}
