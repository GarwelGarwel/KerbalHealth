using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalHealth
{
    /// <summary>
    /// Keeps modifiers introduced by vessel parts etc.
    /// </summary>
    public class HealthModifierSet
    {
        /// <summary>
        /// Cache of processed vessels, refreshed at every update
        /// </summary>
        public static Dictionary<Guid, HealthModifierSet> VesselCache { get; set; } = new Dictionary<Guid, HealthModifierSet>();

        /// <summary>
        /// Returns vessel health modifiers for the given vessel, either cached or calculated
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static HealthModifierSet GetVesselModifiers(Vessel v)
            => VesselCache.ContainsKey(v.id) ? VesselCache[v.id] : (VesselCache[v.id] = new HealthModifierSet(v));

        /// <summary>
        /// Returns vessel health modifiers for the vessel with the given kerbal
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static HealthModifierSet GetVesselModifiers(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
            {
                if (VesselCache.Count > 0)
                {
                    Core.Log("In editor and VesselHealthInfo found in cache. Retrieving.");
                    return VesselCache.First().Value;
                }
                Core.Log("In editor and VesselHealthInfo not found in cache. Calculating and adding to cache.");
                return VesselCache[Guid.Empty] = new HealthModifierSet(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.CrewCount);
            }
            return pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned ? new HealthModifierSet() : GetVesselModifiers(Core.KerbalVessel(pcm));
        }

        public int CrewCapacity { get; set; }
        public double HPChange { get; set; }
        public double Space { get; set; }
        public double RecuperationPower { get; set; }
        public double MaxRecuperaction { get; set; }
        public double Recuperation => Math.Min(RecuperationPower, MaxRecuperaction);
        public double Decay { get; set; }
        public double Shielding { get; set; }
        public double PartsRadiation { get; set; }
        public double ExposureMultiplier { get; set; } = 1;
        public double ShelterExposure { get; set; } = 1;
        public double Exposure => GetExposure(Shielding, CrewCapacity);
        public Dictionary<string, double> BonusSums { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> FreeMultipliers { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> MinMultipliers { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> MaxMultipliers { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Returns exposure provided by shielding
        /// </summary>
        /// <param name="shielding">Total shielding</param>
        /// <param name="crew">Crew capacity</param>
        /// <returns></returns>
        public static double GetExposure(double shielding, double crew)
            => Math.Pow(2, -shielding * KerbalHealthRadiationSettings.Instance.ShieldingEffect / Math.Pow(crew, 2f / 3));

        /// <summary>
        /// Returns effective multiplier for the given factor
        /// </summary>
        /// <param name="factorId"></param>
        /// <param name="crewCount"></param>
        /// <returns></returns>
        public double GetMultiplier(string factorId, int crewCount)
        {
            double res = 1 - BonusSums[factorId] / crewCount;
            res = res < 1 ? Math.Max(res, MinMultipliers[factorId]) : Math.Min(res, MaxMultipliers[factorId]);
            return res * FreeMultipliers[factorId];
        }

        /// <summary>
        /// Returns amoung of shielding provided by resources held in the part
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double GetResourceShielding(Part p)
        {
            double s = 0;
            foreach (KeyValuePair<int, double> res in Core.ResourceShielding)
            {
                p.GetConnectedResourceTotals(res.Key, ResourceFlowMode.NO_FLOW, out double amount, out double maxAmount);
                if (amount != 0)
                    Core.Log("Part " + p.name + " contains " + amount + " / " + maxAmount + " of shielding resource " + res.Key);
                s += res.Value * amount;
            }
            return s;
        }

        /// <summary>
        /// Returns radiation exposure in the specific part
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static double GetPartExtendedExposure(Part part)
        {
            if (part.CrewCapacity == 0)
                return 1;

            double s = 0;
            List<Part> parts = new List<Part>() { part };
            if (part.parent != null)
                parts.Add(part.parent);
            parts.AddRange(part.children);
            List<ModuleKerbalHealth> modules = new List<ModuleKerbalHealth>();
            foreach (Part p in parts)
                if ((p.CrewCapacity == 0) || (p == part))
                {
                    modules.AddRange(p.FindModulesImplementing<ModuleKerbalHealth>());
                    s += GetResourceShielding(p);
                }

            foreach (ModuleKerbalHealth m in modules)
                if (m.IsModuleActive)
                    s += m.shielding;
            return GetExposure(s, part.CrewCapacity);
        }

        /// <summary>
        /// Checks a part for its effects on the kerbal
        /// </summary>
        /// <param name="part"></param>
        /// <param name="crew"></param>
        public void ProcessPart(Part part, bool crewInPart)
        {
            if (part == null)
            {
                Core.Log("HealthModifierSet: 'part' is null. Unless the kerbal is on EVA, this is probably an error.", LogLevel.Important);
                return;
            }
            foreach (ModuleKerbalHealth mkh in part.FindModulesImplementing<ModuleKerbalHealth>())
                if (mkh.IsModuleActive && (!mkh.partCrewOnly ^ crewInPart))
                {
                    Core.Log("Processing " + mkh.Title + " Module in " + part.name + ".");
                    Core.Log("PartCrewOnly: " + mkh.partCrewOnly + "; CrewInPart: " + crewInPart + "; condition: " + (!mkh.partCrewOnly ^ crewInPart));
                    HPChange += mkh.hpChangePerDay;
                    Space += mkh.space;
                    if (mkh.recuperation != 0)
                    {
                        RecuperationPower += mkh.RecuperationPower;
                        Core.Log("Module's recuperation power = " + mkh.RecuperationPower);
                        MaxRecuperaction = Math.Max(MaxRecuperaction, mkh.recuperation);
                    }
                    Decay += mkh.DecayPower;

                    // Processing factor multiplier
                    if ((mkh.multiplier != 1) && (mkh.MultiplyFactor != null))
                    {
                        if (mkh.crewCap > 0)
                            BonusSums[mkh.multiplyFactor] += (1 - mkh.multiplier) * Math.Min(mkh.crewCap, mkh.CappedAffectedCrewCount);
                        else FreeMultipliers[mkh.MultiplyFactor.Name] *= mkh.multiplier;
                        if (mkh.multiplier > 1)
                            MaxMultipliers[mkh.MultiplyFactor.Name] = Math.Max(MaxMultipliers[mkh.MultiplyFactor.Name], mkh.multiplier);
                        else MinMultipliers[mkh.MultiplyFactor.Name] = Math.Min(MinMultipliers[mkh.MultiplyFactor.Name], mkh.multiplier);
                    }
                    Core.Log((HPChange != 0 ? "HP change after this module: " + HPChange + ". " : "") + (mkh.MultiplyFactor != null ? "Bonus to " + mkh.MultiplyFactor.Name + ": " + BonusSums[mkh.MultiplyFactor.Name] + ". Free multiplier: " + FreeMultipliers[mkh.MultiplyFactor.Name] + "." : ""));
                    Shielding += mkh.shielding;
                    if (mkh.shielding != 0)
                        Core.Log("Shielding of this module is " + mkh.shielding + ".");
                    PartsRadiation += mkh.radioactivity;
                    if (mkh.radioactivity != 0)
                        Core.Log("Radioactive emission of this module is " + mkh.radioactivity);
                }
        }

        /// <summary>
        /// Processes several parts and also records their RadiationShielding values
        /// </summary>
        /// <param name="parts"></param>
        public void ProcessParts(List<Part> parts, int crew)
        {
            Core.Log("Processing " + parts.Count + " parts...");
            CrewCapacity = 0;
            List<PartExposure> exposures = new List<PartExposure>();
            foreach (Part p in parts)
            {
                ProcessPart(p, false);
                Shielding += GetResourceShielding(p);
                if (p.CrewCapacity > 0)
                {
                    Core.Log("Possible shelter part: " + p.name + " with exposure " + GetPartExtendedExposure(p).ToString("P1"));
                    exposures.Add(new PartExposure(p));
                    CrewCapacity += p.CrewCapacity;
                }
                exposures.Sort();
            }

            // Calculating shelter exposure
            double x = 0;
            int i = 0;
            for (int c = 0; i < exposures.Count; i++)
            {
                Core.Log("Part " + exposures[i].Part.name + " with exposure " + exposures[i].Exposure.ToString("P1") + " and crew cap " + exposures[i].Part.CrewCapacity);
                x += exposures[i].Exposure * Math.Min(exposures[i].Part.CrewCapacity, crew - c);
                c += exposures[i].Part.CrewCapacity;
                if (c >= crew)
                    break;
            }
            Core.Log("Average exposure in top " + (i + 1) + " parts is " + (x / crew).ToString("P1") + "; general vessel exposure is " + Exposure.ToString("P1"));
            ShelterExposure = Math.Min(x / crew, Exposure);
        }

        /// <summary>
        /// Returns a text description of all significant values
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string res = "";
            if (HPChange != 0)
                res += "\nHP change per day: " + HPChange.ToString("F2");
            if (Space != 0)
                res += "\nSpace: " + Space.ToString("F1");
            if (RecuperationPower != 0)
                res += "\nRecuperation Power: " + RecuperationPower.ToString("F1") + "% (max " + MaxRecuperaction.ToString("F1") + "%)";
            if (Decay != 0)
                res += "\nDecay: " + Shielding.ToString("F1") + "%";
            if (Shielding != 0)
                res += "\nShielding: " + Shielding.ToString("F1");
            if (PartsRadiation != 0)
                res += "\nParts radiation: " + PartsRadiation.ToString("F0");

            foreach (HealthFactor f in Core.Factors)
            {
                if (BonusSums[f.Name] != 0)
                    res += "\n" + f.Name + " bonus sum: " + BonusSums[f.Name];
                if (FreeMultipliers[f.Name] != 1)
                    res += "\n" + f.Name + " free multiplier: " + FreeMultipliers[f.Name];
                if (MinMultipliers[f.Name] != 1)
                    res += "\n" + f.Name + " min multiplier: " + MinMultipliers[f.Name];
                if (MaxMultipliers[f.Name] != 1)
                    res += "\n" + f.Name + " max multiplier: " + MaxMultipliers[f.Name];
            }

            if (BonusSums["All"] != 0)
                res += "\nWildcard bonus sum: " + BonusSums["All"];
            if (FreeMultipliers["All"] != 1)
                res += "\nWildcard free multiplier: " + FreeMultipliers["All"];
            if (MinMultipliers["All"] != 1)
                res += "\nWildcard min multiplier: " + MinMultipliers["All"];
            if (MaxMultipliers["All"] != 1)
                res += "\nWildcard max multiplier: " + MaxMultipliers["All"];

            return res.Trim();
        }

        /// <summary>
        /// Returns a deep copy of the instance
        /// </summary>
        /// <returns></returns>
        public HealthModifierSet Clone()
        {
            HealthModifierSet hms = (HealthModifierSet)this.MemberwiseClone();
            hms.BonusSums = new Dictionary<string, double>(BonusSums);
            hms.FreeMultipliers = new Dictionary<string, double>(FreeMultipliers);
            hms.MinMultipliers = new Dictionary<string, double>(MinMultipliers);
            hms.MaxMultipliers = new Dictionary<string, double>(MaxMultipliers);
            return hms;
        }

        public HealthModifierSet()
        {
            foreach (HealthFactor f in Core.Factors)
            {
                BonusSums[f.Name] = 0;
                FreeMultipliers[f.Name] = 1;
                MinMultipliers[f.Name] = 1;
                MaxMultipliers[f.Name] = 1;
            }

            BonusSums["All"] = 0;
            FreeMultipliers["All"] = 1;
            MinMultipliers["All"] = 1;
            MaxMultipliers["All"] = 1;
        }

        public HealthModifierSet(Vessel v) : this() => ProcessParts(v?.Parts, v.GetCrewCount());

        public HealthModifierSet(List<Part> parts, int crew) : this() => ProcessParts(parts, crew);

        class PartExposure : IComparable<PartExposure>
        {
            public Part Part { get; set; }
            public double Exposure { get; private set; }

            public int CompareTo(PartExposure other) => Exposure.CompareTo(other.Exposure);

            public PartExposure(Part p)
            {
                Part = p;
                Exposure = GetPartExtendedExposure(p);
            }
        }
    }
}
