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

        public void Load(ConfigNode node)
        {
            Name = node.GetString("name");
            if (Name == null)
            {
                Core.Log("Missing 'name' key in body properties definition.", LogLevel.Error);
                return;
            }
            if (Body == null)
                Core.Log($"Body '{Name}' not found.", LogLevel.Important);
            Magnetosphere = node.GetDouble("magnetosphere", Magnetosphere);
            AtmosphericAbsorption = node.GetDouble("atmosphericAbsorption", AtmosphericAbsorption);
            Radioactivity = node.GetDouble("radioactivity", Radioactivity);
        }

        public PlanetHealthConfig(CelestialBody body)
        {
            Name = body.bodyName;
            Magnetosphere = body.IsPlanet() ? 1 : 0;
        }

        public PlanetHealthConfig(ConfigNode node) => Load(node);

        public override string ToString() =>
            $"{Name}\r\nMagnetosphere: {Magnetosphere:F2}\r\nAtmospheric Absorption: {AtmosphericAbsorption:F2}\r\nRadioactivity: {Radioactivity:F0}";
    }
}
