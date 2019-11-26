using KSP.Localization;
namespace KerbalHealth
{
    public class AssignedFactor : HealthFactor
    {
        public override string Name => "Assigned";

        public override string Title => Localizer.Format("#KH_Assigned");//"Assigned"

        public override double BaseChangePerDay => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().AssignedFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor) return IsEnabledInEditor() ? BaseChangePerDay : 0;
            return (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) ? BaseChangePerDay : 0;
        }
    }
}
