using KSP.Localization;

namespace KerbalHealth
{
    class EVAFactor : HealthFactor
    {
        public const float BaseChangePerDay_Default = -10;

        public override string Name => "EVA";

        public override string Title => Localizer.Format("#KH_Factor_EVA");

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double BaseChangePerDay => BaseChangePerDay_Default * KerbalHealthFactorsSettings.Instance.EVAEffect;

        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            return khs.IsOnEVA ? BaseChangePerDay : 0;
        }
    }
}
