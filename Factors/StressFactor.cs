using KSP.Localization;

namespace KerbalHealth
{
    public class StressFactor : HealthFactor
    {
        public override string Name => "Stress";

        public override string Title => Localizer.Format("#KH_Factor_Stress");

        public override bool Cachable => false;

        public override double BaseChangePerDay => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().StressFactor;

        /// <summary>
        /// Returns HP change per day due to stress at the current training level for the kerbal
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        double ChangePerDayActual(ProtoCrewMember pcm) => BaseChangePerDay * (1 - Core.KerbalHealthList[pcm].TrainingLevel);

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
                if (IsEnabledInEditor())
                    return (!KerbalHealthFactorsSettings.Instance.TrainingEnabled || KerbalHealthEditorReport.TrainingEnabled)
                        ? BaseChangePerDay * (1 - Core.TrainingCap)
                        : BaseChangePerDay;
                else return 0;
            return (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) ? ChangePerDayActual(pcm) : 0;
        }
    }
}
