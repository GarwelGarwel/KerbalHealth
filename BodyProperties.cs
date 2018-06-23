using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class BodyProperties
    {
        /// <summary>
        /// Celestial body name (non-localized)
        /// </summary>
        public string Name { get; set; }

        public CelestialBody Body => FlightGlobals.GetBodyByName(Name);

        public bool HasMagneticField { get; set; }

        public double MagneticFieldStrength { get; set; } = 1;

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
                HasMagneticField = Core.GetBool(value, "hasMagneticField", true);
                MagneticFieldStrength = Core.GetDouble(value, "magneticField", 1);
            }
        }

        public BodyProperties(ConfigNode node) => ConfigNode = node;
    }
}
