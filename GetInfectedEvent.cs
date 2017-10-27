using System;

namespace KerbalHealth
{
    public class GetInfectedEvent : Event
    {
        public override string Name => "GetInfected";

        protected override bool IsSilent => true;

        public override bool Condition() => Core.SicknessEnabled && !khs.HasCondition("Infected") && !khs.HasCondition("Sick") && !khs.HasCondition("Immune");

        public override double ChancePerDay()
        {
            if (khs.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Available)
                return 1 / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().KSCGetSickPeriod;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().ContagionPeriod <= 0) return 0;
            Core.Log("Counting infected crewmates for " + khs.Name + "...");
            int sickCrewmates = 0;
            foreach (ProtoCrewMember pcm in Core.KerbalVessel(khs.PCM).GetVesselCrew())
                if ((pcm.name != khs.Name) && (Core.KerbalHealthList.Find(pcm).HasCondition("Sick") || (Core.KerbalHealthList.Find(pcm).HasCondition("Infected"))))
                    sickCrewmates++;
            Core.Log(sickCrewmates + " infected crewmates found.");
            return 1 - Math.Pow(1 - 1 / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().ContagionPeriod, sickCrewmates);
        }

        protected override void Run()
        {
            Core.Log("Infecting " + khs.Name + "...");
            khs.AddCondition(new HealthCondition("Infected", false));
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().IncubationDuration <= 0)
                new GetSickEvent().Process(khs);
        }
    }
}
