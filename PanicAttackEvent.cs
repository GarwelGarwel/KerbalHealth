namespace KerbalHealth
{
    public class PanicAttackEvent : Event
    {
        public override string Name => "PanicAttack";

        public override string Message() => khs.Name + " is having a panic attack! " + (khs.PCM.gender == ProtoCrewMember.Gender.Male ? "He" : "She") + " is expected to get better in about " + Core.FormatTime(inactionTime, false) + ".";

        public override bool Condition() => (khs.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) && !khs.HasCondition("Exhausted");

        public override double ChancePerDay()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().PanicAttackPeriod > 0)
            {
                double c = 1 / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().PanicAttackPeriod * (1 - (khs.Health - Core.ExhaustionStartHealth) / (1 - Core.ExhaustionStartHealth)) * (1 - khs.PCM.courage);
                if (Core.QuirksEnabled)
                    foreach (Quirk q in khs.Quirks)
                        foreach (HealthEffect he in q.Effects)
                            if (he.IsApplicable(khs)) c *= he.PanicAttackChance;
                return c;
            }
            else return 0;
        }

        // Make temporarily inactive
        double inactionTime;
        protected override void Run()
        {
            inactionTime = Core.rand.NextDouble() * HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().PanicAttackMaxDuration;
            Core.Log(khs.Name + " will be inactive for " + inactionTime + " seconds.");
            khs.PCM.SetInactive(inactionTime);
        }
    }
}
