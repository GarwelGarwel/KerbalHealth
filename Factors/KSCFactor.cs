using KSP.Localization;

namespace KerbalHealth
{
    public class KSCFactor : HealthFactor
    {
        public override string Name => "KSC";

        public override string Title => Localizer.Format("#KH_Factor_KSC");

        public override bool ConstantForUnloaded => false;

        public override bool ShownInEditor => false;

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.KSCFactor;

        public override double ChangePerDay(KerbalHealthStatus khs) =>
            khs.ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available && !khs.IsTrainingAtKSC ? BaseChangePerDay : 0;
    }
}
