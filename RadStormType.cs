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

        public ConfigNode ConfigNode
        {
            set
            {
                Name = value.GetString("name", "");
                Weight = value.GetDouble("weight");
                MeanMagnitude = value.GetDouble("magnitude");
                MagnitudeDispersion = value.GetDouble("magnitudeDispersion", 0.4);
                MeanVelocity = value.GetDouble("velocity", 500000);
                VelocityDispersion = value.GetDouble("velocityDispersion", 0.5);
            }
        }

        public RadStormType(ConfigNode node) => ConfigNode = node;

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
    }
}
