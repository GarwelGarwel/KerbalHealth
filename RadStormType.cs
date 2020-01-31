using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class RadStormType
    {
        public string Name { get; set; }
        public double Weight { get; set; }
        public double MeanMagnitude { get; set; }
        public double MagnitudeDispersion { get; set; }
        public double MeanVelocity { get; set; }
        public double VelocityDispersion { get; set; }

        public double GetMagnitude() => MeanMagnitude * Math.Exp(Core.GetGaussian(0.4));
        public double GetVelocity() => MeanVelocity * Math.Exp(Core.GetGaussian(0.5));

        public ConfigNode ConfigNode
        {
            set
            {
                Name = Core.GetString(value, "name", "");
                Weight = Core.GetDouble(value, "weight");
                MeanMagnitude = Core.GetDouble(value, "magnitude");
                MagnitudeDispersion = Core.GetDouble(value, "magnitudeDispersion", 0.4);
                MeanVelocity = Core.GetDouble(value, "velocity", 500000);
                VelocityDispersion = Core.GetDouble(value, "velocityDispersion", 0.5);
            }
        }

        public RadStormType(ConfigNode node) => ConfigNode = node;
    }
}
