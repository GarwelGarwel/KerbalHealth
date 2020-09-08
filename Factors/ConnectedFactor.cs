using KSP.Localization;

namespace KerbalHealth
{
    public class ConnectedFactor : HealthFactor
    {
        public override string Name => "Connected";

        public override string Title => Localizer.Format("#KH_Factor_Connected");//Connected

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.ConnectedFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            return (pcm.IsLoaded() && (pcm.GetVessel().Connection != null) && pcm.GetVessel().Connection.IsConnectedHome)
                ? BaseChangePerDay
                : 0;
        }
    }
}
