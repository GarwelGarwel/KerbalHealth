using System;

namespace KerbalHealth
{

    /// <summary>
    /// Represents a (potential) solar radiation storm (CME)
    /// </summary>
    public class RadStorm
    {
        public enum TargetType { None, Body, Vessel };
        public TargetType Target { get; set; }
        
        string name;
        public string Name
        {
            get => (Target == TargetType.Vessel) ? Vessel?.vesselName : name;
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
            get => (Target == TargetType.Body) ? FlightGlobals.GetBodyByName(Name) : null;
            set
            {
                Target = TargetType.Body;
                Name = value?.name;
            }
        }

        public Vessel Vessel
        {
            get => (Target == TargetType.Vessel) && FlightGlobals.PersistentVesselIds.ContainsKey(VesselId) ? FlightGlobals.PersistentVesselIds[VesselId] : null;
            set
            {
                Target = TargetType.Vessel;
                VesselId = value.persistentId;
            }
        }

        public double DistanceFromSun
        {
            get
            {
                switch (Target)
                {
                    case TargetType.Body: return CelestialBody.orbit.altitude + Sun.Instance.sun.Radius;
                    case TargetType.Vessel: return Core.DistanceToSun(Vessel);
                }
                return 0;
            }
        }

        public bool Affects(ProtoCrewMember pcm)
        {
            Vessel v = Core.KerbalVessel(pcm);
            if (v == null) return false;
            if (Target == TargetType.Body)
                return Core.GetPlanet(v.mainBody)?.name == Name;
            if (Target == TargetType.Vessel)
                return v.persistentId == VesselId;
            return false;
        }

        public ConfigNode ConfigNode
        {
            get
            {
                if (Target == TargetType.None)
                {
                    Core.Log("Trying to save RadStormTarget of type None.", Core.LogLevel.Important);
                    return null;
                }
                ConfigNode n = new ConfigNode("RADSTORM");
                n.AddValue("target", Target.ToString());
                if (Target == TargetType.Vessel) n.AddValue("id", VesselId);
                else n.AddValue("body", Name);
                if (Magnitutde > 0) n.AddValue("magnitude", Magnitutde);
                if (Time > 0) n.AddValue("time", Time);
                return n;
            }
            set
            {
                try
                { Target = (TargetType)Enum.Parse(typeof(TargetType), value.GetValue("target"), true); }
                catch (Exception)
                {
                    Core.Log("No valid 'target' value found in RadStorm ConfigNode:\r\n" + value, Core.LogLevel.Error);
                    Target = TargetType.None;
                    return;
                }
                if (Target == TargetType.Vessel)
                {
                    VesselId = Core.GetUInt(value, "id");
                    if (!FlightGlobals.PersistentVesselIds.ContainsKey(VesselId))
                    {
                        Core.Log("Vessel id " + VesselId + " from RadStorm ConfigNode not found.", Core.LogLevel.Error);
                        Target = TargetType.None;
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
