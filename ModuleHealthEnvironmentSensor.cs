using KSP.Localization;

namespace KerbalHealth
{
    class ModuleHealthEnvironmentSensor : PartModule
    {
        [KSPField]
        public string type = "";

        [KSPField(guiActive = true, guiActiveEditor = false)]
        public float displayData;

        public override void OnStart(StartState state)
        {
            if (Core.IsInEditor)
                return;

            Core.Log($"ModuleHealthEnvironmentSensor.OnStart({state}) for {part.name}");
            switch (type.ToLowerInvariant())
            {
                case "radiation":
                    Fields["displayData"].guiName = "#KH_Radiation";
                    Fields["displayData"].guiUnits = "#KH_Module_perDay";
                    Fields["displayData"].guiFormat = "N0";
                    break;

                case "magnetosphere":
                    Fields["displayData"].guiName = "#KH_Module_MagnetosphereShielding";
                    Fields["displayData"].guiUnits = "";
                    Fields["displayData"].guiFormat = "P0";
                    break;

                default:
                    Fields["displayData"].guiActive = false;
                    break;
            }
        }

        public void FixedUpdate()
        {
            if (Core.IsInEditor)
                return;

            if (!isActiveAndEnabled || !vessel.IsControllable || !KerbalHealthGeneralSettings.Instance.modEnabled || !KerbalHealthRadiationSettings.Instance.RadiationEnabled || KerbalHealthRadiationSettings.Instance.UseKerbalismRadiation)
            {
                if (Fields["displayData"].guiActive)
                    Core.Log($"{vessel.vesselName} is not controllable, the ModuleHealthEnvironmentSensor in {part.name} is inactive, radiation feature is disabled or Kerbalism radiation is used. No environment data is displayed.");
                Fields["displayData"].guiActive = false;
                return;
            }
            else Fields["displayData"].guiActive = true;

            switch (type.ToLowerInvariant())
            {
                case "radiation":
                    displayData = (float)KerbalHealthStatus.GetCosmicRadiation(vessel);
                    break;

                case "magnetosphere":
                    displayData = 1 - (float)KerbalHealthStatus.GetMagnetosphereCoefficient(vessel);
                    break;

                default:
                    Fields["displayData"].guiActive = false;
                    break;
            }
        }

        public override string GetInfo() => Localizer.Format($"#KH_ModuleSensorInfo_{type.ToLowerInvariant()}");
    }
}
