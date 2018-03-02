using System;
using System.Collections.Generic;

namespace KerbalHealth
{
    /// <summary>
    /// Contains data about a kerbal's health
    /// </summary>
    public class KerbalHealthStatus
    {
        #region BASIC PROPERTIES
        string name;
        /// <summary>
        /// Kerbal's name
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                name = value;
                pcmCached = null;
            }
        }

        string trait = null;
        /// <summary>
        /// Returns saved kerbal's trait or current trait if nothing is saved
        /// </summary>
        string Trait
        {
            get => trait ?? PCM.trait;
            set => trait = value;
        }

        /// <summary>
        /// Returns true if the kerbal is marked as being on EVA
        /// </summary>
        public bool IsOnEVA { get; set; } = false;

        /// <summary>
        /// Returns true if a low health alarm has been shown for the kerbal
        /// </summary>
        public bool IsWarned { get; set; } = true;

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
                    if (pcm.name == Name) return pcmCached = pcm;
                foreach (ProtoCrewMember pcm in HighLogic.fetch.currentGame.CrewRoster.Tourist)
                    if (pcm.name == Name) return pcmCached = pcm;
                foreach (ProtoCrewMember pcm in HighLogic.fetch.currentGame.CrewRoster.Unowned)
                    if (pcm.name == Name) return pcmCached = pcm;
                return null;
            }
            set
            {
                Name = value.name;
                pcmCached = value;
            }
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

        public VesselHealthInfo VesselHealthInfo { get; set; } = new VesselHealthInfo();

        #endregion
        #region CONDITIONS

        /// <summary>
        /// Returns a list of all active health conditions for the kerbal
        /// </summary>
        public List<HealthCondition> Conditions { get; set; } = new List<HealthCondition>();

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
        public bool HasCondition(string condition) => GetCondition(condition) != null;

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
        #endregion
        #region QUIRKS

        /// <summary>
        /// List of this kerbal's quirks
        /// </summary>
        public List<Quirk> Quirks { get; set; } = new List<Quirk>();

        /// <summary>
        /// Last level processed for the kerbal
        /// </summary>
        public int QuirkLevel { get; set; } = 0;

        /// <summary>
        /// Adds the quirk unless it is already present
        /// </summary>
        /// <param name="quirk"></param>
        public void AddQuirk(Quirk quirk)
        { if (!Quirks.Contains(quirk)) Quirks.Add(quirk); }

        /// <summary>
        /// Adds the quirk unless it is already present
        /// </summary>
        /// <param name="quirk"></param>
        public void AddQuirk(string quirk)
        {
            Quirk q = Core.GetQuirk(quirk);
            if (q == null) q = new Quirk(quirk);
            Core.Quirks.Add(q);
            AddQuirk(q);
        }

        static double GetQuirkWeight(double val, double k) => val * (2 - 4 / (k + 1)) + 2 / (k + 1);

        public Quirk GetRandomQuirk(int level)
        {
            List<Quirk> availableQuirks = new List<Quirk>();
            List<double> weights = new List<double>();
            double weightSum = 0;
            foreach (Quirk q in Core.Quirks)
                if (q.IsVisible && q.IsAvailableTo(this, level) && !Quirks.Contains(q))
                {
                    availableQuirks.Add(q);
                    double w = GetQuirkWeight(PCM.courage, q.CourageWeight) * GetQuirkWeight(PCM.stupidity, q.StupidityWeight);
                    weightSum += w;
                    weights.Add(w);
                    Core.Log("Available quirk: " + q.Name + " (weight " + w + ")");
                }
            if ((availableQuirks.Count == 0) || (weightSum <= 0))
            {
                Core.Log("No available quirks found for " + Name + " (level " + level + ").", Core.LogLevel.Important);
                return null;
            }
            double r = Core.rand.NextDouble() * weightSum;
            Core.Log("Quirk selection roll: " + r + " out of " + weightSum);
            for (int i = 0; i < availableQuirks.Count; i++)
            {
                r -= weights[i];
                if (r < 0)
                {
                    Core.Log("Quirk " + availableQuirks[i].Name + " selected.");
                    return availableQuirks[i];
                }
            }
            Core.Log("Something is terribly wrong with quirk selection!", Core.LogLevel.Error);
            return null;
        }

        public void AddRandomQuirk(int level)
        {
            Quirk q = GetRandomQuirk(level);
            Quirks.Add(q);
            Core.ShowMessage(Name + " acquired a new quirk.\n" + q, true, false);
        }

        public void AddRandomQuirk() => AddRandomQuirk(PCM.experienceLevel);

        public void AwardQuirks()
        {
            for (int l = QuirkLevel + 1; l <= PCM.experienceLevel; l++)
            {
                if (Quirks.Count >= 2) break;
                double r = Core.rand.NextDouble();
                if (r < 0.2)
                {
                    Core.Log("A quirk will be added to " + Name + " (level " + l + "). Dice roll = " + r);
                    AddRandomQuirk(l);
                }
                else Core.Log("No quirks will be added to " + Name + " (level " + l + "). Dice roll = " + r);
            }
            QuirkLevel = PCM.experienceLevel;
        }

        #endregion
        #region HP
        double hp;
        /// <summary>
        /// Kerbal's health points
        /// </summary>
        public double HP
        {
            get => hp;
            set
            {
                if (value < 0) hp = 0;
                else if (value > MaxHP) hp = MaxHP;
                else hp = value;
                if (!IsWarned && Health < Core.LowHealthAlert)
                {
                    Core.ShowMessage(Name + "'s health is dangerously low!", true);
                    IsWarned = true;
                }
                else if (IsWarned && Health >= Core.LowHealthAlert) IsWarned = false;
            }
        }

        /// <summary>
        /// Returns the max number of HP for the kerbal (not including the modifier)
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static double GetMaxHP(ProtoCrewMember pcm) => Core.BaseMaxHP + (pcm != null ? Core.HPPerLevel * pcm.experienceLevel : 0);

        /// <summary>
        /// Returns the max number of HP for the kerbal (including the modifier)
        /// </summary>
        public double MaxHP
        {
            get
            {
                double k = 1;
                foreach (Quirk q in Quirks)
                    if (q != null)
                        foreach (HealthEffect he in q.Effects)
                            if (he != null) k *= he.MaxHP;
                return (GetMaxHP(PCM) + MaxHPModifier) * RadiationMaxHPModifier * k;
            }
        }

        /// <summary>
        /// Returns kerbal's HP relative to MaxHealth (0 to 1)
        /// </summary>
        public double Health => HP / MaxHP;

        /// <summary>
        /// Health points added to (or subtracted from) kerbal's max HP
        /// </summary>
        public double MaxHPModifier { get; set; }
        #endregion
        #region HP CHANGE
        double CachedChange { get; set; } = 0;

        /// <summary>
        /// HP change per day rate in the latest update. Only includes factors, not marginal change
        /// </summary>
        public double LastChange { get; set; } = 0;

        /// <summary>
        /// Health recuperation in the latest update
        /// </summary>
        public double LastRecuperation { get; set; } = 0;

        /// <summary>
        /// Health decay in the latest update
        /// </summary>
        public double LastDecay { get; set; } = 0;

        /// <summary>
        /// HP change due to recuperation/decay
        /// </summary>
        public double MarginalChange => (MaxHP - HP) * (LastRecuperation / 100) - HP * (LastDecay / 100);

        /// <summary>
        /// Total HP change per day rate in the latest update
        /// </summary>
        public double LastChangeTotal => LastChange + MarginalChange;

        /// <summary>
        /// List of factors' effect on the kerbal (used for monitoring only)
        /// </summary>
        public Dictionary<string, double> Factors { get; set; } = new Dictionary<string, double>(Core.Factors.Count);
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
        public double TimeToNextCondition() => TimeToValue(NextConditionHP());

        /// <summary>
        /// Returns HP level when marginal HP change balances out "fixed" change. If <= 0, no such level
        /// </summary>
        /// <returns></returns>
        public double GetBalanceHP()
        {
            Core.Log(Name + "'s last change: " + LastChange + ". Recuperation: " + LastRecuperation + "%. Decay: " + LastDecay + "%.");
            if (LastChange == 0) HealthChangePerDay();
            if (LastRecuperation <= LastDecay) return 0;
            return (MaxHP * LastRecuperation + LastChange * 100) / (LastRecuperation - LastDecay);
        }

        #endregion
        #region RADIATION
        /// <summary>
        /// Lifetime absorbed dose of ionizing radiation, in banana equivalent doses (BEDs, 1 BED = 1e-7 Sv)
        /// </summary>
        public double Dose { get; set; }

        /// <summary>
        /// Returns the fraction of max HP that the kerbal has considering radiation effects. 1e7 of RadiationDose = -25% of MaxHP
        /// </summary>
        public double RadiationMaxHPModifier => Core.RadiationEnabled ? 1 - Dose * 1e-7 * Core.RadiationEffect : 1;

        /// <summary>
        /// Level of background radiation absorbed by the body, in bananas per day
        /// </summary>
        public double Radiation { get; set; }

        double partsRadiation = 0;

        /// <summary>
        /// Proportion of radiation that gets absorbed by the kerbal
        /// </summary>
        public double Exposure { get; set; }

        public static double GetExposure(double shielding, double crewCap) => Math.Pow(2, -shielding * Core.ShieldingEffect / Math.Pow(crewCap, 2f / 3));

        static double GetSolarRadiationAtDistance(double distance) => Core.SolarRadiation * Core.Sqr(FlightGlobals.GetHomeBody().orbit.radius / distance);

        static bool IsPlanet(CelestialBody body) => body?.orbit?.referenceBody == Sun.Instance.sun;

        static CelestialBody GetPlanet(CelestialBody body) => ((body == null) || IsPlanet(body)) ? body : GetPlanet(body?.orbit?.referenceBody);

        /// <summary>
        /// Returns level of current cosmic radiation for this kerbal, before exposure
        /// </summary>
        /// <returns>Cosmic radiation level in bananas/day</returns>
        public double GetCosmicRadiation()
        {
            double cosmicRadiationRate = 1, distanceToSun = 0;
            Vessel v = Core.KerbalVessel(PCM);
            if (v == null)
            {
                Core.Log(Name + "'s vessel not found. No radiation added.", Core.LogLevel.Important);
                return 0;
            }
            Core.Log(Name + " is in " + v.vesselName + " in " + v.mainBody.bodyName + "'s SOI at an altitude of " + v.altitude + ", situation: " + v.SituationString + ", distance to Sun: " + v.distanceToSun);
            if (v.mainBody != Sun.Instance.sun)
            {
                distanceToSun = (v.distanceToSun > 0) ? v.distanceToSun : GetPlanet(v.mainBody).orbit.radius;
                if (IsPlanet(v.mainBody) && (v.altitude < v.mainBody.scienceValues.spaceAltitudeThreshold)) cosmicRadiationRate = Core.InSpaceLowCoefficient;
                else cosmicRadiationRate = Core.InSpaceHighCoefficient;
                if (v.mainBody.atmosphere)
                    if (v.altitude < v.mainBody.scienceValues.flyingAltitudeThreshold) cosmicRadiationRate *= Core.TroposphereCoefficient;
                    else if (v.altitude < v.mainBody.atmosphereDepth) cosmicRadiationRate *= Core.StratoCoefficient;
                if (v.altitude < v.mainBody.Radius * Core.BodyShieldingAltitude) cosmicRadiationRate *= 0.5;  // Half of radiation is blocked by the celestial body when very close to it
            }
            else distanceToSun = v.altitude + Sun.Instance.sun.Radius;
            Core.Log("Solar Radiation Quoficient = " + cosmicRadiationRate);
            Core.Log("Distance to Sun = " + distanceToSun + " (" + (distanceToSun / FlightGlobals.GetHomeBody().orbit.radius) + " AU)");
            Core.Log("Nominal Solar Radiation @ Vessel's Location = " + GetSolarRadiationAtDistance(distanceToSun));
            Core.Log("Nominal Galactic Radiation = " + Core.GalacticRadiation);
            return cosmicRadiationRate * (GetSolarRadiationAtDistance(distanceToSun) + Core.GalacticRadiation) * KSPUtil.dateTimeFormatter.Day / 21600;
        }

        #endregion
        #region HEALTH UPDATE

        /// <summary>
        /// Returns effective HP change rate per day
        /// </summary>
        /// <returns></returns>
        public double HealthChangePerDay()
        {
            ProtoCrewMember pcm = PCM;
            if (pcm == null)
            {
                Core.Log(Name + " was not found in the kerbal roster!", Core.LogLevel.Error);
                return 0;
            }

            if (HasCondition("Frozen"))
            {
                Core.Log(Name + " is frozen, health does not change.");
                return 0;
            }

            if (IsOnEVA && ((pcm.seat != null) || (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)))
            {
                Core.Log(Name + " is back from EVA.", Core.LogLevel.Important);
                IsOnEVA = false;
            }

            LastChange = 0;
            bool recalculateCache = Core.IsKerbalLoaded(pcm) || Core.IsInEditor;
            if (recalculateCache || (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned))
            {
                CachedChange = 0;
                Factors = new Dictionary<string, double>(Core.Factors.Count);
            }
            else Core.Log("Cached HP change for " + pcm.name + " is " + CachedChange + " HP/day.");

            // Processing parts
            if (Core.IsKerbalLoaded(pcm) || Core.IsInEditor)
            {
                Core.Log("VHI cache contains " + VesselHealthInfo.Cache.Count + " record(s).");
                VesselHealthInfo = VesselHealthInfo.GetVesselInfo(pcm).Clone();
                Core.Log("Vessel Health Info before applying part and kerbal modifiers:\n" + VesselHealthInfo);
                Core.Log("Now about to process part " + Core.GetCrewPart(pcm)?.name + " where " + Name + " is located.");
                VesselHealthInfo.ProcessPart(Core.GetCrewPart(pcm), true);
                foreach (Quirk q in Quirks) q.Apply(this);
                Core.Log("Vessel Health Info after applying all modifiers:\n" + VesselHealthInfo);
                LastChange = VesselHealthInfo.HPChange;
                LastRecuperation = VesselHealthInfo.Recuperation;
                LastDecay = VesselHealthInfo.Decay;
                partsRadiation = VesselHealthInfo.PartsRadiation;
                Exposure = GetExposure(VesselHealthInfo.Shielding, Core.GetCrewCapacity(pcm));
                if (IsOnEVA) Exposure *= Core.EVAExposure;
            }

            Core.Log("Processing " + Core.Factors.Count + " factors for " + Name + "...");
            int crewCount = Core.GetCrewCount(pcm);
            foreach (HealthFactor f in Core.Factors)
            {
                if (f.Cachable && !recalculateCache)
                {
                    Core.Log(f.Name + " is not recalculated for " + pcm.name + " (" + HighLogic.LoadedScene + " scene, " + (Core.IsKerbalLoaded(pcm) ? "" : "not ") + "loaded, " + (IsOnEVA ? "" : "not ") + "on EVA).");
                    continue;
                }
                double c = f.ChangePerDay(pcm) * VesselHealthInfo.GetMultiplier(f.Name, crewCount) * VesselHealthInfo.GetMultiplier("All", crewCount);
                Core.Log("Multiplier for " + f.Name + " is " + VesselHealthInfo.GetMultiplier(f.Name, crewCount) + " * " + VesselHealthInfo.FreeMultipliers[f.Name] + " (bonus sum: " + VesselHealthInfo.BonusSums[f.Name] + "; multipliers: " + VesselHealthInfo.MinMultipliers[f.Name] + ".." + VesselHealthInfo.MaxMultipliers[f.Name] + ")");
                Core.Log(f.Name + "'s effect on " + pcm.name + " is " + c + " HP/day.");
                Factors[f.Name] = c;
                if (f.Cachable) CachedChange += c;
                else LastChange += c;
            }
            LastChange += CachedChange;
            double mc = MarginalChange;

            Core.Log("Recuperation/decay change for " + pcm.name + ": " + mc + " (+" + LastRecuperation + "%, -" + LastDecay + "%).");
            Core.Log("Total change for " + pcm.name + ": " + (LastChange + mc) + " HP/day.");
            if (recalculateCache) Core.Log("Total shielding: " + VesselHealthInfo.Shielding + "; crew capacity: " + Core.GetCrewCapacity(pcm));
            return LastChangeTotal;
        }

        /// <summary>
        /// Updates kerbal's HP and status
        /// </summary>
        /// <param name="interval">Number of seconds since the last update</param>
        public void Update(double interval)
        {
            Core.Log("Updating " + Name + "'s health.");

            // UNCOMMENT THE NEXT LINE TO ENABLE AWARDING QUIRKS
            //AwardQuirks();

            bool frozen = HasCondition("Frozen");

            if (Core.RadiationEnabled && ((PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || frozen))
            {
                Radiation = Exposure * (partsRadiation + GetCosmicRadiation());
                Dose += Radiation / KSPUtil.dateTimeFormatter.Day * interval;
                Core.Log(Name + "'s radiation level is " + Radiation + " bananas/day. Total accumulated dose is " + Dose + " bananas.");
            }
            else Radiation = 0;

            if (frozen)
            {
                Core.Log(Name + " is frozen, health doesn't change.");
                return;
            }

            HP += HealthChangePerDay() / KSPUtil.dateTimeFormatter.Day * interval;

            if ((HP <= 0) && Core.DeathEnabled)
            {
                Core.Log(Name + " dies due to having " + HP + " health.", Core.LogLevel.Important);
                if (PCM.seat != null) PCM.seat.part.RemoveCrewmember(PCM);
                PCM.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                Vessel.CrewWasModified(Core.KerbalVessel(PCM));
                Core.ShowMessage(Name + " has died of poor health!", true);
            }

            if (HasCondition("Exhausted"))
            {
                if (HP >= Core.ExhaustionEndHealth * MaxHP)
                {
                    RemoveCondition("Exhausted");
                    Core.ShowMessage(Name + " is no longer exhausted.", PCM);
                }
            }
            else if (HP < Core.ExhaustionStartHealth * MaxHP)
            {
                AddCondition(new HealthCondition("Exhausted"));
                Core.ShowMessage(Name + " is exhausted!", PCM);
            }
        }
        #endregion
        #region SAVING, LOADING, INITIALIZING ETC.
        public ConfigNode ConfigNode
        {
            get
            {
                ConfigNode n = new ConfigNode("KerbalHealthStatus");
                n.AddValue("name", Name);
                n.AddValue("health", HP);
                if (MaxHPModifier != 0) n.AddValue("maxHPModifier", MaxHPModifier);
                n.AddValue("dose", Dose);
                if (Radiation != 0) n.AddValue("radiation", Radiation);
                if (partsRadiation != 0) n.AddValue("partsRadiation", partsRadiation);
                if (Exposure != 1) n.AddValue("exposure", Exposure);
                foreach (HealthCondition hc in Conditions)
                    n.AddNode(hc.ConfigNode);
                foreach (Quirk q in Quirks)
                    n.AddValue("quirk", q.Name);
                if (QuirkLevel != 0) n.AddValue("quirkLevel", QuirkLevel);
                if (HasCondition("Exhausted")) n.AddValue("trait", Trait);
                if (CachedChange != 0) n.AddValue("cachedChange", CachedChange);
                if (LastRecuperation != 0) n.AddValue("lastRecuperation", LastRecuperation);
                if (LastDecay != 0) n.AddValue("lastDecay", LastDecay);
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
                partsRadiation = Core.GetDouble(value, "partsRadiation");
                Exposure = Core.GetDouble(value, "exposure", 1);
                foreach (ConfigNode n in value.GetNodes("HealthCondition"))
                    AddCondition(new HealthCondition(n));
                foreach (string s in value.GetValues("quirk"))
                    AddQuirk(s);
                QuirkLevel = Core.GetInt(value, "quirkLevel");
                if (HasCondition("Exhausted")) Trait = value.GetValue("trait");
                CachedChange = Core.GetDouble(value, "cachedChange");
                LastRecuperation = Core.GetDouble(value, "lastRecuperation");
                LastDecay = Core.GetDouble(value, "lastDecay");
                IsOnEVA = Core.GetBool(value, "onEva");
            }
        }

        public override bool Equals(object obj) => ((KerbalHealthStatus)obj).Name.Equals(Name);

        public override int GetHashCode() => ConfigNode.GetHashCode();

        public KerbalHealthStatus Clone() => (KerbalHealthStatus)this.MemberwiseClone();

        public KerbalHealthStatus(string name, double health)
        {
            Name = name;
            HP = health;
        }

        public KerbalHealthStatus(string name)
        {
            Name = name;
            HP = MaxHP;
            Core.Log("Created record for " + name + " with " + HP + " HP.");
        }

        public KerbalHealthStatus(ConfigNode node) => ConfigNode = node;
        #endregion
    }
}
