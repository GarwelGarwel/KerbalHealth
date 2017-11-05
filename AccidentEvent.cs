namespace KerbalHealth
{
    public class AccidentEvent : Event
    {
        public override string Name => "Accident";

        public override string Message() => khs.Name + " has lost some health in an accident.";

        public override bool Condition() => true;

        public override double ChancePerDay() => (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().AccidentPeriod > 0) ? 2 / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().AccidentPeriod * khs.PCM.stupidity : 0;

        float MinDamage
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().AccidentMinDamage;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().AccidentMinDamage = value;
        }

        float MaxDamage
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().AccidentMaxDamage;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().AccidentMaxDamage = value;
        }

        double Damage(double x) => 1 - MinDamage - (MaxDamage - MinDamage) * x;

        protected override void Run() => khs.HP *= Damage(Core.rand.NextDouble());
    }
}
