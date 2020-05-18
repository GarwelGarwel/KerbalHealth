using System;

namespace KerbalHealth
{
    public class RadStormType
    {
        public string Name { get; set; }
        public double Weight { get; set; }
        /// <summary>
        /// Mean magnitude, measured in day-doses of solar radiation (defined in the settings, default = 2500 BED)
        /// </summary>
        public double MeanMagnitude { get; set; }
        public double MagnitudeDispersion { get; set; }
        public double MeanVelocity { get; set; }
        public double VelocityDispersion { get; set; }

        /// <summary>
        /// Returns a random, Gaussian-dispersed magnitude of a radstorm in BED
        /// </summary>
        /// <returns></returns>
        public double GetMagnitude() => MeanMagnitude * Math.Exp(Core.GetGaussian(0.4)) * KerbalHealthRadiationSettings.Instance.SolarRadiation;

        /// <summary>
        /// Returns a random, Gaussian-dispersed velocity value for a storm, in m/s
        /// </summary>
        /// <returns></returns>
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
