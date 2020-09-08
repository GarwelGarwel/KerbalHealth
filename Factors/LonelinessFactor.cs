using KSP.Localization;

namespace KerbalHealth
{
    class LonelinessFactor : HealthFactor
    {
        public override string Name => "Loneliness";

        public override string Title => Localizer.Format("#KH_Factor_Loneliness");//Loneliness

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.LonelinessFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor && !IsEnabledInEditor())
                return 0;
            return (Core.GetCrewCount(pcm) <= 1) && !pcm.isBadass ? BaseChangePerDay : 0;
        }
    }
}
