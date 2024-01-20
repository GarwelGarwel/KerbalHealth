using KSP.Localization;

namespace KerbalHealth
{
    public class IsolationFactor : HealthFactor
    {
        public const float BaseChangePerDay_Default = -0.5f;

        public override string Name => "Isolation";

        public override string Title => Localizer.Format("#KH_Factor_Isolation");

        public override double BaseChangePerDay => BaseChangePerDay_Default * KerbalHealthFactorsSettings.Instance.IsolationEffect;

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            Vessel v = khs.ProtoCrewMember.GetVessel();
            return v != null && v.IsConnectedHome() ? 0 : BaseChangePerDay;
        }
    }
}
