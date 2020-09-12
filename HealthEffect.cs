namespace KerbalHealth
{
    public class HealthEffect
    {
        public HealthModifierSet HealthModifiers { get; set; } = new HealthModifierSet();

        public Logic Logic { get; set; } = new Logic();

        public HealthEffect(ConfigNode node)
        {
            HealthModifiers.ConfigNode = node;
            Logic.ConfigNode = node;
        }

        private HealthEffect()
        { }

        public bool IsApplicable(KerbalHealthStatus khs) => Logic.Test(khs.PCM);

        public override string ToString()
        {
            string res = HealthModifiers.ToString();

            string l = Logic.ToString();
            if (l.Length != 0)
                res += $"\nLogic:\n{l}";

            return res.Trim();
        }
    }
}
