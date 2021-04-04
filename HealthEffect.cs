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
        public const string ConfigNodeName = "HEALTH_EFFECTS";

        /// <summary>
        /// Cache of processed vessels, refreshed at every update
        /// </summary>
        public static Dictionary<Guid, HealthEffect> VesselCache { get; set; } = new Dictionary<Guid, HealthEffect>();

        public double HPChange { get; set; }

        public double MaxHP { get; set; } = 1;

        public double MaxHPBonus { get; set; } = 0;

        public double ExhaustedStart { get; set; } = 1;

        public double ExhaustedEnd { get; set; } = 1;

        public double Space { get; set; }

        public double Recuperation { get; set; }

        public double MaxRecuperaction { get; set; }

        public double EffectiveRecuperation => Math.Min(Recuperation, MaxRecuperaction);

        public double Decay { get; set; }

        public double Shielding { get; set; }

        public double Radioactivity { get; set; }

        public double ExposureMultiplier { get; set; } = 1;

        public double VesselExposure => GetExposure(Shielding, Math.Max(CrewCapacity, 1));

        public double ShelterExposure { get; set; } = 1;

        public int CrewCapacity { get; set; }

        public double AccidentChance { get; set; } = 1;

        public double PanicAttackChance { get; set; } = 1;

        public double SicknessChance { get; set; } = 1;

        public double CureChance { get; set; } = 1;

        public double LoseImmunityChance { get; set; } = 1;

        public ConfigNode ConfigNode
        {
            get
            {
                ConfigNode node = new ConfigNode(ConfigNodeName);
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
                if (ExposureMultiplier != 1)
                    node.AddValue("exposure", ExposureMultiplier);
                if (ShelterExposure != 1)
                    node.AddValue("shelterExposure", ShelterExposure);
                if (CrewCapacity != 0)
                    node.AddValue("crewCapacity", CrewCapacity);
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
                if (value == null)
                    return;
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
                ExposureMultiplier = value.GetDouble("exposure", 1);
                ShelterExposure = value.GetDouble("shelterExposure", 1);
                CrewCapacity = value.GetInt("crewCapacity");
                AccidentChance = value.GetDouble("accidentChance", 1);
                PanicAttackChance = value.GetDouble("panicAttackChance", 1);
                SicknessChance = value.GetDouble("sicknessChance", 1);
                CureChance = value.GetDouble("cureChance", 1);
                LoseImmunityChance = value.GetDouble("loseImmunityChance", 1);
                FactorMultipliers.Clear();
                foreach (FactorMultiplier fm in value.GetNodes(FactorMultiplier.ConfigNodeName).Select(n => new FactorMultiplier(n)))
                    FactorMultipliers.Add(fm);
            }
        }

        public FactorMultiplierList FactorMultipliers { get; set; } = new FactorMultiplierList();

        public HealthEffect()
        {
            foreach (HealthFactor f in Core.Factors)
                FactorMultipliers.Add(new FactorMultiplier(f));
            FactorMultipliers.Add(new FactorMultiplier());
        }

        public HealthEffect(ConfigNode configNode) => ConfigNode = configNode;

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
        /// <param name="crewCap">Crew capacity</param>
        /// <returns></returns>
        public static double GetExposure(double shielding, double crewCap) =>
            Math.Pow(2, -shielding * KerbalHealthRadiationSettings.Instance.ShieldingEffect / Math.Pow(crewCap, 2f / 3));

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

        /// <summary>
        /// Creates a new effect that combines effects from two objects
        /// </summary>
        /// <param name="effect1"></param>
        /// <param name="effect2"></param>
        /// <returns></returns>
        public static HealthEffect Combine(HealthEffect effect1, HealthEffect effect2)
        {
            if (effect1 == null)
                return effect2.Clone();
            else if (effect2 == null)
                return effect1.Clone();
            return effect1.Clone().CombineWith(effect2);
        }

        /// <summary>
        /// Adds effects from another effect to this one and returns this object
        /// </summary>
        /// <param name="effect"></param>
        /// <returns></returns>
        public HealthEffect CombineWith(HealthEffect effect)
        {
            if (effect == null)
                return this;
            HPChange += effect.HPChange;
            MaxHP *= effect.MaxHP;
            MaxHPBonus += effect.MaxHPBonus;
            ExhaustedStart *= effect.ExhaustedStart;
            ExhaustedEnd *= effect.ExhaustedEnd;
            Space += effect.Space;
            Recuperation += effect.Recuperation;
            MaxRecuperaction = Math.Max(MaxRecuperaction, effect.MaxRecuperaction);
            Decay += effect.Decay;
            Shielding += effect.Shielding;
            Radioactivity += effect.Radioactivity;
            ExposureMultiplier *= effect.ExposureMultiplier;
            ShelterExposure = Math.Min(ShelterExposure, effect.ShelterExposure);
            CrewCapacity += effect.CrewCapacity;
            AccidentChance *= effect.AccidentChance;
            PanicAttackChance *= effect.PanicAttackChance;
            SicknessChance *= effect.SicknessChance;
            CureChance *= effect.CureChance;
            LoseImmunityChance *= effect.LoseImmunityChance;
            FactorMultipliers.CombineWith(effect.FactorMultipliers);
            return this;
        }

        /// <summary>
        /// Checks a part for its effects on the kerbal
        /// </summary>
        /// <param name="part">Part to process</param>
        /// <param name="crewCount">Current crew</param>
        /// <param name="partCrewModules">Whether to analyze modules with partCrewOnly flag or without</param>
        public void ProcessPart(Part part, int crewCount, bool partCrewModules)
        {
            if (part == null)
            {
                Core.Log("HealthEffect: 'part' is null. Unless the kerbal is on EVA, this is probably an error.", LogLevel.Important);
                return;
            }

            foreach (ModuleKerbalHealth mkh in part.FindModulesImplementing<ModuleKerbalHealth>().Where(m => m.IsModuleActive && (m.partCrewOnly == partCrewModules)))
            {
                Core.Log($"Processing {mkh.Title} Module in {part.name}.");
                Core.Log($"PartCrewOnly: {mkh.partCrewOnly}; CrewInPart: {partCrewModules}; condition: {(!mkh.partCrewOnly ^ partCrewModules)}");
                HPChange += mkh.hpChangePerDay;
                Space += mkh.Space;
                if (mkh.recuperation != 0)
                {
                    Recuperation += mkh.RecuperationPower;
                    Core.Log($"Module's recuperation power = {mkh.RecuperationPower}");
                    MaxRecuperaction = Math.Max(MaxRecuperaction, mkh.recuperation);
                }
                Decay += mkh.DecayPower;

                // Processing factor multiplier
                if (mkh.Multiplier != 1)
                {
                    Core.Log($"Factor multiplier for {mkh.MultiplyFactor}: {mkh.Multiplier:P1}.");
                    FactorMultiplier factorMultiplier = GetFactorMultiplier(mkh.multiplyFactor);
                    if (mkh.crewCap > 0)
                        factorMultiplier.AddRestrictedMultiplier(mkh.Multiplier, mkh.crewCap, crewCount);
                    else factorMultiplier.AddFreeMultiplier(mkh.Multiplier);
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
            List<PartExposureComparer> exposures = new List<PartExposureComparer>();
            CrewCapacity = 0;
            foreach (Part p in parts)
            {
                ProcessPart(p, crewCount, false);
                Shielding += GetResourceShielding(p);
                if (p.CrewCapacity > 0)
                {
                    Core.Log($"Possible shelter part: {p.partName} with exposure {GetPartExtendedExposure(p):P1}.");
                    exposures.Add(new PartExposureComparer(p));
                    CrewCapacity += p.CrewCapacity;
                }
                exposures.Sort();
            }

            // Calculating shelter exposure
            double x = 0;
            int c = 0;
            for (int i = 0; i < exposures.Count; i++)
            {
                Core.Log($"Part {exposures[i].Part.partName} with exposure {exposures[i].Exposure:P1} and crew cap {exposures[i].Part.CrewCapacity}.");
                x += exposures[i].Exposure * Math.Min(exposures[i].Part.CrewCapacity, crewCount - c);
                c += exposures[i].Part.CrewCapacity;
                if (c >= crewCount)
                    break;
            }
            Core.Log($"Average exposure in top {exposures.Count} parts is {x / crewCount:P1}; general vessel exposure is {ExposureMultiplier:P1}.");
            ShelterExposure = Math.Min(x / c, VesselExposure);
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
            if (MaxHP != 1)
                res += $"\nMax HP: x{MaxHP}";
            if (MaxHPBonus != 0)
                res += $"\nMax HP bonus: {MaxHPBonus}";
            if (ExhaustedStart != 1)
                res += $"\nExhaustion start: x{ExhaustedStart}";
            if (ExhaustedEnd != 1)
                res += $"\nExhaustion end: x{ExhaustedEnd}";
            if (Space != 0)
                res += $"\nSpace: {Space:F1}";
            if (Recuperation != 0)
                res += $"\nRecuperation Power: {Recuperation:F1}% (max {MaxRecuperaction:F1}%)";
            if (Decay != 0)
                res += $"\nDecay: {Decay:F1}%";
            if (Shielding != 0)
                res += $"\nShielding: {Shielding:F1}";
            if (Radioactivity != 0)
                res += $"\nParts radioactivity: {Radioactivity:F0}";
            if (ExposureMultiplier != 1)
                res += $"\nExposure multiplier: {ExposureMultiplier:P1}";
            if (ShelterExposure != 1)
                res += $"\nShelter exposure: {ShelterExposure:P1}";
            if (CrewCapacity != 0)
                res += $"\nCrew capacity: {CrewCapacity}";
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
            hms.FactorMultipliers = new FactorMultiplierList(FactorMultipliers);
            return hms;
        }

        FactorMultiplier GetFactorMultiplier(string factorName) => FactorMultipliers[Core.GetHealthFactor(factorName)];

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
