using KSP.Localization;
namespace KerbalHealth
{
    class LonelinessFactor : HealthFactor
    {
        public override string Name => "Loneliness";
        public override string Title => Localizer.Format("#KH_Loneliness");//Loneliness

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.LonelinessFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor && !IsEnabledInEditor()) return 0;
            if ((Core.GetCrewCount(pcm) <= 1) && !pcm.isBadass) return BaseChangePerDay; else return 0;
        }
    }
}
