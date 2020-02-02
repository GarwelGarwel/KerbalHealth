using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalHealth
{
    /// <summary>
    /// Provides general static methods and fields for KerbalHealth
    /// </summary>
    public static class Core
    {
        public static bool Loaded = false;

        /// <summary>
        /// List of all tracked kerbals
        /// </summary>
        public static KerbalHealthList KerbalHealthList { get; set; } = new KerbalHealthList();

        static List<HealthFactor> factors = new List<HealthFactor>() {
            new StressFactor(),
            new ConfinementFactor(),
            new LonelinessFactor(),
            new MicrogravityFactor(),
            new EVAFactor(),
            new ConditionsFactor(),
            new ConnectedFactor(),
            new HomeFactor(),
            new KSCFactor()
        };

        /// <summary>
        /// List of all factors to be checked
        /// </summary>
        public static List<HealthFactor> Factors
        {
            get => factors;
            set => factors = value;
        }

        /// <summary>
        /// Returns factor with a given id
        /// </summary>
        /// <param name="id">Factor id</param>
        /// <returns></returns>
        public static HealthFactor GetHealthFactor(string id)
        {
            foreach (HealthFactor f in Factors) if (f.Name == id) return f;
            return null;
        }

        /// <summary>
        /// List sof all possible health conditions
        /// </summary>
        public static Dictionary<string, HealthCondition> HealthConditions;

        public static HealthCondition GetHealthCondition(string s) => HealthConditions.ContainsKey(s) ? HealthConditions[s] : null;

        /// <summary>
        /// Keeps data about all resources that provide Shielding. Key is resource id, value is amount of shielding provided by 1 unit
        /// </summary>
        public static Dictionary<int, double> ResourceShielding { get; set; } = new Dictionary<int, double>();

        public static void AddResourceShielding(string name, double shieldingPerTon)
        {
            PartResourceDefinition prd = PartResourceLibrary.Instance?.GetDefinition(name);
            if (prd == null)
            {
                Log("Can't find ResourceDefinition for " + name + ".", LogLevel.Important);
                return;
            }
            ResourceShielding.Add(prd.id, shieldingPerTon * prd.density);
        }

        public static List<Quirk> Quirks { get; set; } = new List<Quirk>();

        public static Quirk GetQuirk(string name)
        {
            foreach (Quirk q in Quirks)
                if (string.Compare(name, q.Name, true) == 0) return q;
            return null;
        }

        public static Dictionary<CelestialBody, PlanetHealthConfig> PlanetConfigs { get; set; }

        public static PlanetHealthConfig GetPlanetConfig(string name)
        {
            CelestialBody cb = FlightGlobals.GetBodyByName(name);
            if ((cb == null) || !PlanetConfigs.ContainsKey(cb)) return null;
            return PlanetConfigs[cb];
        }

        public static List<RadStormType> RadStormTypes { get; set; }
        static double radStormTypesTotalWeight = 0;

        public static RadStormType GetRandomRadStormType()
        {
            double d = Core.rand.NextDouble() * radStormTypesTotalWeight;
            foreach (RadStormType rst in RadStormTypes)
            {
                d -= rst.Weight;
                if (d < 0) return rst;
            }
            return null;
        }

        public static double SolarCycleDuration { get; set; }
        public static double SolarCycleStartingPhase { get; set; }
        public static double RadStormMinChancePerDay { get; set; }
        public static double RadStormMaxChancePerDay { get; set; }
        public static double SolarCyclePhase => (SolarCycleStartingPhase + Planetarium.GetUniversalTime() / SolarCycleDuration) % 1;
        public static double RadStormChance => RadStormMinChancePerDay + (RadStormMaxChancePerDay - RadStormMinChancePerDay) * (Math.Sin(2 * Math.PI * (SolarCyclePhase + 0.75)) + 1) / 2;

        /// <summary>
        /// Loads necessary mod data from KerbalHealth.cfg and 
        /// </summary>
        public static void LoadConfig()
        {
            Log("Loading config...", LogLevel.Important);

            ConfigNode config = GameDatabase.Instance.GetConfigNodes("KERBALHEALTH_CONFIG")[0];

            HealthConditions = new Dictionary<string, HealthCondition>();
            foreach (ConfigNode n in  config.GetNodes("HEALTH_CONDITION"))
                HealthConditions.Add(n.GetValue("name"), new HealthCondition(n));
            Core.Log(HealthConditions.Count + " health conditions loaded:");
            foreach (HealthCondition hc in HealthConditions.Values)
                Core.Log(hc.ToString());

            ResourceShielding = new Dictionary<int, double>();
            foreach (ConfigNode n in config.GetNodes("RESOURCE_SHIELDING"))
                AddResourceShielding(n.GetValue("name"), GetDouble(n, "shielding"));
            Log(ResourceShielding.Count + " resource shielding values loaded.", LogLevel.Important);

            Quirks = new List<Quirk>();
            foreach (ConfigNode n in config.GetNodes("HEALTH_QUIRK"))
                Quirks.Add(new Quirk(n));
            Core.Log(Quirks.Count + " quirks loaded.", LogLevel.Important);

            PlanetConfigs = new Dictionary<CelestialBody, PlanetHealthConfig>(FlightGlobals.Bodies.Count);
            foreach (CelestialBody b in FlightGlobals.Bodies) PlanetConfigs.Add(b, new PlanetHealthConfig(b));
            int i = 0;
            foreach (ConfigNode n in config.GetNodes("PLANET_HEALTH_CONFIG"))
            {
                PlanetHealthConfig bc = GetPlanetConfig(GetString(n, "name"));
                if (bc != null)
                {
                    bc.ConfigNode = n;
                    i++;
                }
            }
            Core.Log(i + " planet configs out of " + PlanetConfigs.Count + " bodies loaded.", LogLevel.Important);

            SolarCycleDuration = GetDouble(config, "solarCycleDuration", 11) * KSPUtil.dateTimeFormatter.Year;
            SolarCycleStartingPhase = GetDouble(config, "solarCycleStartingPhase");
            RadStormMinChancePerDay = GetDouble(config, "radStormMinChance", 0.00015);
            RadStormMaxChancePerDay = GetDouble(config, "radStormMaxChance", 0.00229);

            RadStormTypes = new List<RadStormType>();
            i = 0;
            foreach (ConfigNode n in config.GetNodes("RADSTORM_TYPE"))
            {
                RadStormTypes.Add(new RadStormType(n));
                radStormTypesTotalWeight += RadStormTypes[i++].Weight;
            }
            Core.Log(i + " radstorm types loaded with total weight " + radStormTypesTotalWeight, LogLevel.Important);

            trainingCaps = new List<double>(3) { 0.6, 0.75, 0.85 };
            foreach (ConfigNode n in config.GetNodes("TRAINING_CAPS"))
            {
                int j = Core.GetInt(n, "level");
                if (j == 0) continue;
                trainingCaps[j - 1] = Core.GetDouble(n, "cap");
            }

            Loaded = true;
        }

        #region SETTINGS

        /// <summary>
        /// Is Kerbal Health is enabled via Settings menu?
        /// </summary>
        public static bool ModEnabled
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().modEnabled;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().modEnabled = value;
        }

        /// <summary>
        /// Use Blizzy's Toolbar mod instead of stock app launcher
        /// </summary>
        public static bool ShowAppLauncherButton
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ShowAppLauncherButton;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ShowAppLauncherButton = value;
        }

        /// <summary>
        /// Number of game seconds between updates
        /// </summary>
        public static float UpdateInterval
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UpdateInterval;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UpdateInterval = value;
        }

        /// <summary>
        /// Minimum number of real-world seconds between updates (used in high timewarp)
        /// </summary>
        public static float MinUpdateInterval
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().MinUpdateInterval;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().MinUpdateInterval = value;
        }

        /// <summary>
        /// Base amount of health points (for level 0 kerbal)
        /// </summary>
        public static float BaseMaxHP
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().BaseMaxHP;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().BaseMaxHP = value;
        }

        /// <summary>
        /// HP increase per kerbal level
        /// </summary>
        public static float HPPerLevel
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().HPPerLevel;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().HPPerLevel = value;
        }

        /// <summary>
        /// Health level when a low health alert is shown
        /// </summary>
        public static float LowHealthAlert
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().LowHealthAlert;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().LowHealthAlert = value;
        }

        /// <summary>
        /// Will kerbals die upon reaching negative health?
        /// </summary>
        public static bool DeathEnabled
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().DeathEnabled;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().DeathEnabled = value;
        }

        /// <summary>
        /// Health % when the kerbal becomes exhausted (i.e. a Tourist). Must be <= <see cref="ExhaustionEndHealth"/>.
        /// </summary>
        public static float ExhaustionStartHealth
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionStartHealth;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionStartHealth = value;
        }

        /// <summary>
        /// Health % when the kerbal leaves exhausted state (i.e. becomes Crew again). Must be >= <see cref="ExhaustionStartHealth"/>.
        /// </summary>
        public static float ExhaustionEndHealth
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionEndHealth;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionEndHealth = value;
        }

        /// <summary>
        /// Kerbals must train for vessels/parts
        /// </summary>
        public static bool TrainingEnabled
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().TrainingEnabled;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().TrainingEnabled = value;
        }

        /// <summary>
        /// Random events can happen
        /// </summary>
        public static bool ConditionsEnabled
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().ConditionsEnabled;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().ConditionsEnabled = value;
        }

        /// <summary>
        /// Relative effect of conditions on health
        /// </summary>
        public static float ConditionsEffect
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().ConditionsEffect;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().ConditionsEffect = value;
        }

        /// <summary>
        /// Quirks can be awarded to kerbals and affect their health stats
        /// </summary>
        public static bool QuirksEnabled
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().QuirksEnabled;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().QuirksEnabled = value;
        }

        /// <summary>
        /// Maximum number of quirks for a kerbal
        /// </summary>
        public static int MaxQuirks
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().MaxQuirks;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().MaxQuirks = value;
        }

        /// <summary>
        /// Chance of a kerbal being awarded a quirk when he/she levels up
        /// </summary>
        public static float QuirkChance
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().QuirkChance;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().QuirkChance = value;
        }

        /// <summary>
        /// Quirks are only awarded when kerbals level up at KSC, not during missions
        /// </summary>
        public static bool AwardQuirksOnMissions
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().AwardQuirksOnMissions;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().AwardQuirksOnMissions = value;
        }

        /// <summary>
        /// Chances of getting some quirks depend on Courage and Stupidity of the kerbal
        /// </summary>
        public static bool StatsAffectQuirkWeights
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().StatsAffectQuirkWeights;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().StatsAffectQuirkWeights = value;
        }

        /// <summary>
        /// Whether to run radiation-related parts of the code
        /// </summary>
        public static bool RadiationEnabled
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().RadiationEnabled;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().RadiationEnabled = value;
        }

        /// <summary>
        /// Percentage of max health drained by 1e7 (10M) doses. 0 to disable effect
        /// </summary>
        public static float RadiationEffect
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().RadiationEffect;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().RadiationEffect = value;
        }

        /// <summary>
        /// Efficiency of radiation shielding provided by parts and resources
        /// </summary>
        public static float ShieldingEffect
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().ShieldingEffect;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().ShieldingEffect = value;
        }

        /// <summary>
        /// How much cosmic radiation reaches vessels in high planetary orbits and on moons
        /// </summary>
        public static float InSpaceHighCoefficient
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceHighCoefficient;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceHighCoefficient = value;
        }

        /// <summary>
        /// How much cosmic radiation reaches vessels in low planetary orbits
        /// </summary>
        public static float InSpaceLowCoefficient
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceLowCoefficient;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceLowCoefficient = value;
        }

        /// <summary>
        /// How much cosmic radiation reaches outer layers of the atmosphere
        /// </summary>
        public static float StratosphereCoefficient
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().StratoCoefficient;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().StratoCoefficient = value;
        }

        /// <summary>
        /// How much cosmic radiation reaches the ground and lower layers of the atmosphere
        /// </summary>
        public static float TroposphereCoefficient
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().TroposphereCoefficient;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().TroposphereCoefficient = value;
        }

        /// <summary>
        /// How much more radiaiton kerbals receive when on EVA
        /// </summary>
        public static float EVAExposure
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().EVAExposure;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().EVAExposure = value;
        }

        /// <summary>
        /// Solar radiation in interplanetary space at 1 AU, banana doses/day
        /// </summary>
        public static float SolarRadiation
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().SolarRadiation;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().SolarRadiation = value;
        }

        /// <summary>
        /// Galactic cosmic radiation in interplanetary space, banana doses/day
        /// </summary>
        public static float GalacticRadiation
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().GalacticRadiation;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().GalacticRadiation = value;
        }

        /// <summary>
        /// How much radiation dose is removed per day during decontamination
        /// </summary>
        public static float DecontaminationRate
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationRate;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationRate = value;
        }

        /// <summary>
        /// How much Health is lost while decontamination process takes place
        /// </summary>
        public static float DecontaminationHealthLoss
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationHealthLoss;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationHealthLoss = value;
        }

        /// <summary>
        /// How much Funds each decontamination procedure costs (Career only)
        /// </summary>
        public static float DecontaminationFundsCost
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationFundsCost;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationFundsCost = value;
        }

        /// <summary>
        /// How much Science each decontamination procedure costs (Career & Science modes)
        /// </summary>
        public static float DecontaminationScienceCost
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationScienceCost;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationScienceCost = value;
        }

        /// <summary>
        /// Min level of the Astronaut Complex for Decontamination
        /// </summary>
        public static int DecontaminationAstronautComplexLevel
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationAstronautComplexLevel;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationAstronautComplexLevel = value;
        }

        /// <summary>
        /// Min level of the R&D Facility for Decontamination
        /// </summary>
        public static int DecontaminationRNDLevel
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationRNDLevel;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().DecontaminationRNDLevel = value;
        }

        #endregion
        /// <summary>
        /// True if the current scene is Editor (VAB or SPH)
        /// </summary>
        public static bool IsInEditor => HighLogic.LoadedSceneIsEditor;

        /// <summary>
        /// Returns number of current crew in a vessel the kerbal is in or in the currently constructed vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static int GetCrewCount(ProtoCrewMember pcm) => IsInEditor ? ShipConstruction.ShipManifest.CrewCount : (IsKerbalLoaded(pcm) ? KerbalVessel(pcm).GetCrewCount() : 1);

        /// <summary>
        /// Returns number of maximum crew in a vessel the kerbal is in or in the currently constructed vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static int GetCrewCapacity(ProtoCrewMember pcm) => IsInEditor ? ShipConstruction.ShipManifest.GetAllCrew(true).Count : (IsKerbalLoaded(pcm) ? Math.Max(KerbalVessel(pcm).GetCrewCapacity(), 1) : 1);

        /// <summary>
        /// Returns Part where ProtoCrewMember is currently located or null if none
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static Part GetCrewPart(ProtoCrewMember pcm) => IsInEditor ? KSPUtil.GetPartByCraftID(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.GetPartForCrew(pcm).PartID) : pcm?.seat?.part;

        /// <summary>
        /// Returns true if the kerbal is in a loaded vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static bool IsKerbalLoaded(ProtoCrewMember pcm) => KerbalVessel(pcm)?.loaded ?? false;

        /// <summary>
        /// Returns true if kerbal exists and is either assigned or available
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static bool IsKerbalTrackable(ProtoCrewMember pcm) => (pcm != null) && ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available) || (pcm.rosterStatus == (ProtoCrewMember.RosterStatus﻿)9001));

        static Dictionary<string, Vessel> kerbalVesselsCache = new Dictionary<string, Vessel>();

        /// <summary>
        /// Clears kerbal vessels cache, to be called on every list update or when necessary
        /// </summary>
        public static void ClearCache()
        {
            kerbalVesselsCache.Clear();
            HealthModifierSet.VesselCache.Clear();
        }

        /// <summary>
        /// Returns <see cref="Vessel"/> the kerbal is in or null if the kerbal is not assigned
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static Vessel KerbalVessel(ProtoCrewMember pcm)
        {
            if (pcm == null) return null;
            if (DFWrapper.InstanceExists && DFWrapper.DeepFreezeAPI.FrozenKerbals.ContainsKey(pcm.name))
            {
                Vessel v = FlightGlobals.FindVessel(DFWrapper.DeepFreezeAPI.FrozenKerbals[pcm.name].vesselID);
                Log(pcm.name + " found in FrozenKerbals.");
                kerbalVesselsCache.Add(pcm.name, v);
                return v;
            }
            if (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned) return null;
            if (kerbalVesselsCache.ContainsKey(pcm.name)) return kerbalVesselsCache[pcm.name];
            foreach (Vessel v in FlightGlobals.Vessels)
                foreach (ProtoCrewMember k in v.GetVesselCrew())
                    if (k == pcm)
                    {
                        kerbalVesselsCache.Add(pcm.name, v);
                        return v;
                    }
            Log(pcm.name + " is " + pcm.rosterStatus + " and was not found in any of the " + FlightGlobals.Vessels.Count + " vessels!", LogLevel.Important);
            return null;
        }

        public static double DistanceToSun(Vessel v) =>
            (v.mainBody == Sun.Instance.sun)
            ? v.altitude + Sun.Instance.sun.Radius
            : ((v.distanceToSun > 0) ? v.distanceToSun : Core.GetPlanet(v.mainBody).orbit.altitude + Sun.Instance.sun.Radius);

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
                if (amount != 0) Core.Log("Part " + p.name + " contains " + amount + " / " + maxAmount + " of shielding resource " + res.Key);
                s += res.Value * amount;
            }
            return s;
        }

        /// <summary>
        /// Returns exposure provided by shielding
        /// </summary>
        /// <param name="shielding">Total shielding</param>
        /// <param name="crew">Crew capacity</param>
        /// <returns></returns>
        public static double GetExposure(double shielding, double crew) => Math.Pow(2, -shielding * Core.ShieldingEffect / Math.Pow(crew, 2f / 3));

        /// <summary>
        /// Returns radiation exposure in the specific part
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public static double GetPartExposure(Part part)
        {
            if (part.CrewCapacity == 0) return 1;
            double s = 0;
            List<Part> parts = new List<Part>() { part };
            if (part.parent != null) parts.Add(part.parent);
            parts.AddRange(part.children);
            List<ModuleKerbalHealth> modules = new List<ModuleKerbalHealth>();
            foreach (Part p in parts)
                if ((p.CrewCapacity == 0) || (p == part))
                {
                    modules.AddRange(p.FindModulesImplementing<ModuleKerbalHealth>());
                    s += Core.GetResourceShielding(p);
                }
            foreach (ModuleKerbalHealth m in modules)
                if (m.IsModuleActive) s += m.shielding;
            return Core.GetExposure(s, part.CrewCapacity);
        }

        class PartExposure : IComparable<PartExposure>
        {
            public Part Part { get; set; }
            public double Exposure { get; private set; }

            public int CompareTo(PartExposure other) => Exposure.CompareTo(other.Exposure);

            public PartExposure(Part p)
            {
                Part = p;
                Exposure = Core.GetPartExposure(p);
            }
        }

        public static double GetShelterExposure(List<Part> parts, int crew)
        {
            List<PartExposure> exposures = new List<PartExposure>();
            int crewCap = 0;
            foreach (Part p in parts)
                if (p.CrewCapacity > 0)
                {
                    Core.Log("Possible shelter part: " + p.name + " with exposure " + Core.GetPartExposure(p).ToString("P1"), LogLevel.Important);
                    exposures.Add(new PartExposure(p));
                    crewCap += p.CrewCapacity;
                }
            exposures.Sort();
            double x = 0;
            int i = 0;
            for (int c = 0; i < exposures.Count; i++)
            {
                Core.Log("Part " + exposures[i].Part.name + " with exposure " + exposures[i].Exposure.ToString("P1") + " and crew cap " + exposures[i].Part.CrewCapacity, LogLevel.Important);
                x += exposures[i].Exposure * Math.Min(exposures[i].Part.CrewCapacity, crew - c);
                c += exposures[i].Part.CrewCapacity;
                if (c >= crew)
                {
                    Core.Log("These parts are enough.", LogLevel.Important);
                    break;
                }
            }
            Core.Log("Average exposure in top " + (i + 1) + " parts is " + (x / crew).ToString("P1"), LogLevel.Important);
            HealthModifierSet hms = new HealthModifierSet(parts);
            Core.Log("Vessel overall exposure is " + hms.GetExposure(crewCap).ToString("P1"), LogLevel.Important);
            return Math.Min(x / crew, hms.GetExposure(crewCap));
        }

        static List<double> trainingCaps;

        /// <summary>
        /// Max amount of stress reduced by training depending on Astronaut Complex's level
        /// </summary>
        public static double TrainingCap => trainingCaps[(int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) * 2)];

        /// <summary>
        /// Returns list of IDs of parts that are used in training and stress calculations
        /// </summary>
        /// <param name="allParts"></param>
        /// <returns></returns>
        public static List<ModuleKerbalHealth> GetTrainingCapableParts(List<Part> allParts)
        {
            List<ModuleKerbalHealth> res = new List<ModuleKerbalHealth>();
            foreach (Part p in allParts)
            {
                List<ModuleKerbalHealth> modules = p.FindModulesImplementing<ModuleKerbalHealth>();
                foreach (ModuleKerbalHealth mkh in modules)
                    if (mkh.complexity != 0)
                    {
                        res.Add(mkh);
                        break;
                    }
            }
            return res;
        }

        public static bool IsPlanet(CelestialBody body) => body?.orbit?.referenceBody == Sun.Instance.sun;

        public static CelestialBody GetPlanet(CelestialBody body) => ((body == null) || IsPlanet(body)) ? body : GetPlanet(body?.orbit?.referenceBody);

        public static string GetString(ConfigNode n, string key, string defaultValue = null) => n.HasValue(key) ? n.GetValue(key) : defaultValue;

        public static double GetDouble(ConfigNode n, string key, double defaultValue = 0)
        {
            double res;
            try {
                res = Double.Parse(n.GetValue(key));
                if (Double.IsNaN(res)) throw new Exception();
            }
            catch (Exception) { res = defaultValue; }
            return res;
        }

        public static int GetInt(ConfigNode n, string key, int defaultValue = 0)
        {
            int res;
            try { res = Int32.Parse(n.GetValue(key)); }
            catch (Exception) { res = defaultValue; }
            return res;
        }

        public static uint GetUInt(ConfigNode n, string key, uint defaultValue = 0)
        {
            uint res;
            try { res = UInt32.Parse(n.GetValue(key)); }
            catch (Exception) { res = defaultValue; }
            return res;
        }

        public static bool GetBool(ConfigNode n, string key, bool defaultValue = false)
        {
            bool res;
            try { res = Boolean.Parse(n.GetValue(key)); }
            catch (Exception) { res = defaultValue; }
            return res;
        }

        /// <summary>
        /// Returns x*x
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static double Sqr(double x) => x * x;

        /// <summary>
        /// Returns a Gaussian-distributed random value
        /// </summary>
        /// <param name="mean"></param>
        /// <param name="stdDev"></param>
        /// <returns></returns>
        public static double GetGaussian(double stdDev = 1, double mean = 0) => mean + stdDev * Math.Sqrt(-2 * Math.Log(1 - rand.NextDouble())) * Math.Sin(2 * Math.PI * (1 - rand.NextDouble()));

        /// <summary>
        /// Returns a string of a value with a mandatory sign (+ or -, unless v = 0)
        /// </summary>
        /// <param name="value">Value to present as a string</param>
        /// <param name="format">String format according to Double.ToString</param>
        /// <returns></returns>
        public static string SignValue(double value, string format) => ((value > 0) ? "+" : "") + value.ToString(format);

        static string[] prefixes = { "", "K", "M", "G", "T" };

        /// <summary>
        /// Converts a number into a string with a multiplicative character (K, M, G or T), if applicable
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="digits">Number of digits to allow before the prefix (must be 3 or more)</param>
        /// <returns></returns>
        public static string PrefixFormat(double value, int digits = 3, bool mandatorySign = false)
        {
            double v = Math.Abs(Math.Round(value));
            if (v < 0.5) return "0";
            if (digits < 3) digits = 3;
            int n, m = (int)Math.Pow(10, digits);
            for (n = 0; (v >= m) && (n < prefixes.Length - 1); n++)
                v /= 1000;
            return (value < 0 ? "-" : (mandatorySign ? "+" : "")) + v.ToString("F" + (digits - Math.Truncate(Math.Log10(v)) - 1)) + prefixes[n];
        }

        /// <summary>
        /// Returns the number of occurences of a character in a string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int CountChars(string s, char c)
        {
            int res = 0;
            foreach (char ch in s) if (ch == c) res++;
            return res;
        }

        /// <summary>
        /// Parses UT into a string (e.g. "2 d 3 h 15 m 59 s"), hides zero elements
        /// </summary>
        /// <param name="time">Time in seconds</param>
        /// <param name="showSeconds">If false, seconds will be displayed only if time is less than 1 minute; otherwise always</param>
        /// <param name="daysTimeLimit">If time is longer than this number of days, time value will be skipped; -1 to alwys show time</param>
        /// <returns></returns>
        public static string ParseUT(double time, bool showSeconds = true, int daysTimeLimit = -1)
        {
            if (Double.IsNaN(time) || (time == 0)) return "—";
            if (time > KSPUtil.dateTimeFormatter.Year * 10) return "10y+";
            double t = time;
            int y, d, m, h;
            string res = "";
            bool show0 = false;
            if (t >= KSPUtil.dateTimeFormatter.Year)
            {
                y = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Year);
                t -= y * KSPUtil.dateTimeFormatter.Year;
                res += y + " y ";
                show0 = true;
            }
            if ((t >= KSPUtil.dateTimeFormatter.Day) || (show0 && (t >= 1)))
            {
                d = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Day);
                t -= d * KSPUtil.dateTimeFormatter.Day;
                res += d + " d ";
                show0 = true;
            }
            if ((daysTimeLimit == -1) || (time < KSPUtil.dateTimeFormatter.Day * daysTimeLimit))
            {
                if ((t >= 3600) || show0)
                {
                    h = (int)Math.Floor(t / 3600);
                    t -= h * 3600;
                    res += h + " h ";
                    show0 = true;
                }
                if ((t >= 60) || show0)
                {
                    m = (int)Math.Floor(t / 60);
                    t -= m * 60;
                    res += m + " m ";
                }
                if ((time < 60) || (showSeconds && (Math.Floor(t) > 0))) res += t.ToString("F0") + " s";
            }
            else if (time < KSPUtil.dateTimeFormatter.Day) res = "0 d";
            return res.TrimEnd();
        }

        public static void ShowMessage(string msg, bool unwarpTime)
        {
            KSP.UI.Screens.MessageSystem.Instance.AddMessage(new KSP.UI.Screens.MessageSystem.Message("Kerbal Health", KSPUtil.PrintDateCompact(Planetarium.GetUniversalTime(), true) + ": " + msg, KSP.UI.Screens.MessageSystemButton.MessageButtonColor.RED, KSP.UI.Screens.MessageSystemButton.ButtonIcons.ALERT));
            if (unwarpTime) TimeWarp.SetRate(0, false, true);
        }

        public static void ShowMessage(string msg, ProtoCrewMember pcm)
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().KSCNotificationsEnabled && ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available) || (pcm.rosterStatus == (ProtoCrewMember.RosterStatus﻿)9001))) return;
            ShowMessage(msg, pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned);
        }

        /// <summary>
        /// Mod-wide random number generator
        /// </summary>
        public static System.Random rand = new System.Random();

        /// <summary>
        /// Log levels:
        /// <list type="bullet">
        /// <item><definition>None: do not log</definition></item>
        /// <item><definition>Error: log only errors</definition></item>
        /// <item><definition>Important: log only errors and important information</definition></item>
        /// <item><definition>Debug: log all information</definition></item>
        /// </list>
        /// </summary>
        public enum LogLevel { None, Error, Important, Debug };

        /// <summary>
        /// Current <see cref="LogLevel"/>: either Debug or Important
        /// </summary>
        public static LogLevel Level => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().DebugMode ? LogLevel.Debug : LogLevel.Important;

        /// <summary>
        /// Returns true if current logging allows logging of messages at messageLevel
        /// </summary>
        /// <param name="messageLevel"></param>
        /// <returns></returns>
        public static bool IsLogging(LogLevel messageLevel = LogLevel.Debug) => messageLevel <= Level;

        /// <summary>
        /// Write into output_log.txt
        /// </summary>
        /// <param name="message">Text to log</param>
        /// <param name="messageLevel"><see cref="LogLevel"/> of the entry</param>
        public static void Log(string message, LogLevel messageLevel = LogLevel.Debug)
        { if (IsLogging(messageLevel) && (message != "")) Debug.Log("[KerbalHealth] " + (messageLevel == LogLevel.Error ? "ERROR: " : "") + message); }
    }
}
