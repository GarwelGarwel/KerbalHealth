using System;

namespace KerbalHealth
{
    public enum RadStormTargetType { None = 0, Body, Vessel };

    /// <summary>
    /// Represents a (potential) solar radiation storm (CME)
    /// </summary>
    public class RadStorm
    {
        public RadStormTargetType Target { get; set; }
        
        string name;
        public string Name
        {
            get => (Target == RadStormTargetType.Vessel) ? Vessel?.vesselName : name;
            set => name = value;
        }

        public string VesselId { get; set; }
        public double Magnitutde { get; set; }
        public double Time { get; set; }

        /// <summary>
        /// Must be a planet (i.e. orbiting the sun)
        /// </summary>
        public CelestialBody CelestialBody
        {
            get => (Target == RadStormTargetType.Body) ? FlightGlobals.GetBodyByName(Name) : null;
            set
            {
                Target = RadStormTargetType.Body;
                Name = value?.name;
            }
        }

        public Vessel Vessel
        {
            get => Target == RadStormTargetType.Vessel ? FlightGlobals.FindVessel(new Guid(VesselId)) : null;
            set
            {
                Target = RadStormTargetType.Vessel;
                VesselId = value.id.ToString();
            }
        }

        public double DistanceFromSun
        {
            get
            {
                switch (Target)
                {
                    case RadStormTargetType.Body:
                        return CelestialBody.orbit.altitude + Sun.Instance.sun.Radius;
                    case RadStormTargetType.Vessel:
                        return Vessel.GetDistanceToSun();
                }
                return 0;
            }
        }

        public bool Affects(ProtoCrewMember pcm)
        {
            Vessel v = pcm.GetVessel();
            if (v == null)
                return false;
            if (Target == RadStormTargetType.Body)
                return v.mainBody.GetPlanet()?.name == Name;
            if (Target == RadStormTargetType.Vessel)
                return v.id.ToString() == VesselId;
            return false;
        }

        public ConfigNode ConfigNode
        {
            get
            {
                if (Target == RadStormTargetType.None)
                {
                    Core.Log("Trying to save RadStormTarget of type None.", LogLevel.Important);
                    return null;
                }
                ConfigNode n = new ConfigNode("RADSTORM");
                n.AddValue("target", Target.ToString());
                if (Target == RadStormTargetType.Vessel)
                    n.AddValue("id", VesselId);
                else n.AddValue("body", Name);
                if (Magnitutde > 0)
                    n.AddValue("magnitude", Magnitutde);
                if (Time > 0)
                    n.AddValue("time", Time);
                return n;
            }

            set
            {
                if (Enum.TryParse(value.GetValue("target"), true, out RadStormTargetType radStormTargetType))
                    Target = radStormTargetType;
                else
                {
                    Core.Log("No valid 'target' value found in RadStorm ConfigNode:\r\n" + value, LogLevel.Error);
                    Target = RadStormTargetType.None;
                    return;
                }

                if (Target == RadStormTargetType.Vessel)
                {
                    VesselId = value.GetString("id");
                    if (FlightGlobals.FindVessel(new Guid(VesselId)) == null)
                    {
                        Core.Log("Vessel id " + VesselId + " from RadStorm ConfigNode not found.", LogLevel.Error);
                        Target = RadStormTargetType.None;
                        return;
                    }
                }
                else Name = value.GetString("body");
                Magnitutde = value.GetDouble("magnitude");
                Time = value.GetDouble("time");
            }
        }

        public RadStorm(CelestialBody body) => CelestialBody = body;

        public RadStorm(Vessel vessel) => Vessel = vessel;

        public RadStorm(ConfigNode node) => ConfigNode = node;
    }
}
