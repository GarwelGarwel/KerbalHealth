using KSP.Localization;

namespace KerbalHealth
{
    public class KSCFactor : HealthFactor
    {
        public const float BaseChangePerDay_Default = 3;

        public override string Name => "KSC";

        public override string Title => Localizer.Format("#KH_Factor_KSC");

        public override bool ConstantForUnloaded => false;

        public override bool ShownInEditor => false;

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double BaseChangePerDay => BaseChangePerDay_Default * KerbalHealthFactorsSettings.Instance.KSCEffect;

        public override double ChangePerDay(KerbalHealthStatus khs) =>
            khs.ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available && !Core.IsInEditor && !khs.IsTrainingAtKSC ? BaseChangePerDay : 0;
    }
}
