namespace KerbalHealth
{
    public class PanicAttackEvent : Event
    {
        public override string Name => "PanicAttack";

        public override string Message() => khs.Name + " is having a panic attack!";

        public override bool Condition() => (khs.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) && !khs.HasCondition("Exhausted");

        public override double ChancePerDay()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().PanicAttackPeriod > 0)
                return 1 / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().PanicAttackPeriod * (1 - (khs.Health - Core.ExhaustionStartHealth) / (1 - Core.ExhaustionStartHealth)) * (1 - khs.PCM.courage);
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
