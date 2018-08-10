namespace KerbalHealth
{
    public class PlanetHealthConfig
    {
        /// <summary>
        /// Celestial body name (non-localized)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Returns a reference to CelestialBody
        /// </summary>
        public CelestialBody Body => FlightGlobals.GetBodyByName(Name);

        /// <summary>
        /// Exponential coefficient of amount of radiation blocked by magnetosphere (default = 1)
        /// </summary>
        public double Magnetosphere { get; set; } = 1;

        /// <summary>
        /// Exponential coefficient of amount of radiation blocked by the atmosphere (default = 1)
        /// </summary>
        public double AtmosphericAbsorption { get; set; } = 1;

        public ConfigNode ConfigNode
        {
            set
            {
                Name = Core.GetString(value, "name");
                if (Name == null)
                {
                    Core.Log("Missing 'name' key in body properties definition.", Core.LogLevel.Error);
                    return;
                }
                if (Body == null) Core.Log("Body '" + Name + "' not found.", Core.LogLevel.Important);
                Magnetosphere = Core.GetDouble(value, "magnetosphere", Magnetosphere);
                AtmosphericAbsorption = Core.GetDouble(value, "atmosphericAbsorption", AtmosphericAbsorption);
            }
        }

        public override string ToString() => Name + "\r\nMagnetosphere: " + Magnetosphere.ToString("F2") + "\r\nAtmospheric Absorption" + AtmosphericAbsorption.ToString("F2");

        public PlanetHealthConfig(CelestialBody body)
        {
            Name = body.bodyName;
            Magnetosphere = Core.IsPlanet(body) ? 1 : 0;
        }

        public PlanetHealthConfig(ConfigNode node) => ConfigNode = node;
    }
}
