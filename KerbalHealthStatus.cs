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
                try { return pcmCached = HighLogic.fetch.currentGame.CrewRoster[Name]; }
                catch (Exception)
                {
                    Core.Log("Could not find ProtoCrewMember for " + Name + ". KerbalHealth kerbal list contains " + Core.KerbalHealthList.Count + " records:\r\n" + Core.KerbalHealthList);
                    return null;
                }
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

        public string LocationString
        {
            get
            {
                switch (PCM.rosterStatus)
                {
                    case ProtoCrewMember.RosterStatus.Available: return "KSC";
                    case ProtoCrewMember.RosterStatus.Dead: return "Dead";
                    case ProtoCrewMember.RosterStatus.Missing: return "Unknown";
                }
                Vessel v = Core.KerbalVessel(PCM);
                if (v == null) return "???";
                if (v.isEVA) return "EVA (" + v.mainBody.bodyName + ")";
                return v.vesselName;
            }
        }

        public HealthModifierSet VesselModifiers { get; set; } = new HealthModifierSet();

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
        /// Returns true if a given condition exists for the kerbal
        /// </summary>
        /// <param name="hc"></param>
        /// <returns></returns>
        public bool HasCondition(HealthCondition hc) => Conditions.Contains(hc);

        /// <summary>
        /// Whether the kerbal is frozen by DeepFreeze mod (has 'Frozen' condition)
        /// </summary>
        public bool IsFrozen
        {
            get => HasCondition("Frozen");
            set
            {
                if (value) AddCondition("Frozen");
                else RemoveCondition("Frozen");
            }
        }

        /// <summary>
        /// Adds a new health condition
        /// </summary>
        /// <param name="condition">Condition to add</param>
        public void AddCondition(HealthCondition condition)
        {
            if (condition == null) return;
            Core.Log("Adding " + condition.Name + " condition to " + Name + "...");
            if (!condition.Stackable && HasCondition(condition)) return;
            Conditions.Add(condition);
            if (Core.ConditionsEnabled) HP += condition.HP * Core.ConditionsEffect;
            Core.Log(condition.Name + " condition added to " + Name + ".", Core.LogLevel.Important);
            if (condition.Incapacitated) MakeIncapacitated();
            if (condition.Visible) Core.ShowMessage(Name + " has acquired " + condition.Title + " condition!\r\n" + condition.Description, PCM);
        }

        public void AddCondition(string condition) => AddCondition(Core.GetHealthCondition(condition));

        /// <summary>
        /// Removes a condition with from the kerbal
        /// </summary>
        /// <param name="condition">Name of condition to remove</param>
        /// <param name="removeAll">If true, all conditions with the same name will be removed. Makes sense for additive conditions. Default is false</param>
        public void RemoveCondition(HealthCondition condition, bool removeAll = false)
        {
            if (condition == null) return;
            Core.Log("Removing " + condition.Name + " condition from " + Name + ".", Core.LogLevel.Important);
            int n = 0;
            if (removeAll)
            {
                while (Conditions.Remove(condition)) n++;
                Core.Log(n + " instance(s) of " + condition.Name + " removed.", Core.LogLevel.Important);
            }
            else n = Conditions.Remove(condition) ? 1 : 0;
            if (Core.ConditionsEnabled && condition.RestoreHP) HP -= condition.HP * n * Core.ConditionsEffect;
            if ((n > 0) && condition.Incapacitated && IsCapable) MakeCapable();
            if ((n > 0) && condition.Visible)
                Core.ShowMessage(Name + " has lost " + condition.Title + " condition!", PCM);
        }

        public void RemoveCondition(string condition, bool removeAll = false) => RemoveCondition(Core.GetHealthCondition(condition), removeAll);

        /// <summary>
        /// Returns a comma-separated list of visible conditions or "OK" if there are no visible conditions
        /// </summary>
        public string ConditionString
        {
            get
            {
                string res = "";
                foreach (HealthCondition hc in Conditions)
                    if (hc.Visible)
                    {
                        if (res != "") res += ", ";
                        res += hc.Title;
                    }
                if (res == "") res = "OK";
                return res;
            }
        }

        /// <summary>
        /// Returns false if at least one of kerbal's current health conditions makes him/her incapacitated (i.e. turns into a Tourist), true otherwise
        /// </summary>
        public bool IsCapable
        {
            get
            {
                foreach (HealthCondition hc in Conditions)
                    if (hc.Incapacitated) return false;
                return true;
            }
        }

        /// <summary>
        /// Turn a kerbal into a Tourist
        /// </summary>
        void MakeIncapacitated()
        {
            if ((Trait != null) && (PCM.type == ProtoCrewMember.KerbalType.Tourist))
            {
                Core.Log(Name + " is already incapacitated.", Core.LogLevel.Important);
                return;
            }
            Core.Log(Name + " (" + Trait + ") is incapacitated.", Core.LogLevel.Important);
            Trait = PCM.trait;
            PCM.type = ProtoCrewMember.KerbalType.Tourist;
            KerbalRoster.SetExperienceTrait(PCM, KerbalRoster.touristTrait);
        }

        /// <summary>
        /// Revives a kerbal after being incapacitated
        /// </summary>
        void MakeCapable()
        {
            if (PCM.type != ProtoCrewMember.KerbalType.Tourist) return;  // Apparently, the kerbal has already been revived by another mod
            Core.Log(Name + " is becoming " + (Trait ?? "something strange") + " again.", Core.LogLevel.Important);
            if ((Trait != null) && (Trait != "Tourist"))
            {
                PCM.type = ProtoCrewMember.KerbalType.Crew;
                KerbalRoster.SetExperienceTrait(PCM, Trait);
            }
            Trait = null;
        }

        /// <summary>
        /// Health level (in percentage) for Exhaustion condition to kick in
        /// </summary>
        public double ExhaustionStart
        {
            get
            {
                double xs = Core.ExhaustionStartHealth;
                if (Core.QuirksEnabled)
                    foreach (Quirk q in Quirks)
                        foreach (HealthEffect he in q.Effects)
                            if (he.IsApplicable(this)) xs *= he.ExhaustedStart;
                return xs;
            }
        }

        /// <summary>
        /// HP for Exhaustion condition to kick in
        /// </summary>
        public double ExhaustionStartHP => ExhaustionStart * MaxHP;

        /// <summary>
        /// Health level (in percentage) for Exhaustion condition to end
        /// </summary>
        public double ExhaustionEnd
        {
            get
            {
                double xe = Core.ExhaustionEndHealth;
                if (Core.QuirksEnabled)
                    foreach (Quirk q in Quirks)
                        foreach (HealthEffect he in q.Effects)
                            if (he.IsApplicable(this))
                                xe *= he.ExhaustedEnd;
                return xe;
            }
        }

        /// <summary>
        /// HP for Exhaustion condition to end
        /// </summary>
        public double ExhaustionEndHP => ExhaustionEnd * MaxHP;
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
                    double w = Core.StatsAffectQuirkWeights ? GetQuirkWeight(PCM.courage, q.CourageWeight) * GetQuirkWeight(PCM.stupidity, q.StupidityWeight) : 1;
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

        public Quirk AddRandomQuirk(int level)
        {
            Quirk q = GetRandomQuirk(level);
            Quirks.Add(q);
            Core.ShowMessage(Name + " acquired a new quirk: " + q, PCM);
            return q;
        }

        public Quirk AddRandomQuirk() => AddRandomQuirk(PCM.experienceLevel);

        public void AwardQuirks()
        {
            if (Core.AwardQuirksOnMissions || (PCM.rosterStatus == ProtoCrewMember.RosterStatus.Available))
            {
                for (int l = QuirkLevel + 1; l <= PCM.experienceLevel; l++)
                {
                    if (Quirks.Count >= Core.MaxQuirks) break;
                    double r = Core.rand.NextDouble();
                    if (r < Core.QuirkChance)
                    {
                        Core.Log("A quirk will be added to " + Name + " (level " + l + "). Dice roll = " + r);
                        AddRandomQuirk(l);
                    }
                    else Core.Log("No quirks will be added to " + Name + " (level " + l + "). Dice roll = " + r);
                }
                QuirkLevel = PCM.experienceLevel;
            }
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
                double k = 1, a = 0;
                if (Core.QuirksEnabled)
                    foreach (Quirk q in Quirks)
                        if (q != null)
                            foreach (HealthEffect he in q.Effects)
                                if ((he != null) && he.IsApplicable(this))
                                {
                                    a += he.MaxHPBonus;
                                    k *= he.MaxHP;
                                }
                return (GetMaxHP(PCM) + MaxHPModifier + a) * RadiationMaxHPModifier * k;
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
        public double LastChangeTotal { get; set; }// => LastChange + MarginalChange;

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
                    return ExhaustionEndHP;
                else return MaxHP;
            if (LastChangeTotal < 0)
                if (HasCondition("Exhausted")) return 0;
                else return ExhaustionStartHP;
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
        public double LastExposure { get; set; } = 1;

        static double GetSolarRadiationAtDistance(double distance) => Core.SolarRadiation * Core.Sqr(FlightGlobals.GetHomeBody().orbit.radius / distance);

        public static double GetMagnetosphereCoefficient(Vessel v)
        {
            double cosmicRadiationRate = 1;
            double a = v.altitude;
            for (CelestialBody b = v.mainBody; b != Sun.Instance.sun; b = b.referenceBody)
            {
                if (Core.PlanetConfigs[b].Magnetosphere != 0)
                    if (a < b.scienceValues.spaceAltitudeThreshold)
                        cosmicRadiationRate *= Math.Pow(Core.InSpaceLowCoefficient, Core.PlanetConfigs[b].Magnetosphere);
                    else cosmicRadiationRate *= Math.Pow(Core.InSpaceHighCoefficient, Core.PlanetConfigs[b].Magnetosphere);
                a = b.orbit.altitude;
            }
            return cosmicRadiationRate;
        }

        /// <summary>
        /// Returns level of cosmic radiation reaching the given vessel
        /// </summary>
        /// <returns>Cosmic radiation level in bananas/day</returns>
        public static double GetCosmicRadiation(Vessel v)
        {
            double cosmicRadiationRate = 1, distanceToSun = 0;
            if (v == null)
            {
                Core.Log("Vessel is null. No radiation added.", Core.LogLevel.Important);
                return 0;
            }
            Core.Log(v.vesselName + " is in " + v.mainBody.bodyName + "'s SOI at an altitude of " + v.altitude + ", situation: " + v.SituationString + ", distance to Sun: " + v.distanceToSun);
            Core.Log("Configs for " + v.mainBody.bodyName + ":\r\n" + Core.PlanetConfigs[v.mainBody] ?? "NOT FOUND");

            if (v.mainBody != Sun.Instance.sun)
            {
                distanceToSun = (v.distanceToSun > 0) ? v.distanceToSun : Core.GetPlanet(v.mainBody).orbit.altitude;
                cosmicRadiationRate = GetMagnetosphereCoefficient(v);
                if (v.mainBody.atmosphere && (Core.PlanetConfigs[v.mainBody].AtmosphericAbsorption != 0))
                    if (v.altitude < v.mainBody.scienceValues.flyingAltitudeThreshold) cosmicRadiationRate *= Math.Pow(Core.TroposphereCoefficient, Core.PlanetConfigs[v.mainBody].AtmosphericAbsorption);
                    else if (v.altitude < v.mainBody.atmosphereDepth) cosmicRadiationRate *= Math.Pow(Core.StratoCoefficient, Core.PlanetConfigs[v.mainBody].AtmosphericAbsorption);
                double occlusionCoefficient = (Math.Sqrt(1 - Core.Sqr(v.mainBody.Radius) / Core.Sqr(v.mainBody.Radius + Math.Max(v.altitude, 0))) + 1) / 2;
                Core.Log("At an altitude of " + v.altitude + " m and R = " + v.mainBody.Radius + " m, occlusion coefficient is " + occlusionCoefficient.ToString("P2") + ".");
                cosmicRadiationRate *= occlusionCoefficient;
            }
            else distanceToSun = v.altitude + Sun.Instance.sun.Radius;
            double naturalRadiation = Core.PlanetConfigs[v.mainBody].Radioactivity * Core.Sqr(v.mainBody.Radius / (v.mainBody.Radius + v.altitude));
            Core.Log("Solar Radiation Quoficient = " + cosmicRadiationRate);
            Core.Log("Distance to Sun = " + distanceToSun + " (" + (distanceToSun / FlightGlobals.GetHomeBody().orbit.radius) + " AU)");
            Core.Log("Nominal Solar Radiation @ Vessel's Location = " + GetSolarRadiationAtDistance(distanceToSun));
            Core.Log("Nominal Galactic Radiation = " + Core.GalacticRadiation);
            Core.Log("Body's natural radiation = " + naturalRadiation);
            return cosmicRadiationRate * (GetSolarRadiationAtDistance(distanceToSun) + Core.GalacticRadiation) + naturalRadiation;
        }

        /// <summary>
        /// Body-emitted radiation reaching the given vessel
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static double GetNaturalRadiation(Vessel v) => Core.PlanetConfigs[v.mainBody].Radioactivity * Core.Sqr(v.mainBody.Radius / (v.mainBody.Radius + v.altitude));

        /// <summary>
        /// Returns true if the kerbal can start decontamination now
        /// </summary>
        public bool IsReadyForDecontamination => (PCM.rosterStatus == ProtoCrewMember.RosterStatus.Available) && (Health >= 1) && (Conditions.Count == 0) && ((HighLogic.CurrentGame.Mode != Game.Modes.CAREER) || Funding.CanAfford(Core.DecontaminationFundsCost)) && (((HighLogic.CurrentGame.Mode != Game.Modes.CAREER) && ((HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)) || ResearchAndDevelopment.CanAfford(Core.DecontaminationScienceCost))) && (ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) >= Core.DecontaminationAstronautComplexLevel) && (ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment) >= Core.DecontaminationRNDLevel);

        /// <summary>
        /// Returns true if the kerbal is currently decontaminating (i.e. has 'Decontaminating' condition)
        /// </summary>
        public bool IsDecontaminating => HasCondition("Decontaminating");

        void ShowXP()
        {
            Core.Log("XP level: " + PCM.experienceLevel + " (delta " + PCM.ExperienceLevelDelta + ")");
            Core.Log("Experience: " + PCM.experience);
            Core.Log("Extra XP: " + PCM.ExtraExperience);
            Core.Log("Full XP: " + PCM.CalculateExperiencePoints(HighLogic.CurrentGame));
        }

        public void StartDecontamination()
        {
            Core.Log("StartDecontamination for " + Name);
            if (!IsReadyForDecontamination)
            {
                Core.Log(Name + " is " + PCM.rosterStatus + "; HP: " + HP + "/" + MaxHP + "; has " + Conditions.Count + " condition(s)", Core.LogLevel.Error);
                return;
            }
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                Core.Log("Taking " + Core.DecontaminationFundsCost + " funds our of " + Funding.Instance.Funds.ToString("N0") + " available for decontamination.");
                Funding.Instance.AddFunds(-Core.DecontaminationFundsCost, TransactionReasons.None);
            }
            if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER) || (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
            {
                Core.Log("Taking " + Core.DecontaminationScienceCost + " science points for decontamination.");
                ResearchAndDevelopment.Instance.AddScience(-Core.DecontaminationScienceCost, TransactionReasons.None);
            }
            if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER) && Core.DecontaminationLevelLoss)  // Not implemented (always false)
            {
                Core.Log("Removing XP from " + Name + " (" + PCM.experience + " XP, level " + PCM.experienceLevel + ") for decontamination.");
                ShowXP();
                //PCM.experienceLevel--;
                //PCM.ExtraExperience -= PCM.experience / 2;
                Core.Log("Flight log for " + Name + " has " + PCM.flightLog.Count + " entries.");
                PCM.flightLog.AddEntry("Decontamination", Planetarium.fetch.Home.bodyName);
                Core.Log("Flight log for " + Name + " has " + PCM.flightLog.Count + " entries.");
                ShowXP();
                Core.Log("Archiving flight log...");
                PCM.ArchiveFlightLog();
                ShowXP();
                Core.Log("Updating experience...");
                PCM.UpdateExperience();
                ShowXP();
            }
            HP *= 1 - Core.DecontaminationHealthLoss;
            AddCondition("Decontaminating");
            Radiation = -Core.DecontaminationRate;
        }

        public void StopDecontamination()
        {
            Core.Log("StopDecontamination for " + Name);
            RemoveCondition("Decontaminating");
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

            if (IsFrozen || IsDecontaminating)
            {
                Core.Log(Name + " is frozen or decontaminating, health does not change.");
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

            // Processing parts and quirks
            HealthModifierSet mods;
            if (recalculateCache)
            {
                Core.Log("Vessel modifiers cache contains " + HealthModifierSet.VesselCache.Count + " record(s).");
                VesselModifiers = HealthModifierSet.GetVesselModifiers(pcm);
                mods = VesselModifiers.Clone();
                Core.Log("Vessel health modifiers before applying part and kerbal effects:\n" + mods);
                Core.Log("Now about to process part " + Core.GetCrewPart(pcm)?.name + " where " + Name + " is located.");
                mods.ProcessPart(Core.GetCrewPart(pcm), true);
                mods.ExposureMultiplier *= mods.GetExposure(Core.GetCrewCapacity(pcm));
                if (IsOnEVA) mods.ExposureMultiplier *= Core.EVAExposure;
            }
            else
            {
                mods = new HealthModifierSet();
                mods.MaxRecuperaction = mods.RecuperationPower = LastRecuperation;
                mods.Decay = LastDecay;
                mods.ExposureMultiplier = LastExposure;
            }

            // Applying quirks
            if (Core.QuirksEnabled)
                foreach (Quirk q in Quirks) q.Apply(this, mods);

            Core.Log("Health modifiers after applying all effects:\n" + mods);

            LastChange = mods.HPChange;
            LastRecuperation = mods.Recuperation;
            LastDecay = mods.Decay;
            LastExposure = mods.ExposureMultiplier;
            partsRadiation = mods.PartsRadiation;

            // Processing factors
            Core.Log("Processing " + Core.Factors.Count + " factors for " + Name + "...");
            int crewCount = Core.GetCrewCount(pcm);
            foreach (HealthFactor f in Core.Factors)
            {
                if (f.Cachable && !recalculateCache)
                {
                    Core.Log(f.Name + " is not recalculated for " + pcm.name + " (" + HighLogic.LoadedScene + " scene, " + (Core.IsKerbalLoaded(pcm) ? "" : "not ") + "loaded, " + (IsOnEVA ? "" : "not ") + "on EVA).");
                    continue;
                }
                double c = f.ChangePerDay(pcm) * mods.GetMultiplier(f.Name, crewCount) * mods.GetMultiplier("All", crewCount);
                Core.Log("Multiplier for " + f.Name + " is " + mods.GetMultiplier(f.Name, crewCount) + " * " + mods.FreeMultipliers[f.Name] + " (bonus sum: " + mods.BonusSums[f.Name] + "; multipliers: " + mods.MinMultipliers[f.Name] + ".." + mods.MaxMultipliers[f.Name] + ")");
                Core.Log(f.Name + "'s effect on " + pcm.name + " is " + c + " HP/day.");
                Factors[f.Name] = c;
                if (f.Cachable) CachedChange += c;
                else LastChange += c;
            }
            LastChange += CachedChange;
            double mc = MarginalChange;
            LastChangeTotal = LastChange + mc;

            Core.Log("Recuperation/decay change for " + pcm.name + ": " + mc + " (+" + LastRecuperation + "%, -" + LastDecay + "%).");
            Core.Log("Total change for " + pcm.name + ": " + LastChangeTotal + " HP/day.");
            if (recalculateCache) Core.Log("Total shielding: " + mods.Shielding + "; crew capacity: " + Core.GetCrewCapacity(pcm));
            return LastChangeTotal;
        }

        /// <summary>
        /// Updates kerbal's HP and status
        /// </summary>
        /// <param name="interval">Number of seconds since the last update</param>
        public void Update(double interval)
        {
            Core.Log("Updating " + Name + "'s health.");

            if (PCM == null)
            {
                Core.Log(Name + " ProtoCrewMember record not found. Aborting health update.", Core.LogLevel.Error);
                return;
            }

            if (Core.QuirksEnabled) AwardQuirks();

            bool frozen = IsFrozen;
            bool decontaminating = IsDecontaminating;

            if (Core.RadiationEnabled)
            {
                if ((PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || frozen)
                {
                    Radiation = LastExposure * (partsRadiation + GetCosmicRadiation(Core.KerbalVessel(PCM))) * KSPUtil.dateTimeFormatter.Day / 21600;
                    Core.Log(Name + "'s radiation level is " + Radiation + " bananas/day. Total accumulated dose is " + Dose + " bananas.");
                    if (decontaminating) StopDecontamination();
                }
                else if (!decontaminating) Radiation = 0;

                Dose += Radiation / KSPUtil.dateTimeFormatter.Day * interval;
                if (Dose < 0)
                {
                    Dose = 0;
                    if (decontaminating) StopDecontamination();
                }
            }

            if (frozen)
            {
                Core.Log(Name + " is frozen, health doesn't change.");
                return;
            }

            if (!decontaminating)
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
                if (HP >= ExhaustionEndHP)
                {
                    RemoveCondition("Exhausted");
                    Core.ShowMessage(Name + " is no longer exhausted.", PCM);
                }
            }
            else if (HP < ExhaustionStartHP)
            {
                AddCondition("Exhausted");
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
                if (LastExposure != 1) n.AddValue("exposure", LastExposure);
                foreach (HealthCondition hc in Conditions) n.AddValue("condition", hc.Name);
                foreach (Quirk q in Quirks) n.AddValue("quirk", q.Name);
                if (QuirkLevel != 0) n.AddValue("quirkLevel", QuirkLevel);
                if (!IsCapable) n.AddValue("trait", Trait);
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
                LastExposure = Core.GetDouble(value, "exposure", 1);
                foreach (string s in value.GetValues("condition"))
                    Conditions.Add(Core.GetHealthCondition(s));
                foreach (ConfigNode n in value.GetNodes("HealthCondition"))
                    Conditions.Add(Core.GetHealthCondition(Core.GetString(n, "name")));
                foreach (string s in value.GetValues("quirk"))
                    AddQuirk(s);
                QuirkLevel = Core.GetInt(value, "quirkLevel");
                Trait = value.GetValue("trait");
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
