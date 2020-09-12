using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalHealth
{
    /// <summary>
    /// Keeps modifiers introduced by vessel, parts, quirks or conditions
    /// </summary>
    public class HealthEffect
    {
        /// <summary>
        /// Cache of processed vessels, refreshed at every update
        /// </summary>
        public static Dictionary<Guid, HealthEffect> VesselCache { get; set; } = new Dictionary<Guid, HealthEffect>();

        public double HPChange { get; set; }

        public double MaxHP { get; set; } = 1;

        // Max HP multiplier, 1 means no change, 2 means 200% etc.
        public double MaxHPBonus { get; set; } = 0;

        // Max HP change
        public double ExhaustedStart { get; set; } = 1;

        // Exhausted start level multiplier
        public double ExhaustedEnd { get; set; } = 1;

        public double Space { get; set; }

        public double Recuperation { get; set; }

        public double MaxRecuperaction { get; set; }

        public double EffectiveRecuperation => Math.Min(Recuperation, MaxRecuperaction);

        public double Decay { get; set; }

        public double Shielding { get; set; }

        public double Radioactivity { get; set; }

        public double Exposure { get; set; } = 1;

        public double ShelterExposure { get; set; } = 1;

        public double AccidentChance { get; set; } = 1;

        // Accident chance multiplier
        public double PanicAttackChance { get; set; } = 1;

        // Panic attack chance multiplier
        public double SicknessChance { get; set; } = 1;

        // Getting infected/sick chance multiplier
        public double CureChance { get; set; } = 1;

        // Sickness cure chance multiplier
        public double LoseImmunityChance { get; set; } = 1;

        public ConfigNode ConfigNode
        {
            get
            {
                ConfigNode node = new ConfigNode("HEALTH_MODIFIERS");
                if (HPChange != 0)
                    node.AddValue("hpChange", HPChange);
                if (MaxHP != 1)
                    node.AddValue("maxHP", MaxHP);
                if (MaxHPBonus != 0)
                    node.AddValue("maxHPBonus", MaxHPBonus);
                if (ExhaustedStart != 1)
                    node.AddValue("exhaustedStart", ExhaustedStart);
                if (ExhaustedEnd != 1)
                    node.AddValue("exhaustedEnd", ExhaustedEnd);
                if (Space != 0)
                    node.AddValue("space", Space);
                if (Recuperation != 0)
                    node.AddValue("recuperation", Recuperation);
                if (MaxRecuperaction != 0)
                    node.AddValue("maxRecuperation", MaxRecuperaction);
                if (Decay != 0)
                    node.AddValue("decay", Decay);
                if (Shielding != 0)
                    node.AddValue("shielding", Shielding);
                if (Radioactivity != 0)
                    node.AddValue("radioactivity", Radioactivity);
                if (Exposure != 1)
                    node.AddValue("exposure", Exposure);
                if (ShelterExposure != 1)
                    node.AddValue("shelterExposure", ShelterExposure);
                if (AccidentChance != 1)
                    node.AddValue("accidentChance", AccidentChance);
                if (PanicAttackChance != 1)
                    node.AddValue("panicAttackChance", PanicAttackChance);
                if (SicknessChance != 1)
                    node.AddValue("sicknessChance", SicknessChance);
                if (CureChance != 1)
                    node.AddValue("cureChance", CureChance);
                if (LoseImmunityChance != 1)
                    node.AddValue("loseImmunityChance", LoseImmunityChance);
                foreach (FactorMultiplier fm in FactorMultipliers.Where(fm => !fm.IsTrivial))
                    node.AddNode(fm.ConfigNode);
                return node;
            }
            set
            {
                HPChange = value.GetDouble("hpChange");
                MaxHP = value.GetDouble("maxHP", 1);
                MaxHPBonus = value.GetDouble("maxHPBonus");
                ExhaustedStart = value.GetDouble("exhaustedStart", 1);
                ExhaustedEnd = value.GetDouble("exhaustedEnd", 1);
                Space = value.GetDouble("space");
                Recuperation = value.GetDouble("recuperation");
                MaxRecuperaction = value.GetDouble("maxRecuperation");
                Decay = value.GetDouble("decay");
                Shielding = value.GetDouble("shielding");
                Radioactivity = value.GetDouble("radioactivity");
                Exposure = value.GetDouble("exposure", 1);
                ShelterExposure = value.GetDouble("shelterExposure", 1);
                AccidentChance = value.GetDouble("accidentChance", 1);
                PanicAttackChance = value.GetDouble("panicAttackChance", 1);
                SicknessChance = value.GetDouble("sicknessChance", 1);
                CureChance = value.GetDouble("cureChance", 1);
                LoseImmunityChance = value.GetDouble("loseImmunityChance", 1);
                foreach (FactorMultiplier fm in value.GetNodes(FactorMultiplier.ConfigNodeName).Select(n => new FactorMultiplier(n)))
                    FactorMultipliers.Add(fm);
            }
        }

        FactorMultiplierList FactorMultipliers { get; set; } = new FactorMultiplierList();

        public HealthEffect()
        {
            foreach (HealthFactor f in Core.Factors)
                FactorMultipliers.Add(new FactorMultiplier(f.Name));
            FactorMultipliers.Add(new FactorMultiplier());
        }

        public HealthEffect(Vessel v) : this() => ProcessParts(v?.Parts, v.GetCrewCount());

        public HealthEffect(List<Part> parts, int crew) : this() => ProcessParts(parts, crew);

        /// <summary>
        /// Returns vessel health modifiers for the given vessel, either cached or calculated
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static HealthEffect GetVesselModifiers(Vessel v) =>
            VesselCache.ContainsKey(v.id) ? VesselCache[v.id] : (VesselCache[v.id] = new HealthEffect(v));

        /// <summary>
        /// Returns vessel health modifiers for the vessel with the given kerbal
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static HealthEffect GetVesselModifiers(ProtoCrewMember pcm)
        {
            if (Core.IsInEditor)
            {
                if (VesselCache.Count > 0)
                {
                    Core.Log("In editor and VesselHealthInfo found in cache. Retrieving.");
                    return VesselCache.First().Value;
                }
                Core.Log("In editor and VesselHealthInfo not found in cache. Calculating and adding to cache.");
                return VesselCache[Guid.Empty] = new HealthEffect(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.CrewCount);
            }
            return pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned ? new HealthEffect() : GetVesselModifiers(pcm.GetVessel());
        }

        /// <summary>
        /// Returns exposure provided by shielding
        /// </summary>
        /// <param name="shielding">Total shielding</param>
        /// <param name="crew">Crew capacity</param>
        /// <returns></returns>
        public static double GetExposure(double shielding, double crew) =>
            Math.Pow(2, -shielding * KerbalHealthRadiationSettings.Instance.ShieldingEffect / Math.Pow(crew, 2f / 3));

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
                    Core.Log($"Part {p.name} contains {amount:N1} / {maxAmount:N0} of shielding resource {res.Key}.");
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
            foreach (Part p in parts.Where(x => x.CrewCapacity == 0 || x == part))
            {
                modules.AddRange(p.FindModulesImplementing<ModuleKerbalHealth>());
                s += GetResourceShielding(p);
            }

            s += modules.Where(mkh => mkh.IsModuleActive).Sum(mkh => mkh.shielding);
            return GetExposure(s, part.CrewCapacity);
        }

        public static HealthEffect Combine(HealthEffect hms1, HealthEffect hms2)
        {
            HealthEffect res = new HealthEffect();
            res.HPChange = hms1.HPChange + hms2.HPChange;
            res.MaxHP = hms1.MaxHP * hms2.MaxHP;
            res.MaxHPBonus = hms1.MaxHPBonus + hms2.MaxHPBonus;
            res.ExhaustedStart = hms1.ExhaustedStart * hms2.ExhaustedStart;
            res.ExhaustedEnd = hms1.ExhaustedEnd * hms2.ExhaustedEnd;
            res.Space = hms1.Space + hms2.Space;
            res.Recuperation = hms1.Recuperation + hms2.Recuperation;
            res.MaxRecuperaction = Math.Max(hms1.MaxRecuperaction, hms2.MaxRecuperaction);
            res.Decay = hms1.Decay + hms2.Decay;
            res.Shielding = hms1.Shielding + hms2.Shielding;
            res.Radioactivity = hms1.Radioactivity + hms2.Radioactivity;
            res.Exposure = hms1.Exposure * hms2.Exposure;
            res.ShelterExposure = Math.Min(hms1.ShelterExposure, hms2.ShelterExposure);
            res.AccidentChance = hms1.AccidentChance * hms2.AccidentChance;
            res.PanicAttackChance = hms1.PanicAttackChance * hms2.PanicAttackChance;
            res.SicknessChance = hms1.SicknessChance * hms2.SicknessChance;
            res.CureChance = hms1.CureChance * hms2.CureChance;
            res.LoseImmunityChance = hms1.LoseImmunityChance * hms2.LoseImmunityChance;
            res.FactorMultipliers.Clear();
            for (int i = 0; i < hms1.FactorMultipliers.Count; i++)
                res.FactorMultipliers[i] = FactorMultiplier.Combine(hms1.FactorMultipliers[i], hms2.FactorMultipliers[i]);
            return res;
        }

        /// <summary>
        /// Checks a part for its effects on the kerbal
        /// </summary>
        /// <param name="part">Part to process</param>
        /// <param name="crewCount">Current crew</param>
        /// <param name="crewInPart">Whether the current kerbal is in this part</param>
        public void ProcessPart(Part part, int crewCount, bool crewInPart)
        {
            if (part == null)
            {
                Core.Log("HealthEffect: 'part' is null. Unless the kerbal is on EVA, this is probably an error.", LogLevel.Important);
                return;
            }

            foreach (ModuleKerbalHealth mkh in part.FindModulesImplementing<ModuleKerbalHealth>().Where(m => m.IsModuleActive && (!m.partCrewOnly ^ crewInPart)))
            {
                Core.Log($"Processing {mkh.Title} Module in {part.name}.");
                Core.Log($"PartCrewOnly: {mkh.partCrewOnly}; CrewInPart: {crewInPart}; condition: {(!mkh.partCrewOnly ^ crewInPart)}");
                HPChange += mkh.hpChangePerDay;
                Space += mkh.space;
                if (mkh.recuperation != 0)
                {
                    Recuperation += mkh.RecuperationPower;
                    Core.Log($"Module's recuperation power = {mkh.RecuperationPower}");
                    MaxRecuperaction = Math.Max(MaxRecuperaction, mkh.recuperation);
                }
                Decay += mkh.DecayPower;

                // Processing factor multiplier
                if (mkh.multiplier != 1)
                {
                    Core.Log($"Factor multiplier for {mkh.MultiplyFactor}: {mkh.multiplier:P1}.");
                    FactorMultiplier factorMultiplier = GetFactorMultiplier(mkh.multiplyFactor);
                    if (mkh.crewCap > 0)
                        factorMultiplier.AddRestrictedMultiplier(mkh.multiplier, mkh.crewCap, crewCount);
                    else factorMultiplier.AddFreeMultiplier(mkh.multiplier);
                }
                Shielding += mkh.shielding;
                if (mkh.shielding != 0)
                    Core.Log($"Shielding of this module is {mkh.shielding}.");
                Radioactivity += mkh.radioactivity;
                if (mkh.radioactivity != 0)
                    Core.Log($"Radioactive emission of this module is {mkh.radioactivity}.");
            }
        }

        /// <summary>
        /// Processes several parts and also records their RadiationShielding values
        /// </summary>
        /// <param name="parts"></param>
        public void ProcessParts(List<Part> parts, int crewCount)
        {
            Core.Log($"Processing {parts.Count} parts...");
            //CrewCapacity = 0;
            List<PartExposureComparer> exposures = new List<PartExposureComparer>();
            foreach (Part p in parts)
            {
                ProcessPart(p, crewCount, false);
                Shielding += GetResourceShielding(p);
                if (p.CrewCapacity > 0)
                {
                    Core.Log($"Possible shelter part: {p.name} with exposure {GetPartExtendedExposure(p):P1}");
                    exposures.Add(new PartExposureComparer(p));
                    //CrewCapacity += p.CrewCapacity;
                }
                exposures.Sort();
            }

            // Calculating shelter exposure
            double x = 0;
            int c = 0;
            for (int i = 0; i < exposures.Count; i++)
            {
                Core.Log($"Part {exposures[i].Part.name} with exposure {exposures[i].Exposure:P1} and crew cap {exposures[i].Part.CrewCapacity}.");
                x += exposures[i].Exposure * Math.Min(exposures[i].Part.CrewCapacity, crewCount - c);
                c += exposures[i].Part.CrewCapacity;
                if (c >= crewCount)
                    break;
            }
            Core.Log($"Average exposure in top {exposures.Count} parts is {x / crewCount:P1}; general vessel exposure is {Exposure:P1}.");
            ShelterExposure = Math.Min(x / crewCount, Exposure);
        }

        /// <summary>
        /// Returns a text description of all significant values
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string res = "";
            if (HPChange != 0)
                res = $"HP change per day: {HPChange:F2}";
            if (Space != 0)
                res += $"\nSpace: {Space:F1}";
            if (Recuperation != 0)
                res += $"\nRecuperation Power: {Recuperation:F1}% (max {MaxRecuperaction:F1}%)";
            if (Decay != 0)
                res += $"\nDecay: {Decay:F1}%";
            if (Shielding != 0)
                res += $"\nShielding: {Shielding:F1}";
            if (Radioactivity != 0)
                res += $"\nParts radiation: {Radioactivity:F0}";

            foreach (FactorMultiplier fm in FactorMultipliers.Where(fm => !fm.IsTrivial))
                res += $"\n{fm}";

            return res.Trim();
        }

        /// <summary>
        /// Returns a deep copy of the instance
        /// </summary>
        /// <returns></returns>
        public HealthEffect Clone()
        {
            HealthEffect hms = (HealthEffect)this.MemberwiseClone();
            hms.FactorMultipliers = new FactorMultiplierList();
            return hms;
        }

        FactorMultiplier GetFactorMultiplier(string factorName) =>
            FactorMultipliers.Find(fm =>
            fm.FactorName == factorName || (fm.FactorName == null && factorName.Equals("All", StringComparison.OrdinalIgnoreCase)));

        class PartExposureComparer : IComparable<PartExposureComparer>
        {
            public Part Part { get; set; }

            public double Exposure { get; private set; }

            public PartExposureComparer(Part p)
            {
                Part = p;
                Exposure = GetPartExtendedExposure(p);
            }

            public int CompareTo(PartExposureComparer other) => Exposure.CompareTo(other.Exposure);
        }
    }
}
