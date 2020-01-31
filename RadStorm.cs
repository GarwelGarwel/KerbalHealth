using System;

namespace KerbalHealth
{

    /// <summary>
    /// Represents a (potential) solar radiation storm (CME)
    /// </summary>
    public class RadStorm
    {
        public enum TargetType { Body, Vessel, None };
        public TargetType Type { get; set; }
        
        string name;
        public string Name
        {
            get => (Type == TargetType.Vessel) ? Vessel?.vesselName : name;
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
            get => (Type == TargetType.Body) ? FlightGlobals.GetBodyByName(Name) : null;
            set
            {
                Type = TargetType.Body;
                Name = value?.name;
            }
        }

        public Vessel Vessel
        {
            get => (Type == TargetType.Vessel) ? FlightGlobals.PersistentVesselIds[VesselId] : null;
            set
            {
                Type = TargetType.Vessel;
                VesselId = value.persistentId;
            }
        }

        public object Target
        {
            get
            {
                switch (Type)
                {
                    case TargetType.Body: return CelestialBody;
                    case TargetType.Vessel: return Vessel;
                }
                return null;
            }
            set
            {
                if (value is CelestialBody) CelestialBody = (CelestialBody)value;
                else if (value is Vessel) Vessel = (Vessel)value;
                else Type = TargetType.None;
            }
        }

        public double DistanceFromSun
        {
            get
            {
                switch (Type)
                {
                    case TargetType.Body: return CelestialBody.orbit.altitude;
                    case TargetType.Vessel: return Vessel.distanceToSun;
                }
                return 0;
            }
        }

        public ConfigNode ConfigNode
        {
            get
            {
                if (Type == TargetType.None)
                {
                    Core.Log("Trying to save RadStormTarget of type None.", Core.LogLevel.Important);
                    return null;
                }
                ConfigNode n = new ConfigNode("RADSTORM");
                n.AddValue("type", Type.ToString());
                if (Type == TargetType.Vessel) n.AddValue("id", VesselId);
                else n.AddValue("body", Name);
                if (Magnitutde > 0) n.AddValue("magnitude", Magnitutde);
                if (Time > 0) n.AddValue("time", Time);
                return n;
            }
            set
            {
                Type = (TargetType)Enum.Parse(typeof(TargetType), value.GetValue("type"), true);
                if (Type == TargetType.Vessel)
                {
                    VesselId = Core.GetUInt(value, "id");
                    if (!FlightGlobals.PersistentVesselIds.ContainsKey(VesselId))
                    {
                        Core.Log("Vessel id " + VesselId + " not found in RadStormTarget.ConfigNode.set.", Core.LogLevel.Error);
                        Type = TargetType.None;
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

        public RadStorm()
        { }
    }
}
