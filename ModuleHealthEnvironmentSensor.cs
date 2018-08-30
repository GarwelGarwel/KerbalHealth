namespace KerbalHealth
{
    class ModuleHealthEnvironmentSensor : PartModule
    {
        [KSPField]
        public string type = "";  // "radiation" - shows current radiation level; "magnetosphere" - shows current magnetosphere coefficient

        [KSPField(guiActive = true)]
        public float displayData;

        public override void OnStart(StartState state)
        {
            if (Core.IsInEditor) return;
            Core.Log("ModuleHealthEnvironmentSensor.OnStart(" + state + ") for " + part.name);
            switch (type.ToLower())
            {
                case "radiation":
                    Fields["displayData"].guiName = "Radiation";
                    Fields["displayData"].guiUnits = "/day";
                    Fields["displayData"].guiFormat = "F0";
                    break;
                case "magnetosphere":
                    Fields["displayData"].guiName = "Magnetosphere Shielding";
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
            if (Core.IsInEditor) return;
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
                    Core.Log("Displaying radiation of " + displayData);
                    break;
                case "magnetosphere":
                    displayData = 1 - (float)KerbalHealthStatus.GetMagnetosphereCoefficient(vessel);
                    Core.Log("Displaying magnetic field strength of " + displayData);
                    break;
                default:
                    Fields["displayData"].guiActive = false;
                    Core.Log("Unrecognized sensor type '" + type + "'!");
                    break;
            }
        }

        public override string GetInfo() => "Measures " + type.ToLower();
    }
}
