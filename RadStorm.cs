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

        public uint VesselId { get; set; }
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
            get => (Target == RadStormTargetType.Vessel) && FlightGlobals.PersistentVesselIds.ContainsKey(VesselId) ? FlightGlobals.PersistentVesselIds[VesselId] : null;
            set
            {
                Target = RadStormTargetType.Vessel;
                VesselId = value.persistentId;
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
                        return Core.DistanceToSun(Vessel);
                }
                return 0;
            }
        }

        public bool Affects(ProtoCrewMember pcm)
        {
            Vessel v = Core.KerbalVessel(pcm);
            if (v == null)
                return false;
            if (Target == RadStormTargetType.Body)
                return Core.GetPlanet(v.mainBody)?.name == Name;
            if (Target == RadStormTargetType.Vessel)
                return v.persistentId == VesselId;
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
                try
                { Target = (RadStormTargetType)Enum.Parse(typeof(RadStormTargetType), value.GetValue("target"), true); }
                catch (ArgumentException)
                {
                    Core.Log("No valid 'target' value found in RadStorm ConfigNode:\r\n" + value, LogLevel.Error);
                    Target = RadStormTargetType.None;
                    return;
                }

                if (Target == RadStormTargetType.Vessel)
                {
                    VesselId = Core.GetUInt(value, "id");
                    if (!FlightGlobals.PersistentVesselIds.ContainsKey(VesselId))
                    {
                        Core.Log("Vessel id " + VesselId + " from RadStorm ConfigNode not found.", LogLevel.Error);
                        Target = RadStormTargetType.None;
                        return;
                    }
                }
                else Name = Core.GetString(value, "body");
                Magnitutde = Core.GetDouble(value, "magnitude");
                Time = Core.GetDouble(value, "time");
            }
        }

        public RadStorm(CelestialBody body) => CelestialBody = body;

        public RadStorm(Vessel vessel) => Vessel = vessel;

        public RadStorm(ConfigNode node) => ConfigNode = node;
    }
}
