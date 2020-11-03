namespace KerbalHealth
{
    /// <summary>
    /// Used for custom configuration of celestial bodies' radiation shielding & emission
    /// </summary>
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

        /// <summary>
        /// Natural radiation level at sea level
        /// </summary>
        public double Radioactivity { get; set; } = 0;

        public ConfigNode ConfigNode
        {
            set
            {
                Name = value.GetString("name");
                if (Name == null)
                {
                    Core.Log("Missing 'name' key in body properties definition.", LogLevel.Error);
                    return;
                }
                if (Body == null)
                    Core.Log($"Body '{Name}' not found.", LogLevel.Important);
                Magnetosphere = value.GetDouble("magnetosphere", Magnetosphere);
                AtmosphericAbsorption = value.GetDouble("atmosphericAbsorption", AtmosphericAbsorption);
                Radioactivity = value.GetDouble("radioactivity", Radioactivity);
            }
        }

        public PlanetHealthConfig(CelestialBody body)
        {
            Name = body.bodyName;
            Magnetosphere = body.IsPlanet() ? 1 : 0;
        }

        public PlanetHealthConfig(ConfigNode node) => ConfigNode = node;

        public override string ToString() =>
            $"{Name}\r\nMagnetosphere: {Magnetosphere:F2}\r\nAtmospheric Absorption: {AtmosphericAbsorption:F2}\r\nRadioactivity: {Radioactivity:F0}";
    }
}
