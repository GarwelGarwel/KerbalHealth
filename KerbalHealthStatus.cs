using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbalHealth
{
    /// <summary>
    /// Contains data about a kerbal's health
    /// </summary>
    public class KerbalHealthStatus
    {
        string name;
        double maxHPModifier = 0;  // How many HP are added (or subtracted) to maximum HP
        double hp;
        double dose = 0, radiation = 0, exposure = 1;
        double cachedChange = 0, lastChange = 0;  // Cached HP change per day (for unloaded vessels), last ordinary (non-marginal) change (used for statistics/monitoring)
        double lastMarginalPositiveChange = 0, lastMarginalNegativeChange = 0;  // Cached marginal HP change (in %)
        List<HealthCondition> conditions = new List<HealthCondition>();
        string trait = null;
        bool onEva = false;  // True if kerbal is on EVA
        bool warned = true;  // True if a warning has already been displayed for this kerbal

        // These dictionaries are used to calculate factor modifiers from part modules
        Dictionary<string, double> fmBonusSums = new Dictionary<string, double>(), fmFreeMultipliers = new Dictionary<string, double>(), minMultipliers = new Dictionary<string, double>(), maxMultipliers = new Dictionary<string, double>();

        /// <summary>
        /// Kerbal's name
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                pcmCached = null;
            }
        }

        /// <summary>
        /// Kerbal's health points
        /// </summary>
        public double HP
        {
            get { return hp; }
            set
            {
                if (value < 0) hp = 0;
                else if (value > MaxHP) hp = MaxHP;
                else hp = value;
                if (!warned && Health < Core.LowHealthAlert)
                {
                    Core.ShowMessage(Name + "'s health is dangerously low!", true);
                    warned = true;
                }
                else if (warned && Health >= Core.LowHealthAlert) warned = false;
            }
        }

        /// <summary>
        /// Returns kerbal's HP relative to MaxHealth (0 to 1)
        /// </summary>
        public double Health { get { return HP / MaxHP; } }

        /// <summary>
        /// Health points added to (or subtracted from) kerbal's max HP
        /// </summary>
        public double MaxHPModifier
        {
            get { return maxHPModifier; }
            set { maxHPModifier = value; }
        }

        /// <summary>
        /// Lifetime absorbed dose of ionizing radiation, in banana equivalent doses (BEDs, 1 BED = 1e-7 Sv)
        /// </summary>
        public double Dose
        {
            get { return dose; }
            set { dose = value; }
        }

        /// <summary>
        /// Returns the fraction of max HP that the kerbal has considering radiation effects. 1e7 of RadiationDose = -25% of MaxHP
        /// </summary>
        public double RadiationMaxHPModifier
        { get { return Core.RadiationEnabled ? 1 - Dose * 2.5e-8 * Core.RadiationEffect : 1; } }

        /// <summary>
        /// Level of background radiation absorbed by the body, in bananas per day
        /// </summary>
        public double Radiation
        {
            get { return radiation; }
            set { radiation = value; }
        }

        public double Exposure
        {
            get { return exposure; }
            set { exposure = value; }
        }

        static double kscRadiation = 0.0005;  // How much cosmic radiation reaches KSC
        static double landedRadiationQ = 0.05;  // How much cosmic radiation reaches planetary surface (not including atmosphere effect)
        static double atmoRadiationQ = 0.01;  // How much cosmic radiation atmosphere lets through, multiplies with planetLandedRadiationQ
        static double flyingRadiationQ = 0.003;
        static double inSpaceLowRadiationQ = 0.1;
        static double inSpaceHighRadiationQ = 0.5;
        static double evaExposure = 10;

        static double solarRadiation = 2000;  // Sun radiation level at the home planet's orbit
        static double galacticRadiation = 2000;  // Galactic cosmic rays level

        static double GetSolarRadiationAtDistance(double distance)
        { return solarRadiation * Core.Sqr(FlightGlobals.GetHomeBody().orbit.radius / distance); }

        static bool IsPlanet(CelestialBody body)
        { return body?.orbit?.referenceBody == Sun.Instance.sun; }

        static CelestialBody GetPlanet(CelestialBody body)
        { return ((body == null) || IsPlanet(body)) ? body : GetPlanet(body?.orbit?.referenceBody); }

        public double GetCurrentRadiation()
        {
            double cosmicRadiationQ = 1, distanceToSun = 0;
            if (PCM.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
            {
                cosmicRadiationQ = kscRadiation;
                distanceToSun = FlightGlobals.GetHomeBody().orbit.radius;
            }
            else
            {
                Vessel v = Core.KerbalVessel(PCM);
                Core.Log(Name + " is in " + v.vesselName + " in " + v.mainBody.bodyName + "'s SOI at an altitude of " + v.altitude + ", situation: " + v.SituationString + ", distance to Sun: " + v.distanceToSun);
                if (v.mainBody != Sun.Instance.sun)
                {
                    distanceToSun = (v.distanceToSun > 0) ? v.distanceToSun : GetPlanet(v.mainBody).orbit.radius;
                    if (IsPlanet(v.mainBody))
                        switch (v.situation)
                        {
                            case Vessel.Situations.PRELAUNCH:
                            case Vessel.Situations.LANDED:
                            case Vessel.Situations.SPLASHED:
                                cosmicRadiationQ *= landedRadiationQ;
                                break;
                            case Vessel.Situations.FLYING:
                                cosmicRadiationQ *= flyingRadiationQ;
                                break;
                            default:
                                if (v.altitude < v.mainBody.scienceValues.spaceAltitudeThreshold) cosmicRadiationQ *= inSpaceLowRadiationQ;
                                else cosmicRadiationQ *= inSpaceHighRadiationQ;
                                break;
                        }
                    else cosmicRadiationQ *= inSpaceHighRadiationQ;
                    if (v.mainBody.atmosphere && ((v.situation == Vessel.Situations.PRELAUNCH) || (v.situation == Vessel.Situations.LANDED) || (v.situation == Vessel.Situations.SPLASHED)))
                        cosmicRadiationQ *= atmoRadiationQ;
                }
                else distanceToSun = v.altitude + Sun.Instance.sun.Radius;
            }
            Core.Log("Solar Radiation Quoficient = " + cosmicRadiationQ);
            Core.Log("Distance to Sun = " + distanceToSun + " (" + (distanceToSun / FlightGlobals.GetHomeBody().orbit.radius) + " AU)");
            Core.Log("Nominal Solar Radiation @ Vessel's Location = " + GetSolarRadiationAtDistance(distanceToSun));
            Core.Log("Nominal Galactic Radiation = " + galacticRadiation);
            Exposure = IsOnEVA ? evaExposure : 1;
            return Exposure * cosmicRadiationQ * (GetSolarRadiationAtDistance(distanceToSun) + galacticRadiation);
        }

        double CachedChange
        {
            get { return cachedChange; }
            set { cachedChange = value; }
        }

        /// <summary>
        /// HP change per day rate in the latest update. Only includes factors, not marginal change
        /// </summary>
        public double LastChange
        {
            get { return lastChange; }
            set { lastChange = value; }
        }

        /// <summary>
        /// Marginal change in the latest update (positive only)
        /// </summary>
        public double LastMarginalPositiveChange
        {
            get { return lastMarginalPositiveChange; }
            set { lastMarginalPositiveChange = value; }
        }

        /// <summary>
        /// Marginal change in the latest update (negative only)
        /// </summary>
        public double LastMarginalNegativeChange
        {
            get { return lastMarginalNegativeChange; }
            set { lastMarginalNegativeChange = value; }
        }

        /// <summary>
        /// HP change due to marginal effects
        /// </summary>
        public double MarginalChange
        { get { return (MaxHP - HP) * (LastMarginalPositiveChange / 100) - HP * (LastMarginalNegativeChange / 100); } }

        /// <summary>
        /// Total HP change per day rate in the latest update
        /// </summary>
        public double LastChangeTotal
        { get { return LastChange + MarginalChange; } }

        /// <summary>
        /// Returns a list of all active health conditions for the kerbal
        /// </summary>
        public List<HealthCondition> Conditions
        {
            get { return conditions; }
            set { conditions = value; }
        }

        /// <summary>
        /// Returns the condition with a given name, if present (null otherwise)
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public HealthCondition GetCondition(string condition)
        {
            foreach (HealthCondition hc in Conditions)
                if (hc.Name == condition) return hc;
            return null;
        }

        /// <summary>
        /// Returns true if a given condition exists for the kerbal
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public bool HasCondition(string condition)
        { return GetCondition(condition) != null; }

        /// <summary>
        /// Adds a new health condition
        /// </summary>
        /// <param name="condition">Condition to add</param>
        /// <param name="additive">If true, the condition will be added even if it already exists (false by default)</param>
        public void AddCondition(HealthCondition condition, bool additive = false)
        {
            Core.Log("Adding " + condition.Name + " condition to " + Name + "...");
            if (!additive && HasCondition(condition.Name)) return;
            Conditions.Add(condition);
            switch (condition.Name)
            {
                case "OK":
                    Core.Log("Reviving " + Name + " as " + Trait + "...", Core.LogLevel.Important);
                    if (PCM.type != ProtoCrewMember.KerbalType.Tourist) return;  // Apparently, the kerbal has been revived by another mod
                    PCM.type = ProtoCrewMember.KerbalType.Crew;
                    PCM.trait = Trait;
                    break;
                case "Exhausted":
                    Core.Log(Name + " (" + Trait + ") is exhausted.", Core.LogLevel.Important);
                    Trait = PCM.trait;
                    PCM.type = ProtoCrewMember.KerbalType.Tourist;
                    break;
            }
            Core.Log(condition.Name + " condition added to " + Name + ".", Core.LogLevel.Important);
        }

        /// <summary>
        /// Removes a condition with from the kerbal
        /// </summary>
        /// <param name="condition">Name of condition to remove</param>
        /// <param name="removeAll">If true, all conditions with the same name will be removed. Makes sense for additive conditions. Default is false</param>
        public void RemoveCondition(string condition, bool removeAll = false)
        {
            bool found = false;
            Core.Log("Removing " + condition + " condition from " + Name + "...");
            foreach (HealthCondition hc in Conditions)
                if (hc.Name == condition)
                {
                    found = true;
                    Conditions.Remove(hc);
                    if (!removeAll) break;
                }
            if (found)
            {
                Core.Log(condition + " condition removed from " + Name + ".", Core.LogLevel.Important);
                switch (condition)
                {
                    case "Exhausted":
                        if (PCM.type != ProtoCrewMember.KerbalType.Tourist) return;  // Apparently, the kerbal has been revived by another mod
                        PCM.type = ProtoCrewMember.KerbalType.Crew;
                        PCM.trait = Trait;
                        break;
                }
            }
        }

        /// <summary>
        /// Returns a comma-separated list of visible conditions or "OK" if there are no visible conditions
        /// </summary>
        public string ConditionString
        {
            get
            {
                string res = "";
                foreach (HealthCondition hc in Conditions)
                    if (hc.IsVisible)
                    {
                        if (res != "") res += ", ";
                        res += hc.Title;
                    }
                if (res == "") res = "OK";
                return res;
            }
        }

        /// <summary>
        /// Returns saved kerbal's trait or current trait if nothing is saved
        /// </summary>
        string Trait
        {
            get { return trait ?? PCM.trait; }
            set { trait = value; }
        }

        /// <summary>
        /// Returns true if the kerbal is marked as being on EVA
        /// </summary>
        public bool IsOnEVA
        {
            get { return onEva; }
            set { onEva = value; }
        }

        /// <summary>
        /// Returns true if a low health alarm has been shown for the kerbal
        /// </summary>
        public bool IsWarned
        {
            get { return warned; }
            set { warned = value; }
        }

        ProtoCrewMember pcmCached;
        /// <summary>
        /// Returns ProtoCrewMember for the kerbal
        /// </summary>
        public ProtoCrewMember PCM
        {
            get
            {
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

        /// <summary>
        /// Returns the max number of HP for the kerbal (not including the modifier)
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static double GetMaxHP(ProtoCrewMember pcm)
        { return Core.BaseMaxHP + Core.HPPerLevel * pcm.experienceLevel; }

        /// <summary>
        /// Returns the max number of HP for the kerbal (including the modifier)
        /// </summary>
        public double MaxHP
        { get { return (GetMaxHP(PCM) + MaxHPModifier) * RadiationMaxHPModifier; } }

        /// <summary>
        /// How many seconds left until HP reaches the given level, at the current HP change rate
        /// </summary>
        /// <param name="target">Target HP level</param>
        /// <returns></returns>
        public double TimeToValue(double target)
        {
            if (LastChangeTotal == 0) return double.NaN;
            double res = (target - HP) / LastChangeTotal;
            if (res < 0) return double.NaN;
            return res * KSPUtil.dateTimeFormatter.Day;
        }

        /// <summary>
        /// Returns HP number for the next condition (OK, Exhausted or death)
        /// </summary>
        /// <returns></returns>
        public double NextConditionHP()
        {
            if (LastChangeTotal > 0)
                if (HasCondition("Exhausted"))
                    return Core.ExhaustionEndHealth * MaxHP;
                else return MaxHP;
            if (LastChangeTotal < 0)
                if (HasCondition("Exhausted")) return 0;
                else return Core.ExhaustionStartHealth * MaxHP;
            return double.NaN;
        }

        /// <summary>
        /// Returns number of seconds until the next condition is reached
        /// </summary>
        /// <returns></returns>
        public double TimeToNextCondition()
        { return TimeToValue(NextConditionHP()); }

        /// <summary>
        /// Returns HP level when marginal HP change balances out "fixed" change. If <= 0, no such level
        /// </summary>
        /// <returns></returns>
        public double GetBalanceHP()
        {
            Core.Log(Name + "'s last change: " + LastChange + ", MPC: " + LastMarginalPositiveChange + "%, MNC: " + LastMarginalNegativeChange + "%.");
            if (LastChange == 0) HealthChangePerDay();
            if (LastMarginalPositiveChange <= LastMarginalNegativeChange) return 0;
            return (MaxHP * LastMarginalPositiveChange + LastChange * 100) / (LastMarginalPositiveChange - LastMarginalNegativeChange);
        }

        /// <summary>
        /// Returns true if the kerbal is member of an array of ProtoCrewMembers
        /// </summary>
        /// <param name="crew"></param>
        /// <returns></returns>
        bool IsInCrew(ProtoCrewMember[] crew)
        {
            foreach (ProtoCrewMember pcm in crew) if (pcm?.name == Name) return true;
            return false;
        }

        /// <summary>
        /// Checks a part for its effects on the kerbal
        /// </summary>
        /// <param name="part"></param>
        /// <param name="crew"></param>
        /// <param name="change"></param>
        void ProcessPart(Part part, ProtoCrewMember[] crew, ref double change)
        {
            int i = 0;
            foreach (ModuleKerbalHealth mkh in part.FindModulesImplementing<ModuleKerbalHealth>())
            {
                Core.Log("Processing MKH #" + (++i) + "/" + part.FindModulesImplementing<ModuleKerbalHealth>().Count + " of " + part.name + "...\nCrew has " + crew.Length + " members.");
                if (mkh.IsModuleActive && (!mkh.partCrewOnly || IsInCrew(crew)))
                {
                    Core.Log("Module is active. mkh.isActive = " + mkh.isActive + ". mkh.alwaysActive = " + mkh.alwaysActive);
                    change += mkh.hpChangePerDay;
                    if (mkh.hpMarginalChangePerDay > 0) LastMarginalPositiveChange += mkh.hpMarginalChangePerDay;
                    else if (mkh.hpMarginalChangePerDay < 0) LastMarginalNegativeChange -= mkh.hpMarginalChangePerDay;
                    // Processing factor multiplier
                    if ((mkh.multiplier != 1) && (mkh.MultiplyFactor != null))
                    {
                        if (mkh.crewCap > 0) fmBonusSums[mkh.multiplyFactor] += (1 - mkh.multiplier) * Math.Min(mkh.crewCap, mkh.AffectedCrewCount);
                        else fmFreeMultipliers[mkh.MultiplyFactor.Name] *= mkh.multiplier;
                        if (mkh.multiplier > 1) maxMultipliers[mkh.MultiplyFactor.Name] = Math.Max(maxMultipliers[mkh.MultiplyFactor.Name], mkh.multiplier);
                        else minMultipliers[mkh.MultiplyFactor.Name] = Math.Min(minMultipliers[mkh.MultiplyFactor.Name], mkh.multiplier);
                    }
                    Core.Log("HP change after this module: " + change + "." + (mkh.MultiplyFactor != null ? " Bonus to " + mkh.MultiplyFactor.Name + ": " + fmBonusSums[mkh.MultiplyFactor.Name] + ". Free multiplier: " + fmFreeMultipliers[mkh.MultiplyFactor.Name] + "." : ""));
                }
                else Core.Log("This module doesn't affect " + Name + "(active: " + mkh.IsModuleActive + "; part crew only: " + mkh.partCrewOnly + "; in part's crew: " + IsInCrew(crew) + ")");
            }
        }

        double Multiplier(string factorId)
        {
            double res = 1 - fmBonusSums[factorId] / Core.GetCrewCount(PCM);
            if (res < 1) res = Math.Max(res, minMultipliers[factorId]); else res = Math.Min(res, maxMultipliers[factorId]);
            Core.Log("Multiplier for " + factorId + " for " + Name + " is " + res + " (bonus sum: " + fmBonusSums[factorId] + "; free multiplier: " + fmFreeMultipliers[factorId] + "; multipliers: " + minMultipliers[factorId] + ".." + maxMultipliers[factorId] + ")");
            return res * fmFreeMultipliers[factorId];
        }

        /// <summary>
        /// Returns effective HP change rate per day
        /// </summary>
        /// <returns></returns>
        public double HealthChangePerDay()
        {
            double change = 0;
            ProtoCrewMember pcm = PCM;
            if (pcm == null)
            {
                Core.Log(Name + " not found in Core.KerbalHealthList!");
                return 0;
            }

            if (IsOnEVA && ((pcm.seat != null) || (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)))
            {
                Core.Log(Name + " is back from EVA.", Core.LogLevel.Important);
                IsOnEVA = false;
            }

            fmBonusSums.Clear();
            fmBonusSums.Add("All", 0);
            fmFreeMultipliers.Clear();
            fmFreeMultipliers.Add("All", 1);
            minMultipliers.Clear();
            minMultipliers.Add("All", 1);
            maxMultipliers.Clear();
            maxMultipliers.Add("All", 1);
            foreach (HealthFactor f in Core.Factors)
            {
                fmBonusSums.Add(f.Name, 0);
                fmFreeMultipliers.Add(f.Name, 1);
                minMultipliers.Add(f.Name, 1);
                maxMultipliers.Add(f.Name, 1);
            }

            // Processing parts
            if (Core.IsKerbalLoaded(pcm))
            {
                LastMarginalPositiveChange = LastMarginalNegativeChange = 0;
                foreach (Part p in Core.KerbalVessel(pcm).Parts)
                    ProcessPart(p, p.protoModuleCrew.ToArray(), ref change);
            }
            else if (Core.IsInEditor && KerbalHealthEditorReport.HealthModulesEnabled)
            {
                LastMarginalPositiveChange = LastMarginalNegativeChange = 0;
                foreach (PartCrewManifest p in ShipConstruction.ShipManifest.PartManifests)
                    ProcessPart(p.PartInfo.partPrefab, p.GetPartCrew(), ref change);
            }

            LastChange = 0;
            bool recalculateCache = Core.IsKerbalLoaded(pcm) || Core.IsInEditor;
            if (recalculateCache || (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)) CachedChange = 0;
            else Core.Log("Cached HP change for " + pcm.name + " is " + CachedChange + " HP/day.");
            Core.Log("Processing all the " + Core.Factors.Count + " factors for " + Name + "...");
            foreach (HealthFactor f in Core.Factors)
            {
                if (f.Cachable && !recalculateCache)
                {
                    Core.Log(f.Name + " is not recalculated for " + pcm.name + " (" + HighLogic.LoadedScene + " scene, " + (Core.IsKerbalLoaded(pcm) ? "" : "not ") + "loaded, " + (IsOnEVA ? "" : "not ") + "on EVA).");
                    continue;
                }
                double c = f.ChangePerDay(pcm) * Multiplier(f.Name) * Multiplier("All");
                Core.Log(f.Name + "'s effect on " + pcm.name + " is " + c + " HP/day.");
                if (f.Cachable) CachedChange += c;
                else LastChange += c;
            }
            LastChange += CachedChange;

            double mc = MarginalChange;
            Core.Log("Marginal change for " + pcm.name + ": " + mc + " (+" + LastMarginalPositiveChange + "%, -" + LastMarginalNegativeChange + "%).");
            Core.Log("Total change for " + pcm.name + ": " + (LastChange + mc) + " HP/day.");
            return LastChangeTotal;
        }

        /// <summary>
        /// Updates kerbal's HP and status
        /// </summary>
        /// <param name="interval">Number of seconds since the last update</param>
        public void Update(double interval)
        {
            Core.Log("Updating " + Name + "'s health.");
            //if (DFWrapper.APIReady && DFWrapper.DeepFreezeAPI.FrozenKerbals.ContainsKey(Name))
            //{
            //    Core.Log(Name + " is frozen with DeepFreeze; health will not be updated.");
            //    DFWrapper.KerbalInfo dfki;
            //    DFWrapper.DeepFreezeAPI.FrozenKerbals.TryGetValue(Name, out dfki);
            //    if (dfki == null) Core.Log("However, kerbal " + Name + " couldn't be retrieved from FrozenKerbals.");
            //    else Core.Log(Name + "'s rosters status: " + dfki.status + "; type: " + dfki.type);
            //    return;
            //}
            HP += HealthChangePerDay() / KSPUtil.dateTimeFormatter.Day * interval;
            if ((HP <= 0) && Core.DeathEnabled)
            {
                Core.Log(Name + " dies due to having " + HP + " health.", Core.LogLevel.Important);
                if (PCM.seat != null) PCM.seat.part.RemoveCrewmember(PCM);
                //if (IsOnEVA && (Core.KerbalVessel(PCM) == FlightGlobals.ActiveVessel))
                //    FlightGlobals.SetActiveVessel(FlightGlobals.FindNearestControllableVessel(FlightGlobals.ActiveVessel));
                PCM.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                Vessel.CrewWasModified(Core.KerbalVessel(PCM));
                Core.ShowMessage(Name + " has died of poor health!", true);
            }
            if (Core.RadiationEnabled)
            {
                Radiation = GetCurrentRadiation();
                Dose += Radiation / KSPUtil.dateTimeFormatter.Day * interval;
                Core.Log(Name + "'s radiation level is " + Radiation + " BED/day. Total accumulated dose is " + Dose + " BED.");
            }
            if (HasCondition("Exhausted"))
            {
                if (HP >= Core.ExhaustionEndHealth * MaxHP)
                {
                    RemoveCondition("Exhausted");
                    Core.ShowMessage(Name + " is no longer exhausted.", PCM);
                }
            }
            else
            if (HP < Core.ExhaustionStartHealth * MaxHP)
            {
                AddCondition(new HealthCondition("Exhausted"));
                Core.ShowMessage(Name + " is exhausted!", PCM);
            }
        }

        public ConfigNode ConfigNode
        {
            get
            {
                ConfigNode n = new ConfigNode("KerbalHealthStatus");
                n.AddValue("name", Name);
                n.AddValue("health", HP);
                if (MaxHPModifier != 0) n.AddValue("maxHPModifier", MaxHPModifier);
                n.AddValue("dose", Dose);
                n.AddValue("radiation", Radiation);
                n.AddValue("exposure", Exposure);
                foreach (HealthCondition hc in Conditions)
                    n.AddNode(hc.ConfigNode);
                if (HasCondition("Exhausted")) n.AddValue("trait", Trait);
                if (CachedChange != 0) n.AddValue("cachedChange", CachedChange);
                if (LastMarginalPositiveChange != 0) n.AddValue("lastMarginalPositiveChange", LastMarginalPositiveChange);
                if (LastMarginalNegativeChange != 0) n.AddValue("lastMarginalNegativeChange", LastMarginalNegativeChange);
                if (IsOnEVA) n.AddValue("onEva", true);
                return n;
            }
            set
            {
                Name = value.GetValue("name");
                HP = Core.GetDouble(value, "health", MaxHP);
                MaxHPModifier = Core.GetDouble(value, "maxHPModifier");
                Dose = Core.GetDouble(value, "dose");
                Radiation = Core.GetDouble(value, "radiation");
                Exposure = Core.GetDouble(value, "exposure", 1);
                foreach (ConfigNode n in value.GetNodes("HealthCondition"))
                    AddCondition(new HealthCondition(n));
                if (HasCondition("Exhausted")) Trait = value.GetValue("trait");
                CachedChange = Core.GetDouble(value, "cachedChange");
                LastMarginalPositiveChange = Core.GetDouble(value, "lastMarginalPositiveChange");
                LastMarginalNegativeChange = Core.GetDouble(value, "lastMarginalNegativeChange");
                IsOnEVA = Core.GetBool(value, "onEva");
            }
        }

        public override bool Equals(object obj)
        { return ((KerbalHealthStatus)obj).Name.Equals(Name); }

        public override int GetHashCode() { return ConfigNode.GetHashCode(); }

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

        public KerbalHealthStatus(ConfigNode node) { ConfigNode = node; }
    }
}
