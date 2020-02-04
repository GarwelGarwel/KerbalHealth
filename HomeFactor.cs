using KSP.Localization;
namespace KerbalHealth
{
    class HomeFactor : HealthFactor
    {
        public override string Name => "Home";
        public override string Title => Localizer.Format("#KH_Home");//Home

        public override void ResetEnabledInEditor() => SetEnabledInEditor(false);

        public override double BaseChangePerDay => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().HomeFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor) return IsEnabledInEditor() ? BaseChangePerDay : 0;
            if (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
            {
                Core.Log("Home factor is off when kerbal is not assigned.");
                return 0;
            }
            if (Core.KerbalVessel(pcm).mainBody.isHomeWorld && (Core.KerbalVessel(pcm).altitude < 18000))
            {
                Core.Log("Home factor is on.");
                return BaseChangePerDay;
            }
            Core.Log("Home factor is off. Main body: " + Core.KerbalVessel(pcm).mainBody.name + "; altitude: " + Core.KerbalVessel(pcm).altitude + ".");
            return 0;
        }
    }
}
