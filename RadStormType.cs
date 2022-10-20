using System;

namespace KerbalHealth
{
    public class RadStormType
    {
        public string Name { get; set; }

        public float Weight { get; set; }

        /// <summary>
        /// Mean magnitude, measured in day-doses of solar radiation (defined in the settings, default = 2500 BED)
        /// </summary>
        public float MeanMagnitude { get; set; }

        public float MagnitudeDispersion { get; set; }

        public float MeanVelocity { get; set; }

        public float VelocityDispersion { get; set; }

        public RadStormType(ConfigNode node) => Load(node);

        public void Load(ConfigNode node)
        {
            Name = node.GetString("name", "");
            Weight = node.GetFloat("weight");
            MeanMagnitude = node.GetFloat("magnitude");
            MagnitudeDispersion = node.GetFloat("magnitudeDispersion", 0.4f);
            MeanVelocity = node.GetFloat("velocity", 500000);
            VelocityDispersion = node.GetFloat("velocityDispersion", 0.5f);
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
