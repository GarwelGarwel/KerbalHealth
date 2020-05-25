using KSP.Localization;

namespace KerbalHealth
{
    class MicrogravityFactor : HealthFactor
    {
        public override string Name => "Microgravity";

        public override string Title => Localizer.Format("#KH_Microgravity");//Microgravity

        public override double BaseChangePerDay => KerbalHealthFactorsSettings.Instance.MicrogravityFactor;

        public override double ChangePerDay(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
                return IsEnabledInEditor() ? BaseChangePerDay : 0;
            if (Core.KerbalHealthList[pcm] == null)
            {
                Core.Log(pcm.name + " not found in KerbalHealthList. The list has " + Core.KerbalHealthList.Count + " records.", LogLevel.Error);
                return 0;
            }
            if (Core.KerbalVessel(pcm) == null)
            {
                Core.Log("MicrogravityFactor.ChangePerDay: Core.KerbalVessel(pcm) is null for " + pcm.name + "! EVA is " + Core.KerbalHealthList[pcm].IsOnEVA, LogLevel.Error);
                return 0;
            }
            if ((Core.KerbalVessel(pcm).situation & (Vessel.Situations.ORBITING | Vessel.Situations.SUB_ORBITAL | Vessel.Situations.ESCAPING)) != 0)
            {
                Core.Log("Microgravity is on due to being in a " + Core.KerbalVessel(pcm).situation + " situation.");
                return BaseChangePerDay;
            }
            if (pcm.geeForce < 0.1)
            {
                Core.Log("Microgravity is on due to g = " + pcm.geeForce.ToString("F2"));
                return BaseChangePerDay;
            }
            Core.Log("Microgravity is off, g = " + pcm.geeForce);
            return 0;
        }
    }
}
