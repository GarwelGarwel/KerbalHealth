namespace KerbalHealth
{
    public class ConditionalEffect : HealthEffect
    {
        public Logic Logic { get; set; } = new Logic();

        public ConditionalEffect(ConfigNode node)
        {
            Load(node);
            Logic.Load(node);
        }

        private ConditionalEffect()
        { }

        public bool IsApplicable(KerbalHealthStatus khs) => Logic.Test(khs.PCM);

        public override string ToString()
        {
            string res = base.ToString();
            string l = Logic.ToString();
            if (l.Length != 0)
                res += $"\nLogic:\n{l}";
            return res.Trim();
        }
    }
}
