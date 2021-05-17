using KSP.Localization;

namespace KerbalHealth
{
    public class ConnectedFactor : HealthFactor
    {
        public override string Name => "Connected";

        public override string Title => Localizer.Format("#KH_Factor_Connected");//Connected

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.ConnectedFactor;

        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            Vessel v = khs.PCM.GetVessel();
            return (v.loaded && v.Connection != null && v.Connection.IsConnectedHome)
                ? BaseChangePerDay
                : 0;
        }
    }
}
