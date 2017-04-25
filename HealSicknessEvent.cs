using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class HealSicknessEvent : Event
    {
        public override string Name
        { get { return "HealSickness"; } }

        public override string Message()
        {
            return khs.Name + " has healed " + (khs.PCM.gender == ProtoCrewMember.Gender.Male ? "his" : "her") + " sickness!";
        }

        public override bool Condition()
        {
            return khs.HasCondition("Sickness");
        }

        public override double ChancePerDay()
        {
            return 0.1;  // TODO: make chance depend on presence of medics/medbays
        }

        public override void Run()
        {
            khs.RemoveCondition("Sickness");
        }
    }
}
