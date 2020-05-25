namespace KerbalHealth
{
    class ModuleHealthEnvironmentSensor : PartModule
    {
        [KSPField]
        public string type = "";  // "radiation" - shows current radiation level; "magnetosphere" - shows current magnetosphere coefficient

        [KSPField(guiActive = true, guiActiveEditor = false)]
        public float displayData;

        public override void OnStart(StartState state)
        {
            if (Core.IsInEditor)
                return;

            Core.Log("ModuleHealthEnvironmentSensor.OnStart(" + state + ") for " + part.name);
            switch (type.ToLower())
            {
                case "radiation":
                    Fields["displayData"].guiName = "#KH_Radiation";//Radiation
                    Fields["displayData"].guiUnits = "/day";//
                    Fields["displayData"].guiFormat = "N0";
                    break;
                case "magnetosphere":
                    Fields["displayData"].guiName = "Magnetosphere Shielding";//
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

            if (!isActiveAndEnabled || !vessel.IsControllable)
            {
                Core.Log(vessel.vesselName + " is not controllable or the ModuleHealthEnvironmentSensor is inactive. No data is displayed by " + part.name);
                Fields["displayData"].guiActive = false;
                return;
            }
            else Fields["displayData"].guiActive = true;

            switch (type.ToLower())
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

        public override string GetInfo() => "Measures " + type.ToLower();
    }
}
