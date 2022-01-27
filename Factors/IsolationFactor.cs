using KSP.Localization;

namespace KerbalHealth
{
    public class IsolationFactor : HealthFactor
    {
        public override string Name => "Isolation";

        public override string Title => Localizer.Format("#KH_Factor_Isolation");

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.IsolationFactor;

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            Vessel v = khs.ProtoCrewMember.GetVessel();
            return (v.loaded && v.Connection != null && v.Connection.IsConnectedHome) ? 0 : BaseChangePerDay;
        }
    }
}
