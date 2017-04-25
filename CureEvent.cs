using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class CureEvent : Event
    {
        public override string Name
        { get { return "Cure"; } }

        public override string Message()
        {
            return khs.Name + " has cured " + (khs.PCM.gender == ProtoCrewMember.Gender.Male ? "his" : "her") + " sickness!";
        }

        public override bool Condition()
        {
            return khs.HasCondition("Sickness");
        }

        public override double ChancePerDay()
        {
            return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().CureChance;  // TODO: make chance depend on presence of scientists/medics/medbays
        }

        public override void Run()
        {
            khs.RemoveCondition("Sickness");
        }
    }
}
