using System;

namespace KerbalHealth
{
    public class CrowdedFactor : HealthFactor
    {
        public override string Name => "Crowded";

        public override double BaseChangePerDay => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().CrowdedBaseFactor;

        public override double ChangePerDay(ProtoCrewMember pcm) => ((Core.IsInEditor && !IsEnabledInEditor()) || Core.KerbalHealthList.Find(pcm).IsOnEVA) ? 0 : BaseChangePerDay * Core.GetCrewCount(pcm) / Math.Max(HealthModifierSet.GetVesselModifiers(pcm).Space, 0.1);
    }
}
