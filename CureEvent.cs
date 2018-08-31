using System;
using System.Collections.Generic;

namespace KerbalHealth
{
    public class CureEvent : Event
    {
        public override string Name => "Cure";

        protected override bool IsSilent => khs.HasCondition("Infected");

        public override string Message() => khs.Name + " has cured " + (khs.PCM.gender == ProtoCrewMember.Gender.Male ? "his" : "her") + " sickness!";

        public override bool Condition() => Core.SicknessEnabled && khs.HasCondition("Sick");

        public override double ChancePerDay()
        {
            double chance = 0;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().SicknessDuration > 0)
                chance = 1 / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().SicknessDuration;
            Core.Log("Chance of sickness self-treatment is " + chance + ".");
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().TreatmentDuration <= 0) return chance;
            int sickCrew = 0, doctorsCrew = 0;
            List<ProtoCrewMember> kerbalsList;
            if (khs.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Available)
            {
                kerbalsList = new List<ProtoCrewMember>(HighLogic.fetch.currentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Crew, ProtoCrewMember.RosterStatus.Available));
                kerbalsList.AddRange(HighLogic.fetch.currentGame.CrewRoster.Kerbals(ProtoCrewMember.KerbalType.Tourist, ProtoCrewMember.RosterStatus.Available));
            }
            else kerbalsList = Core.KerbalVessel(khs.PCM).GetVesselCrew();
            foreach (ProtoCrewMember pcm in kerbalsList)
            {
                if (Core.KerbalHealthList.Find(pcm).HasCondition("Sick")) sickCrew++;
                if (pcm.trait == "Scientist") doctorsCrew++;
                if (pcm.trait == "Medic") doctorsCrew += 2;
            }
            Core.Log(khs.Name + "'s vessel has " + sickCrew + " sick crew members, " + doctorsCrew + " doctors.");
            if (doctorsCrew != 0) chance = 1 - (1 - chance) * (1 - Math.Min((double) doctorsCrew / sickCrew, 1) / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().TreatmentDuration);
            if ((chance != 0) && Core.QuirksEnabled)
                foreach (Quirk q in khs.Quirks)
                    foreach (HealthEffect he in q.Effects)
                        if (he.IsApplicable(khs)) chance *= he.CureChance;
            Core.Log("Total chance of sickness curing is " + chance + ".");
            return chance;
        }

        protected override void Run()
        {
            Core.Log("Curing " + khs.Name + "'s sickness...");
            khs.RemoveCondition("Sick");
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().ImmunityDuration > 0)
                khs.AddCondition("Immune");
        }
    }
}
