using System.Collections.Generic;

namespace KerbalHealth
{
    public class AssignedFactor : HealthFactor
    {
        public override string Name => "Assigned";

        public override string Title => "Assigned";

        public override double BaseChangePerDay => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().AssignedFactor;

        double ChangePerDayWithTraining(ProtoCrewMember pcm)
        {
            double trainingLevel = Core.MaxTraining;
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().TrainingEnabled && !Core.IsInEditor)
            {
                List<ModuleKerbalHealth> trainingParts = Core.GetTrainingCapableParts(Core.KerbalVessel(pcm).Parts);
                KerbalHealthStatus khs = Core.KerbalHealthList.Find(pcm);
                double sumTraining = 0;
                foreach (ModuleKerbalHealth mkh in trainingParts)
                    if (khs.TrainedParts.ContainsKey(mkh.id))
                    {
                        Core.Log(pcm.name + " has " + khs.TrainedParts[mkh.id].ToString("P2") + " training for part " + mkh.part.name + " (id " + mkh.id + ")");
                        sumTraining += khs.TrainedParts[mkh.id];
                    }
                    else Core.Log(pcm.name + " is not trained for " + mkh.part.name + " (id " + mkh.id + ")");
                trainingLevel = (trainingParts.Count > 0) ? sumTraining / trainingParts.Count : 0;
                Core.Log("Overall training level for " + trainingParts.Count + " parts is " + trainingLevel.ToString("P2"));
            }
            return BaseChangePerDay * (1 - trainingLevel);
        }

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor) return IsEnabledInEditor() ? ChangePerDayWithTraining(pcm) : 0;
            return (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) ? ChangePerDayWithTraining(pcm) : 0;
        }
    }
}
