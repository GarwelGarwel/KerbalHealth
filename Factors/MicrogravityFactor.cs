using KSP.Localization;

namespace KerbalHealth
{
    class MicrogravityFactor : HealthFactor
    {
        public const float BaseChangePerDay_Default = -1;

        public override string Name => "Microgravity";

        public override string Title => Localizer.Format("#KH_Factor_Microgravity");//Microgravity

        public override double BaseChangePerDay => BaseChangePerDay_Default * KerbalHealthFactorsSettings.Instance.MicrogravityEffect;

        public override double ChangePerDay(KerbalHealthStatus khs)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            Vessel vessel = khs.ProtoCrewMember.GetVessel();
            if (vessel == null)
            {
                Core.Log($"MicrogravityEffect.ChangePerDay: Core.GetVessel(pcm) is null for {khs.Name}! EVA is {khs.IsOnEVA}.", LogLevel.Error);
                return 0;
            }
            if ((vessel.situation & (Vessel.Situations.ORBITING | Vessel.Situations.SUB_ORBITAL | Vessel.Situations.ESCAPING)) != 0)
            {
                Core.Log($"Microgravity is on due to being in a {vessel.situation} situation.");
                return BaseChangePerDay;
            }
            if (khs.ProtoCrewMember.geeForce < 0.1)
            {
                Core.Log($"Microgravity is on due to g = {khs.ProtoCrewMember.geeForce:F2}.");
                return BaseChangePerDay;
            }
            Core.Log($"Microgravity is off, g = {khs.ProtoCrewMember.geeForce:F2}.");
            return 0;
        }
    }
}
