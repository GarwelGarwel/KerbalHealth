using KSP.Localization;
using System;

namespace KerbalHealth
{
    public class ConfinementFactor : HealthFactor
    {
        public override string Name => "Confinement";

        public override string Title => Localizer.Format("#KH_Factor_Confinement");//Confinement

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.ConfinementBaseFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
            => ((Core.IsInEditor && !IsEnabledInEditor()) || Core.KerbalHealthList[pcm].IsOnEVA)
            ? 0
            : BaseChangePerDay * Core.GetCrewCount(pcm) / Math.Max(HealthEffect.GetVesselModifiers(pcm).Space, 0.1);
    }
}
