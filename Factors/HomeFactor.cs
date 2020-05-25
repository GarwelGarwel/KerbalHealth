using KSP.Localization;

namespace KerbalHealth
{
    class HomeFactor : HealthFactor
    {
        public override string Name => "Home";

        public override string Title => Localizer.Format("#KH_Home");//Home

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.HomeFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            if (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
            {
                Core.Log("Home factor is off when kerbal is not assigned.");
                return 0;
            }
            CelestialBody body = Core.KerbalVessel(pcm)?.mainBody;
            if (body == null)
            {
                Core.Log("Could not find main body for " + pcm.name, LogLevel.Error);
                return 0;
            }
            if (body.isHomeWorld && (Core.KerbalVessel(pcm).altitude < body.scienceValues.flyingAltitudeThreshold))
            {
                Core.Log("Home factor is on.");
                return BaseChangePerDay;
            }
            Core.Log("Home factor is off. Main body: " + Core.KerbalVessel(pcm).mainBody.name + "; altitude: " + Core.KerbalVessel(pcm).altitude + ".");
            return 0;
        }
    }
}
