using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbalHealth
{
    public class KerbalHealthStatus
    {
        public enum HealthCondition { OK, Exhausted }  // conditions

        string name;
        double hp;
        double lastChange;  // Cached HP change per day (for unloaded vessels)
        double lastMarginalPositiveChange, lastMarginalNegativeChange;  // Cached marginal HP change (in %)
        HealthCondition condition = HealthCondition.OK;
        string trait = null;
        bool onEva = false;  // True if kerbal is on EVA

        // These dictionaries are used to calculate factor modifiers
        Dictionary<string, double> fmBonusSums = new Dictionary<string, double>(), fmFreeMultipliers = new Dictionary<string, double>();
        double minMultiplier, maxMultiplier;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                pcmCached = null;
            }
        }

        public double HP
        {
            get { return hp; }
            set
            {
                //Core.Log("KerbalHealthStatus.HP.set");
                if (value < Core.MinHP) hp = Core.MinHP;
                else if (value > MaxHP) hp = MaxHP;
                else hp = value;
            }
        }

        public double Health { get { return (HP - Core.MinHP) / (MaxHP - Core.MinHP); } }  // % of health relative to MaxHealth

        public double LastChange
        {
            get { return lastChange; }
            set { lastChange = value; }
        }

        public double LastMarginalPositiveChange
        {
            get { return lastMarginalPositiveChange; }
            set { lastMarginalPositiveChange = value; }
        }

        public double LastMarginalNegativeChange
        {
            get { return lastMarginalNegativeChange; }
            set { lastMarginalNegativeChange = value; }
        }

        public HealthCondition Condition
        {
            get { return condition; }
            set
            {
                //Core.Log("KerbalHealthStatus.Condition.set");
                if (value == condition) return;
                switch (value)
                {
                    case HealthCondition.OK:
                        Core.Log("Reviving " + Name + " as " + Trait + "...", Core.LogLevels.Important);
                        PCM.type = ProtoCrewMember.KerbalType.Crew;
                        PCM.trait = Trait;
                        break;
                    case HealthCondition.Exhausted:
                        Core.Log(Name + " (" + Trait + ") is exhausted.", Core.LogLevels.Important);
                        Trait = PCM.trait;
                        PCM.type = ProtoCrewMember.KerbalType.Tourist;
                        break;
                }
                condition = value;
            }
        }

        string Trait
        {
            get { return trait ?? PCM.trait; }
            set { trait = value; }
        }

        public bool IsOnEVA
        {
            get { return onEva; }
            set { onEva = value; }
        }

        ProtoCrewMember pcmCached;
        public ProtoCrewMember PCM
        {
            get
            {
                //Core.Log("KerbalHealthStatus.HP.set");
                if (pcmCached != null) return pcmCached;
                foreach (ProtoCrewMember pcm in HighLogic.fetch.currentGame.CrewRoster.Crew)
                    if (pcm.name == Name)
                    {
                        pcmCached = pcm;
                        return pcm;
                    }
                foreach (ProtoCrewMember pcm in HighLogic.fetch.currentGame.CrewRoster.Tourist)
                    if (pcm.name == Name)
                    {
                        pcmCached = pcm;
                        return pcm;
                    }
                return null;
            }
            set
            {
                Name = value.name;
                pcmCached = value;
            }
        }

        public static double GetMaxHP(ProtoCrewMember pcm)
        {
            return Core.BaseMaxHP + Core.HPPerLevel * pcm.experienceLevel;
        }

        public double MaxHP
        {
            get { return GetMaxHP(PCM); }
        }

        public double TimeToValue(double target)
        {
            double change = HealthChangePerDay();
            if (change == 0) return double.NaN;
            double res = (target - HP) / change;
            if (res < 0) return double.NaN;
            return res * 21600;
        }

        public double NextConditionHP()
        {
            if (HealthChangePerDay() > 0)
            {
                switch (Condition)
                {
                    case HealthCondition.OK:
                        return MaxHP;
                    case HealthCondition.Exhausted:
                        return Core.ExhaustionEndHealth * MaxHP;
                }
            }
            switch (Condition)
            {
                case HealthCondition.OK:
                    return Core.ExhaustionStartHealth * MaxHP;
                case HealthCondition.Exhausted:
                    return Core.DeathHealth * MaxHP;
            }
            return double.NaN;
        }

        public double TimeToNextCondition()
        { return TimeToValue(NextConditionHP()); }

        // Returns HP level when marginal HP change balances out "fixed" change. If <= 0, no such level
        public double GetBalanceHP()
        {
            Core.Log(Name + "'s last change: " + LastChange + ", MPC: " + LastMarginalPositiveChange + "%, MNC: " + LastMarginalNegativeChange + "%.");
            if (LastChange == 0) HealthChangePerDay();
            if (LastMarginalPositiveChange <= LastMarginalNegativeChange) return 0;
            return (MaxHP * LastMarginalPositiveChange + LastChange * 100) / (LastMarginalPositiveChange - LastMarginalNegativeChange);
        }

        double MarginalChange
        { get { return (MaxHP - HP) * (LastMarginalPositiveChange / 100) - (HP - Core.MinHP) * (LastMarginalNegativeChange / 100); } }

        bool IsInCrew(ProtoCrewMember[] crew)
        {
            foreach (ProtoCrewMember pcm in crew) if (pcm?.name == Name) return true;
            return false;
        }

        void ProcessPart(Part part, ProtoCrewMember[] crew, ref double change)
        {
            int i = 0;
            foreach (ModuleKerbalHealth mkh in part.FindModulesImplementing<ModuleKerbalHealth>())
            {
                Core.Log("Processing MKH #" + (++i) + "/" + part.FindModulesImplementing<ModuleKerbalHealth>().Count + " of " + part.name + "...\nCrew has " + crew.Length + " members.");
                if (mkh.IsModuleActive && (!mkh.partCrewOnly || IsInCrew(crew)))
                {
                    change += mkh.hpChangePerDay;
                    if (mkh.hpMarginalChangePerDay > 0) LastMarginalPositiveChange += mkh.hpMarginalChangePerDay;
                    else if (mkh.hpMarginalChangePerDay < 0) LastMarginalNegativeChange -= mkh.hpMarginalChangePerDay;
                    // Processing factor multiplier
                    if (mkh.multiplier != 1)
                    {
                        if (mkh.crewCap > 0) fmBonusSums[mkh.multiplyFactor] += (1 - mkh.multiplier) * Math.Min(mkh.crewCap, mkh.AffectedCrewCount);
                        else fmFreeMultipliers[mkh.multiplyFactor] *= mkh.multiplier;
                        if (mkh.multiplier > 1) maxMultiplier = Math.Max(maxMultiplier, mkh.multiplier);
                        else minMultiplier = Math.Min(minMultiplier, mkh.multiplier);
                    }
                }
                else Core.Log("This module doesn't affect " + Name + "(active: " + mkh.IsModuleActive + "; part crew only: " + mkh.partCrewOnly + "; in part's crew: " + IsInCrew(crew) + ")");
            }
        }

        double Multiplier(string factorId)
        {
            double res = 1 - fmBonusSums[factorId] / Core.GetCrewCount(PCM);
            if (res < 1) res = Math.Max(res, minMultiplier); else res = Math.Min(res, maxMultiplier);
            Core.Log("Multiplier for " + factorId + " for " + Name + " is " + res + " (bonus sum: " + fmBonusSums[factorId] + "; free multiplier: " + fmFreeMultipliers[factorId] + "; multipliers: " + minMultiplier + ".." + maxMultiplier + ")");
            return res * fmFreeMultipliers[factorId];
        }

        public double HealthChangePerDay()
        {
            double change = 0;
            ProtoCrewMember pcm = PCM;
            if (pcm == null)
            {
                Core.Log(Name + " not found in Core.KerbalHealthList!");
                return 0;
            }

            if (Core.IsKerbalLoaded(pcm) && IsOnEVA)
            {
                Core.Log(Name + " is back from EVA.", Core.LogLevels.Important);
                IsOnEVA = false;
            }

            //if (IsKerbalLoaded(pcm) || IsOnEVA || Core.IsInEditor) Tooltip = "";
            LastMarginalPositiveChange = LastMarginalNegativeChange = 0;
            fmBonusSums.Clear();
            fmBonusSums.Add("All", 0);
            fmFreeMultipliers.Clear();
            fmFreeMultipliers.Add("All", 1);
            foreach (HealthFactor f in Core.Factors)
            {
                fmBonusSums.Add(f.Id, 0);
                fmFreeMultipliers.Add(f.Id, 1);
            }
            minMultiplier = maxMultiplier = 1;

            // Processing parts
            if ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned && Core.IsKerbalLoaded(pcm)) || Core.IsInEditor)
                    if (Core.IsInEditor)
                        foreach (PartCrewManifest p in ShipConstruction.ShipManifest.PartManifests) ProcessPart(p.PartInfo.partPrefab, p.GetPartCrew(), ref change);
                    else
                        foreach (Part p in pcm.seat.vessel.Parts) ProcessPart(p, p.protoModuleCrew.ToArray(), ref change);

            if (Core.IsKerbalLoaded(pcm) || IsOnEVA) LastChange = 0;

            // Processing all factors
            Core.Log("Processing all the " + Core.Factors.Count + " factors for " + pcm.name + "...");
            foreach (HealthFactor f in Core.Factors)
            {
                if (f.LoadedOnly && !(Core.IsInEditor || Core.IsKerbalLoaded(pcm) || IsOnEVA))
                {
                    Core.Log(f.Id + " does not affect " + pcm.name + " (" + (Core.IsInEditor ? "" : "not ") + "in editor, " + (Core.IsKerbalLoaded(pcm) ? "" : "not ") + "in vessel, " + (IsOnEVA ? "" : "not ") + "on EVA).");
                    continue;
                }
                double m = Multiplier(f.Id) * Multiplier("All");
                double c = f.ChangePerDay(pcm) * m;
                change += c;
                if (Core.IsKerbalLoaded(pcm) || IsOnEVA || Core.IsInEditor)
                {
                    LastChange += c;
                    //Tooltip += "\n" + f.Name + ": " + (c > 0 ? "+" : "") + c.ToString("F1") + (m != 1 ? (" (@" + m.ToString("F2") + "x)") : "");
                }
                Core.Log(f.Id + "'s effect on " + pcm.name + " is " + c + " HP/day.");
            }
            if (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned && !Core.IsKerbalLoaded(pcm)) change = LastChange;
            double mc = MarginalChange;
            //if (IsKerbalLoaded(pcm) || IsOnEVA || Core.IsInEditor)
            //    Tooltip += "\nMarginal Change: " + (mc > 0 ? "+" : "") + mc.ToString("F1") + " (+" + LastMarginalPositiveChange + "%, -" + LastMarginalNegativeChange + "%)";
            Core.Log("Marginal change: " + mc + "(+" + LastMarginalPositiveChange + "%, -" + LastMarginalNegativeChange + "%).");
            return change + mc;
        }

        public void Update(double interval)
        {
            Core.Log("Updating " + Name + "'s health.");
            HP += HealthChangePerDay() / 21600 * interval;
            if (HP <= Core.DeathHealth * MaxHP)
            {
                Core.Log(Name + " dies due to having " + HP + " health.", Core.LogLevels.Important);
                if (PCM.seat != null) PCM.seat.part.RemoveCrewmember(PCM);
                PCM.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                ScreenMessages.PostScreenMessage(Name + " dies of poor health!");
            }
            if (Condition == HealthCondition.OK && HP <= Core.ExhaustionStartHealth * MaxHP)
            {
                Condition = HealthCondition.Exhausted;
                ScreenMessages.PostScreenMessage(Name + " is exhausted!");
            }
            if (Condition == HealthCondition.Exhausted && HP >= Core.ExhaustionEndHealth * MaxHP)
            {
                Condition = HealthCondition.OK;
                ScreenMessages.PostScreenMessage(Name + " has revived.");
            }
        }

        public ConfigNode ConfigNode
        {
            get
            {
                ConfigNode n = new ConfigNode("KerbalHealthStatus");
                n.AddValue("name", Name);
                n.AddValue("health", HP);
                n.AddValue("condition", Condition);
                if (Condition == HealthCondition.Exhausted) n.AddValue("trait", Trait);
                if (LastChange != 0) n.AddValue("lastChange", LastChange);
                if (LastMarginalPositiveChange != 0) n.AddValue("lastMarginalPositiveChange", LastMarginalPositiveChange);
                if (LastMarginalNegativeChange != 0) n.AddValue("lastMarginalNegativeChange", LastMarginalNegativeChange);
                if (IsOnEVA) n.AddValue("onEva", true);
                return n;
            }
            set
            {
                Name = value.GetValue("name");
                HP = Double.Parse(value.GetValue("health"));
                Condition = (KerbalHealthStatus.HealthCondition)Enum.Parse(typeof(HealthCondition), value.GetValue("condition"));
                if (Condition == HealthCondition.Exhausted) Trait = value.GetValue("trait");
                try { LastChange = double.Parse(value.GetValue("lastChange")); }
                catch (Exception) { LastChange = 0; }
                try { LastMarginalPositiveChange = double.Parse(value.GetValue("lastMarginalPositiveChange")); }
                catch (Exception) { LastMarginalPositiveChange = 0; }
                try { LastMarginalNegativeChange = double.Parse(value.GetValue("lastMarginalNegativeChange")); }
                catch (Exception) { LastMarginalNegativeChange = 0; }
                try { IsOnEVA = bool.Parse(value.GetValue("onEva")); }
                catch (Exception) { IsOnEVA = false; }
            }
        }

        public override bool Equals(object obj)
        {
            return ((KerbalHealthStatus)obj).Name.Equals(Name);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public KerbalHealthStatus() { }

        public KerbalHealthStatus(string name)
        {
            Name = name;
            HP = MaxHP;
        }

        public KerbalHealthStatus(string name, double health)
        {
            Name = name;
            HP = health;
        }

        public KerbalHealthStatus(ConfigNode node)
        {
            ConfigNode = node;
        }
    }
}
