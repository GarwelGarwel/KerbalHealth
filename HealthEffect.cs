﻿using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    /// <summary>
    /// Keeps modifiers introduced by vessel, parts, quirks or conditions
    /// </summary>
    public class HealthEffect : IConfigNode
    {
        public const string ConfigNodeName = "HEALTH_EFFECTS";

        public string Description { get; set; }

        public double HPChange { get; set; }

        public double MaxHP { get; set; } = 1;

        public double MaxHPBonus { get; set; } = 0;

        public double CriticalHealth { get; set; } = 1;

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

        public FactorMultiplierList FactorMultipliers { get; set; } = new FactorMultiplierList();

        public void Save(ConfigNode node)
        {
            void AddValue(double value, string name, double defaultValue)
            {
                if (value != defaultValue)
                    node.AddValue(name, value);
            }

            AddValue(HPChange, "hpChange", 0);
            AddValue(MaxHP, "maxHP", 1);
            AddValue(MaxHPBonus, "maxHPBonus", 0);
            AddValue(CriticalHealth, "criticalHealth", 1);
            AddValue(Space, "space", 0);
            AddValue(Recuperation, "recuperation", 0);
            AddValue(MaxRecuperaction, "maxRecuperation", 0);
            AddValue(Decay, "decay", 0);
            AddValue(Shielding, "shielding", 0);
            AddValue(Radioactivity, "radioactivity", 0);
            AddValue(ExposureMultiplier, "exposure", 1);
            AddValue(ShelterExposure, "shelterExposure", 1);
            AddValue(CrewCapacity, "crewCapacity", 0);
            for (int i = 0; i < FactorMultipliers.Count; i++)
                if (!FactorMultipliers[i].IsTrivial)
                {
                    ConfigNode n2 = new ConfigNode(FactorMultiplier.ConfigNodeName);
                    FactorMultipliers[i].Save(n2);
                    node.AddNode(n2);
                }
        }

        public void Load(ConfigNode node)
        {
            if (node == null)
                return;
            Description = node.GetString("description");
            HPChange = node.GetDouble("hpChange");
            MaxHP = node.GetDouble("maxHP", 1);
            MaxHPBonus = node.GetDouble("maxHPBonus");
            CriticalHealth = node.GetDouble("criticalHealth", 1);
            Space = node.GetDouble("space");
            Recuperation = node.GetDouble("recuperation");
            MaxRecuperaction = node.GetDouble("maxRecuperation");
            Decay = node.GetDouble("decay");
            Shielding = node.GetDouble("shielding");
            Radioactivity = node.GetDouble("radioactivity");
            ExposureMultiplier = node.GetDouble("exposure", 1);
            ShelterExposure = node.GetDouble("shelterExposure", 1);
            CrewCapacity = node.GetInt("crewCapacity");
            FactorMultipliers.Clear();
            FactorMultiplier fm = new FactorMultiplier(node);
            if (!fm.IsTrivial)
                FactorMultipliers.Add(fm);
            foreach (FactorMultiplier fm2 in node.GetNodes(FactorMultiplier.ConfigNodeName).Select(n => new FactorMultiplier(n)))
                FactorMultipliers.Add(fm2);
        }

        public HealthEffect()
        { }

        public HealthEffect(ConfigNode configNode) => Load(configNode);

        public HealthEffect(Vessel v, ConnectedLivingSpace.ICLSSpace clsSpace) : this() => ProcessParts(v?.Parts, v.GetCrewCount(), clsSpace);

        public HealthEffect(List<Part> parts, int crew, ConnectedLivingSpace.ICLSSpace clsSpace) : this() => ProcessParts(parts, crew, clsSpace);

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
            foreach (KeyValuePair<int, double> res in Core.ShieldingResources)
            {
                p.GetConnectedResourceTotals(res.Key, ResourceFlowMode.NO_FLOW, out double amount, out double maxAmount);
                if (amount != 0)
                {
                    Core.Log($"Part {p.name} contains {amount:N1} / {maxAmount:N0} of shielding resource {res.Key}.");
                    s += res.Value * amount;
                }
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
            for (int i = 0; i < parts.Count; i++)
                if (parts[i].CrewCapacity == 0 || parts[i] == part)
                {
                    modules.AddRange(parts[i].FindModulesImplementing<ModuleKerbalHealth>());
                    s += GetResourceShielding(parts[i]);
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
            if (effect.Description != null && effect.Description.Length > 0)
                if (Description != null && Description.Length > 0)
                    Description = $"{Description}\n{effect.Description}";
                else Description = effect.Description;
            HPChange += effect.HPChange;
            MaxHP *= effect.MaxHP;
            MaxHPBonus += effect.MaxHPBonus;
            CriticalHealth *= effect.CriticalHealth;
            Space += effect.Space;
            Recuperation += effect.Recuperation;
            MaxRecuperaction = Math.Max(MaxRecuperaction, effect.MaxRecuperaction);
            Decay += effect.Decay;
            Shielding += effect.Shielding;
            Radioactivity += effect.Radioactivity;
            ExposureMultiplier *= effect.ExposureMultiplier;
            ShelterExposure = Math.Min(ShelterExposure, effect.ShelterExposure);
            CrewCapacity += effect.CrewCapacity;

            FactorMultipliers.CombineWith(effect.FactorMultipliers);
            return this;
        }

        /// <summary>
        /// Checks a part for its effects on the kerbal
        /// </summary>
        /// <param name="part">Part to process</param>
        /// <param name="crewCount">Current crew</param>
        /// <param name="partCrewModules">Whether to analyze modules with partCrewOnly flag or without</param>
        /// <param name="inCLSSpace">Whether the part is located in the same CLS space as the current kerbal (true if CLS integration is not active)</param>
        public void ProcessPart(Part part, int crewCount, bool inCLSSpace)
        {
            if (part == null)
            {
                Core.Log("HealthEffect: 'part' is null. Unless the kerbal is on EVA, this is probably an error.", LogLevel.Important);
                return;
            }

            foreach (ModuleKerbalHealth mkh in part.FindModulesImplementing<ModuleKerbalHealth>().Where(m => m.IsModuleActive))
            {
                Core.Log($"Processing {mkh.Title} Module in {part.name}.");
                if (inCLSSpace || mkh.affectsAllCLSSpaces)
                {
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
                        Core.Log($"Factor multiplier for {mkh.multiplyFactor}: {mkh.Multiplier:P1} (crew cap {mkh.crewCap}).");
                        if (mkh.crewCap > 0)
                            GetFactorMultiplier(mkh.multiplyFactor).AddMultiplier(mkh.Multiplier, mkh.crewCap, crewCount);
                        else GetFactorMultiplier(mkh.multiplyFactor).AddMultiplier(mkh.Multiplier);
                    }
                }
                else Core.Log("The module is not in the kerbal's CLS space.");

                Shielding += mkh.shielding;
                if (mkh.shielding != 0)
                    Core.Log($"Shielding of this module is {mkh.shielding}.");
                float radioactivity = mkh.Radioactivity;
                Radioactivity += radioactivity;
                if (radioactivity != 0)
                    Core.Log($"Radioactive emission of this module is {radioactivity}.");
            }

            Shielding += GetResourceShielding(part);
        }

        /// <summary>
        /// Processes several parts and also records their RadiationShielding values
        /// </summary>
        /// <param name="parts"></param>
        public void ProcessParts(IList<Part> parts, int crewCount, ConnectedLivingSpace.ICLSSpace clsSpace)
        {
            Core.Log($"Processing {parts.Count()} parts for {crewCount} crew in {(clsSpace != null ? clsSpace.Name : "a vessel")}...");
            List<PartExposureComparer> exposures = new List<PartExposureComparer>();
            CrewCapacity = 0;

            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                ProcessPart(p, crewCount, clsSpace == null || clsSpace.Parts.Any(clsPart => clsPart.Part == p));
                if (p.CrewCapacity > 0)
                {
                    if (Core.IsLogging())
                        Core.Log($"Possible shelter part: {p.partName} with exposure {GetPartExtendedExposure(p):P1}.");
                    exposures.Add(new PartExposureComparer(p));
                    CrewCapacity += p.CrewCapacity;
                }
                exposures.Sort();
            }

            if (KerbalHealthRadiationSettings.Instance.RadiationEnabled && !(Kerbalism.Found && KerbalHealthRadiationSettings.Instance.UseKerbalismRadiation))
            {
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
                Core.Log($"Average exposure in top {exposures.Count} parts is {x / crewCount:P1}; general vessel exposure is {ExposureMultiplier:P1}.");
                ShelterExposure = Math.Min(x / c, VesselExposure);
            }
            else ShelterExposure = VesselExposure;
        }

        /// <summary>
        /// Returns a text description of all significant values
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            if (Description != null) 
                res.AppendLine(Localizer.Format(Description));
            if (HPChange != 0)
                res.AppendLine(Localizer.Format("#KH_Effect_HPChange", HPChange.ToString("F2")));
            if (MaxHP != 1)
                res.AppendLine(Localizer.Format("#KH_Effect_MaxHP", MaxHP.ToString("P0")));
            if (MaxHPBonus != 0)
                res.AppendLine(Localizer.Format("#KH_Effect_MaxHPBonus", MaxHPBonus));
            if (CriticalHealth != 1)
                res.AppendLine(Localizer.Format("#KH_Effect_CriticalHealth", CriticalHealth.ToString("P0")));
            if (Space != 0)
                res.AppendLine(Localizer.Format("#KH_Effect_Space", Space.ToString("F1")));
            if (Recuperation != 0)
                res.AppendLine(Localizer.Format("#KH_Effect_Recuperation", Recuperation.ToString("F1"), MaxRecuperaction.ToString("F1")));
            if (Decay != 0)
                res.AppendLine(Localizer.Format("#KH_Effect_Decay", Decay.ToString("F1")));
            if (Shielding != 0)
                res.AppendLine(Localizer.Format("#KH_Effect_Shielding", Shielding.ToString("F1")));
            if (Radioactivity != 0)
                res.AppendLine(Localizer.Format("#KH_Effect_Radioactivity", Radioactivity.ToString("F0")));
            if (ExposureMultiplier != 1)
                res.AppendLine(Localizer.Format("#KH_Effect_ExposureMultiplier", ExposureMultiplier.ToString("P1")));
            if (ShelterExposure != 1)
                res.AppendLine(Localizer.Format("#KH_Effect_ShelterExposure", ShelterExposure.ToString("P1")));
            if (CrewCapacity != 0)
                res.AppendLine(Localizer.Format("#KH_Effect_CrewCapacity", CrewCapacity));
            res.AppendLine(FactorMultipliers.ToString());
            return res.ToStringAndRelease().Trim();
        }

        /// <summary>
        /// Returns a deep copy of the instance
        /// </summary>
        /// <returns></returns>
        public HealthEffect Clone()
        {
            HealthEffect hms = (HealthEffect)MemberwiseClone();
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
