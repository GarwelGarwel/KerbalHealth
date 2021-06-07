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

        public RadStormType(ConfigNode node) => Load(node);

        public void Load(ConfigNode node)
        {
            Name = node.GetString("name", "");
            Weight = node.GetDouble("weight");
            MeanMagnitude = node.GetDouble("magnitude");
            MagnitudeDispersion = node.GetDouble("magnitudeDispersion", 0.4);
            MeanVelocity = node.GetDouble("velocity", 500000);
            VelocityDispersion = node.GetDouble("velocityDispersion", 0.5);
        }

        /// <summary>
        /// Returns a random, Gaussian-dispersed magnitude of a radstorm in BED
        /// </summary>
        /// <returns></returns>
        public double GetRandomMagnitude() => MeanMagnitude * Math.Exp(Core.GetGaussian(0.4)) * KerbalHealthRadiationSettings.Instance.SolarRadiation;

        /// <summary>
        /// Returns a random, Gaussian-dispersed velocity value for a storm, in m/s
        /// </summary>
        /// <returns></returns>
        public double GetRandomVelocity() => MeanVelocity * Math.Exp(Core.GetGaussian(0.5));
    }
}
