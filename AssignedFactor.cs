namespace KerbalHealth
{
    public class AssignedFactor : HealthFactor
    {
        public override string Name => "Assigned";

        public override string Title => "Assigned";

        public override double BaseChangePerDay => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().AssignedFactor;

        double ChangePerDayWithTraining(ProtoCrewMember pcm)
        {
            uint vesselId = Core.IsInEditor ? ShipConstruction.LoadShip().persistentId : Core.KerbalVessel(pcm).persistentId;
            KerbalHealthStatus khs = Core.KerbalHealthList.Find(pcm);
            double trainingLevel = khs.TrainedVessels.ContainsKey(vesselId) ? khs.TrainedVessels[vesselId] : 0;
            Core.Log("VesselId for Assigned Factor: " + vesselId + "; training level = " + trainingLevel);
            return BaseChangePerDay * trainingLevel;
        }

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor) return IsEnabledInEditor() ? ChangePerDayWithTraining(pcm) : 0;
            return (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) ? ChangePerDayWithTraining(pcm) : 0;
        }
    }
}
