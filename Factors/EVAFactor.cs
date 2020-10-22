using KSP.Localization;

namespace KerbalHealth
{
    class EVAFactor : HealthFactor
    {
        public override string Name => "EVA";

        public override string Title => Localizer.Format("#KH_Factor_EVA");

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.EVAFactor;

        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            return khs.IsOnEVA ? BaseChangePerDay : 0;
        }
    }
}
