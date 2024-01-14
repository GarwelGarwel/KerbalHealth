using KSP.Localization;

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

        public bool IsApplicable(KerbalHealthStatus khs) => Logic.Test(khs.ProtoCrewMember);

        public override string ToString()
        {
            string l = Logic.ToString();
            return l.Length != 0 ? $"{base.ToString()}\n{Localizer.Format("#KH_Effect_Logic")}\n{l}" : base.ToString();
        }
    }
}
