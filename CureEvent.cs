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

        protected override NotificationType Notification
        {
            get
            {
                if (khs.HasCondition("Infected")) return NotificationType.Silent;
                return base.Notification;
            }
        }

        public override string Message()
        { return khs.Name + " has cured " + (khs.PCM.gender == ProtoCrewMember.Gender.Male ? "his" : "her") + " sickness!"; }

        public override bool Condition()
        { return khs.HasCondition("Sick"); }

        public override double ChancePerDay()
        {
            double chance = 0;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().SicknessDuration > 0)
                chance = 1 / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().SicknessDuration;
            Core.Log("Chance of sickness self-treatment is " + chance + ".");
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().TreatmentDuration <= 0)
                return chance;
            int sickCrew = 0, doctorsCrew = 0;
            foreach (ProtoCrewMember pcm in Core.KerbalVessel(khs.PCM).GetVesselCrew())
            {
                if (Core.KerbalHealthList.Find(pcm).HasCondition("Sick")) sickCrew++;
                if ((pcm.trait == "Scientist") || (pcm.trait == "Medic")) doctorsCrew++;
            }
            Core.Log(khs.Name + "'s vessel has " + sickCrew + " sick crew members, " + doctorsCrew + " doctors.");
            chance = 1 - (1 - chance) * (1 - Math.Min((double) doctorsCrew / sickCrew, 1) / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().TreatmentDuration);
            Core.Log("Total chance of sickness curing is " + chance + ".");
            return chance;
        }

        protected override void Run()
        {
            Core.Log("Curing " + khs.Name + "'s sickness...");
            khs.RemoveCondition("Sick");
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().ImmunityDuration > 0)
                khs.AddCondition(new HealthCondition("Immune", false));
        }
    }
}
