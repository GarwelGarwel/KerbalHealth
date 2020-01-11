using System.Collections.Generic;

namespace KerbalHealth
{
    public class StressFactor : HealthFactor
    {
        public override string Name => "Stress";

        public override string Title => "Stress";

        public override bool Cachable => false;

        public override double BaseChangePerDay => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().StressFactor;

        double ChangePerDayWithTraining(ProtoCrewMember pcm) => BaseChangePerDay * (1 - Core.KerbalHealthList.Find(pcm).TrainingLevel);

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor) return IsEnabledInEditor() ? BaseChangePerDay * (1 - Core.TrainingCap) : 0;
            return (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) ? ChangePerDayWithTraining(pcm) : 0;
        }
    }
}
