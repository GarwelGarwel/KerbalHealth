using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class BodyHealthConfig
    {
        /// <summary>
        /// Celestial body name (non-localized)
        /// </summary>
        public string Name { get; set; }

        public CelestialBody Body => FlightGlobals.GetBodyByName(Name);

        public bool HasMagneticField { get; set; }

        public double MagneticFieldStrength { get; set; }

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
                HasMagneticField = Core.GetBool(value, "hasMagneticField", HasMagneticField);
                MagneticFieldStrength = Core.GetDouble(value, "magneticField", MagneticFieldStrength);
            }
        }

        public override string ToString() => Name + "\r\nMagnetic Field: " + (HasMagneticField ? "Yes" : "No") + "\r\nMagnetic Field Strength:" + MagneticFieldStrength.ToString("P0");

        public BodyHealthConfig(CelestialBody body)
        {
            Name = body.bodyName;
            HasMagneticField = Core.IsPlanet(body);
            MagneticFieldStrength = 1;
        }

        public BodyHealthConfig(ConfigNode node) => ConfigNode = node;
    }
}
