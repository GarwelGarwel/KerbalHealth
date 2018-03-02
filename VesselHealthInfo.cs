using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    /// <summary>
    /// Keeps modifiers introduced by vessel parts etc.
    /// </summary>
    public class VesselHealthInfo
    {
        /// <summary>
        /// Cache of processed vessels, refreshed at every update
        /// </summary>
        public static Dictionary<Guid, VesselHealthInfo> Cache = new Dictionary<Guid, VesselHealthInfo>();

        /// <summary>
        /// Returns vessel health info for the given vessel, either cached or calculated
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static VesselHealthInfo GetVesselInfo(Vessel v)
        {
            if (Cache.ContainsKey(v.id)) return Cache[v.id];
            return Cache[v.id] = new VesselHealthInfo(v);
        }

        /// <summary>
        /// Returns vessel health info for the vessel with the given kerbal
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static VesselHealthInfo GetVesselInfo(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
            {
                if (Cache.Count > 0)
                {
                    Core.Log("In editor and VesselHealthInfo found in cache. Retrieving.");
                    return Cache.First().Value;
                }
                Core.Log("In editor and VesselHealthInfo not found in cache. Calculating and adding to cache.");
                return Cache[Guid.Empty] = new VesselHealthInfo(EditorLogic.SortedShipList);
            }
            return GetVesselInfo(Core.KerbalVessel(pcm));
        }

        public double HPChange { get; set; }
        public double Space { get; set; }
        public double RecuperationPower { get; set; }
        public double MaxRecuperaction { get; set; }
        public double Recuperation => Math.Min(RecuperationPower, MaxRecuperaction);
        public double Decay { get; set; }
        public double Shielding { get; set; }
        public double PartsRadiation { get; set; }
        public double ExposureModifier { get; set; }
        public Dictionary<string, double> BonusSums { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> FreeMultipliers { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> MinMultipliers { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> MaxMultipliers { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Returns effective multiplier for the given factor
        /// </summary>
        /// <param name="factorId"></param>
        /// <param name="crewCount"></param>
        /// <returns></returns>
        public double GetMultiplier(string factorId, int crewCount)
        {
            double res = 1 - BonusSums[factorId] / crewCount;
            if (res < 1) res = Math.Max(res, MinMultipliers[factorId]); else res = Math.Min(res, MaxMultipliers[factorId]);
            return res * FreeMultipliers[factorId];
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
                Core.Log("VesselHealthInfo: 'part' is null. Unless the kerbal is on EVA, this is probably an error.", Core.LogLevel.Important);
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
                        if (mkh.crewCap > 0) BonusSums[mkh.multiplyFactor] += (1 - mkh.multiplier) * Math.Min(mkh.crewCap, mkh.CappedAffectedCrewCount);
                        else FreeMultipliers[mkh.MultiplyFactor.Name] *= mkh.multiplier;
                        if (mkh.multiplier > 1) MaxMultipliers[mkh.MultiplyFactor.Name] = Math.Max(MaxMultipliers[mkh.MultiplyFactor.Name], mkh.multiplier);
                        else MinMultipliers[mkh.MultiplyFactor.Name] = Math.Min(MinMultipliers[mkh.MultiplyFactor.Name], mkh.multiplier);
                    }
                    Core.Log((HPChange != 0 ? "HP change after this module: " + HPChange + ". " : "") + (mkh.MultiplyFactor != null ? "Bonus to " + mkh.MultiplyFactor.Name + ": " + BonusSums[mkh.MultiplyFactor.Name] + ". Free multiplier: " + FreeMultipliers[mkh.MultiplyFactor.Name] + "." : ""));
                    Shielding += mkh.shielding;
                    if (mkh.shielding != 0) Core.Log("Shielding of this module is " + mkh.shielding + ".");
                    PartsRadiation += mkh.radioactivity;
                    if (mkh.radioactivity != 0) Core.Log("Radioactive emission of this module is " + mkh.radioactivity);
                }
        }

        /// <summary>
        /// Processes several parts and also records their RadiationShielding values
        /// </summary>
        /// <param name="parts"></param>
        public void ProcessParts(List<Part> parts)
        {
            Core.Log("Processing " + parts.Count + " parts...");
            foreach (Part p in parts)
            {
                ProcessPart(p, false);
                foreach (KeyValuePair<int, double> res in Core.ResourceShielding)
                {
                    p.GetConnectedResourceTotals(res.Key, ResourceFlowMode.NO_FLOW, out double amount, out double maxAmount);
                    if (amount != 0) Core.Log("Part " + p.name + " contains " + amount + " / " + maxAmount + " of shielding resource " + res.Key);
                    Shielding += res.Value * amount;
                }
            }
        }

        /// <summary>
        /// Returns a text description of all significant values
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string res = "";
            if (HPChange != 0) res += "\nHP change per day: " + HPChange.ToString("F2");
            if (Space != 0) res += "\nSpace: " + Space.ToString("F1");
            if (RecuperationPower != 0) res += "\nRecuperation Power: " + RecuperationPower.ToString("F1") + "% (max " + MaxRecuperaction.ToString("F1") + "%)";
            if (Decay != 0) res += "\nDecay: " + Shielding.ToString("F1") + "%";
            if (Shielding != 0) res += "\nShielding: " + Shielding.ToString("F1");
            if (PartsRadiation != 0) res += "\nParts radiation: " + PartsRadiation.ToString("F0");
            foreach (HealthFactor f in Core.Factors)
            {
                if (BonusSums[f.Name] != 0) res += "\n" + f.Name + " bonus sum: " + BonusSums[f.Name];
                if (FreeMultipliers[f.Name] != 1) res += "\n" + f.Name + " free multiplier: " + FreeMultipliers[f.Name];
                if (MinMultipliers[f.Name] != 1) res += "\n" + f.Name + " min multiplier: " + MinMultipliers[f.Name];
                if (MaxMultipliers[f.Name] != 1) res += "\n" + f.Name + " max multiplier: " + MaxMultipliers[f.Name];
            }
            if (BonusSums["All"] != 0) res += "\nWildcard bonus sum: " + BonusSums["All"];
            if (FreeMultipliers["All"] != 1) res += "\nWildcard free multiplier: " + FreeMultipliers["All"];
            if (MinMultipliers["All"] != 1) res += "\nWildcard min multiplier: " + MinMultipliers["All"];
            if (MaxMultipliers["All"] != 1) res += "\nWildcard max multiplier: " + MaxMultipliers["All"];
            return res.Trim();
        }

        /// <summary>
        /// Returns a deep copy of the instance
        /// </summary>
        /// <returns></returns>
        public VesselHealthInfo Clone()
        {
            VesselHealthInfo vhi = (VesselHealthInfo)this.MemberwiseClone();
            vhi.BonusSums = new Dictionary<string, double>(BonusSums);
            vhi.FreeMultipliers = new Dictionary<string, double>(FreeMultipliers);
            vhi.MinMultipliers = new Dictionary<string, double>(MinMultipliers);
            vhi.MaxMultipliers = new Dictionary<string, double>(MaxMultipliers);
            return vhi;
        }

        public VesselHealthInfo()
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

        public VesselHealthInfo(Vessel v) : this() => ProcessParts(v.Parts);

        public VesselHealthInfo(List<Part> parts) : this() => ProcessParts(parts);
    }
}
