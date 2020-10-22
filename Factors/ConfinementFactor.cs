using KSP.Localization;
using System;

namespace KerbalHealth
{
    public class ConfinementFactor : HealthFactor
    {
        public const string Id = "Confinement";

        public override string Name => Id;

        public override string Title => Localizer.Format("#KH_Factor_Confinement");//Confinement

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.ConfinementBaseFactor;

        public override double ChangePerDay(KerbalHealthStatus khs)
            => ((Core.IsInEditor && !IsEnabledInEditor()) || (!Core.IsInEditor && khs.IsOnEVA))
            ? 0
            : BaseChangePerDay * Core.GetCrewCount(khs.PCM) / Math.Max(khs.HealthEffects.Space, 0.1);
    }
}
