using KSP.Localization;

namespace KerbalHealth
{
    class MicrogravityFactor : HealthFactor
    {
        public override string Name => "Microgravity";

        public override string Title => Localizer.Format("#KH_Factor_Microgravity");//Microgravity

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.MicrogravityFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            if (Core.KerbalHealthList[pcm] == null)
            {
                Core.Log($"{pcm.name} not found in KerbalHealthList. The list has {Core.KerbalHealthList.Count} records.", LogLevel.Error);
                return 0;
            }
            Vessel vessel = pcm.GetVessel();
            if (vessel == null)
            {
                Core.Log($"MicrogravityFactor.ChangePerDay: Core.GetVessel(pcm) is null for {pcm.name}! EVA is {Core.KerbalHealthList[pcm].IsOnEVA}.", LogLevel.Error);
                return 0;
            }
            if ((vessel.situation & (Vessel.Situations.ORBITING | Vessel.Situations.SUB_ORBITAL | Vessel.Situations.ESCAPING)) != 0)
            {
                Core.Log($"Microgravity is on due to being in a {vessel.situation} situation.");
                return BaseChangePerDay;
            }
            if (pcm.geeForce < 0.1)
            {
                Core.Log($"Microgravity is on due to g = {pcm.geeForce:F2}.");
                return BaseChangePerDay;
            }
            Core.Log($"Microgravity is off, g = {pcm.geeForce:F2}.");
            return 0;
        }
    }
}
