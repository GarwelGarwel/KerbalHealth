using KSP.Localization;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalHealth
{
    /// <summary>
    /// Log levels:
    /// <list type="bullet">
    /// <item><definition>None: do not log</definition></item>
    /// <item><definition>Error: log only errors</definition></item>
    /// <item><definition>Important: log only errors and important information</definition></item>
    /// <item><definition>Debug: log all information</definition></item>
    /// </list>
    /// </summary>
    public enum LogLevel
    {
        None = 0,
        Error,
        Important,
        Debug
    };

    /// <summary>
    /// Provides general static methods and fields for KerbalHealth
    /// </summary>
    public static class Core
    {
        // Used by DeepFreeze
        public const ProtoCrewMember.RosterStatus﻿ Status_Frozen = (ProtoCrewMember.RosterStatus﻿)9001;

        public static bool ConfigLoaded = false;

        /// <summary>
        /// List of all possible health conditions
        /// </summary>
        public static Dictionary<string, HealthCondition> HealthConditions;

        //static readonly string[] prefixes = { "", "K", "M", "G", "T" };
        static readonly string prefixes = "KMGT";

        static Dictionary<string, Vessel> kerbalVesselsCache = new Dictionary<string, Vessel>();

        /// <summary>
        /// List of all tracked kerbals
        /// </summary>
        public static KerbalHealthList KerbalHealthList { get; set; } = new KerbalHealthList();

        /// <summary>
        /// List of all factors to be checked
        /// </summary>
        public static List<HealthFactor> Factors { get; set; } = new List<HealthFactor>()
        {
            new StressFactor(),
            new ConfinementFactor(),
            new MicrogravityFactor(),
            new LonelinessFactor(),
            new IsolationFactor(),
            new EVAFactor(),
            new HomeFactor(),
            new KSCFactor(),
            new ConditionsFactor()
        };

        /// <summary>
        /// Keeps data about all resources that provide Shielding. Key is resource id, value is amount of shielding provided by 1 unit
        /// </summary>
        public static Dictionary<int, double> ShieldingResources { get; set; } = new Dictionary<int, double>();

        /// <summary>
        /// List of all possible quirks
        /// </summary>
        public static List<Quirk> Quirks { get; set; } = new List<Quirk>();

        public static Dictionary<CelestialBody, PlanetHealthConfig> PlanetConfigs { get; set; }

        /// <summary>
        /// True if the current scene is Editor (VAB or SPH)
        /// </summary>
        public static bool IsInEditor => HighLogic.LoadedSceneIsEditor;

        /// <summary>
        /// Returns factor with a given id
        /// </summary>
        /// <param name="id">Factor id</param>
        /// <returns></returns>
        public static HealthFactor GetHealthFactor(string id) => Factors.Find(f => f.Name == id);

        public static HealthCondition GetHealthCondition(string s) => HealthConditions.TryGetValue(s, out HealthCondition value) ? value : null;

        public static void AddResourceShielding(string name, double shieldingPerTon)
        {
            PartResourceDefinition prd = PartResourceLibrary.Instance?.GetDefinition(name);
            if (prd != null)
                ShieldingResources.Add(prd.id, shieldingPerTon * prd.density);
            else Log($"Can't find ResourceDefinition for {name}.");
        }

        public static Quirk GetQuirk(string name) => Quirks.Find(q => name == q.Name);

        public static PlanetHealthConfig GetPlanetConfig(string name)
        {
            CelestialBody cb = FlightGlobals.GetBodyByName(name);
            return cb != null && PlanetConfigs.TryGetValue(cb, out PlanetHealthConfig res) ? res : null;
        }

        public static float GetInternalFacilityLevel(int displayFacilityLevel) => (float)(displayFacilityLevel - 1) / 2;

        #region RAD STORMS

        static float radStormTypesTotalWeight = 0;

        public static List<RadStormType> RadStormTypes { get; set; }

        public static float SolarCycleDuration { get; set; }

        public static float SolarCycleStartingPhase { get; set; }

        public static float RadStormMinMBTE { get; set; }

        public static float RadStormMaxMBTE { get; set; }

        public static float SolarCyclePhase => (int)(SolarCycleStartingPhase + Planetarium.GetUniversalTime() / SolarCycleDuration) % 1;

        public static float RadStormMTBE => RadStormMinMBTE + (RadStormMaxMBTE - RadStormMinMBTE) * (float)(Math.Sin(2 * Math.PI * (SolarCyclePhase + 0.75)) + 1) / 2;

        public static RadStormType GetRandomRadStormType()
        {
            double d = Rand.NextDouble() * radStormTypesTotalWeight;
            foreach (RadStormType rst in RadStormTypes)
            {
                d -= rst.Weight;
                if (d < 0)
                    return rst;
            }
            return null;
        }

        #endregion

        #region CREW UTILITIES

        public static IList<ProtoCrewMember> GetCrew(ProtoCrewMember pcm, bool entireVessel)
        {
            Vessel vessel = pcm.GetVessel();
            if (!entireVessel && CLS.Enabled && pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned)
                return pcm.GetCLSSpace(vessel).GetCrew().ToList();
            if (IsInEditor)
                return ShipConstruction.ShipManifest.GetAllCrew(false);
            return vessel != null ? vessel.GetVesselCrew() : new List<ProtoCrewMember>();
        }

        /// <summary>
        /// Returns number of current crew in a vessel (or CLS space) the kerbal is in or in the currently constructed vessel
        /// </summary>
        public static int GetCrewCount(ProtoCrewMember pcm, bool entireVessel)
        {
            Vessel vessel = pcm.GetVessel();
            if (!entireVessel && CLS.Enabled)
                return pcm.GetCLSSpace(vessel).GetCrewCount();
            if (IsInEditor)
                return ShipConstruction.ShipManifest.CrewCount;
            return vessel != null ? vessel.GetCrewCount() : 1;
        }

        public static int GetColleaguesCount(ProtoCrewMember pcm) =>
            pcm.type != ProtoCrewMember.KerbalType.Tourist
            ? Math.Max(GetCrew(pcm, true).Count(pcm2 => pcm.trait == pcm2.trait), 1)
            : (1 + GetCrew(pcm, true).Count(pcm2 => pcm2.type != ProtoCrewMember.KerbalType.Tourist));

        /// <summary>
        /// Returns Part where ProtoCrewMember is currently located or null if none
        /// </summary>
        public static Part GetCrewPart(this ProtoCrewMember pcm) =>
            IsInEditor ? KSPUtil.GetPartByCraftID(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.GetPartForCrew(pcm).PartID) : pcm?.seat?.part;

        #endregion

        #region VESSEL UTILITIES

        public static string GetPartTitle(string partName) =>
            partName.StartsWith("kerbalEVA") ? Localizer.Format("#KH_SpacesuitPart", partName) : (PartLoader.getPartInfoByName(partName)?.title ?? partName);

        /// <summary>
        /// Returns true if the kerbal is in a loaded vessel
        /// </summary>
        public static bool IsUnpacked(this ProtoCrewMember pcm)
        {
            Vessel vessel = pcm.GetVessel();
            return vessel != null && vessel.loaded && !vessel.packed;
        }

        /// <summary>
        /// Returns true if kerbal exists and is either assigned or available
        /// </summary>
        public static bool IsTrackable(this ProtoCrewMember pcm) =>
            pcm != null
            && (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned
            || pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available
            || pcm.rosterStatus == Status_Frozen);

        /// <summary>
        /// Returns true if the vessel has a KSC connection either with CommNet or RemoteTech
        /// </summary>
        public static bool IsConnectedHome(this Vessel v) =>
            (v.Connection != null && v.Connection.IsConnectedHome) || (RemoteTech.Installed && v.IsConnectedWithRemoteTech());

        /// <summary>
        /// Clears kerbal vessels cache, to be called on every list update or when necessary
        /// </summary>
        public static void ClearVesselsCache() => kerbalVesselsCache.Clear();

        /// <summary>
        /// Returns <see cref="Vessel"/> the kerbal is in or null if the kerbal is not assigned
        /// </summary>
        public static Vessel GetVessel(this ProtoCrewMember pcm)
        {
            if (pcm == null || (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned && pcm.rosterStatus != Status_Frozen))
                return null;

            if (kerbalVesselsCache.TryGetValue(pcm.name, out Vessel vessel))
                return vessel;

            if (DFWrapper.InstanceExists && DFWrapper.DeepFreezeAPI.FrozenKerbals.TryGetValue(pcm.name, out DFWrapper.KerbalInfo kerbal))
            {
                vessel = FlightGlobals.FindVessel(kerbal.vesselID);
                Log($"{pcm.name} found frozen in {vessel?.vesselName ?? "NULL"}.");
                kerbalVesselsCache.Add(pcm.name, vessel);
                return vessel;
            }

            vessel = FlightGlobals.Vessels.Find(v => v.GetVesselCrew().Contains(pcm));
            if (vessel != null)
                kerbalVesselsCache.Add(pcm.name, vessel);
            else Log($"{pcm.name} is {pcm.rosterStatus} and was not found in any of the {FlightGlobals.Vessels.Count} vessels!", LogLevel.Important);
            return vessel;
        }

        #endregion

        #region CELESTIAL BODY UTILITIES

        public static bool IsPlanet(this CelestialBody body) => body?.orbit?.referenceBody == Sun.Instance.sun;

        public static CelestialBody GetPlanet(this CelestialBody body) => body == null || body.IsPlanet() ? body : body.orbit?.referenceBody?.GetPlanet();

        public static double GetDistanceToSun(this Vessel v) =>
            v.mainBody == Sun.Instance.sun
            ? v.altitude + Sun.Instance.sun.Radius
            : (v.distanceToSun > 0 ? v.distanceToSun : v.mainBody.GetPlanet().orbit.altitude + Sun.Instance.sun.Radius);

        public static float GetScienceMultiplier(this Vessel vessel, bool ignoreLandedOnHomebody = true)
        {
            CelestialBody body = vessel?.mainBody;
            if (body == null)
            {
                Log($"Could not get science multiplier for {vessel?.vesselName}.", LogLevel.Error);
                return 0;
            }
            if (ignoreLandedOnHomebody && body.isHomeWorld && vessel.LandedOrSplashed)
                return 0;
            if (vessel.Landed)
                return body.scienceValues.LandedDataValue;
            if (vessel.Splashed)
                return body.scienceValues.SplashedDataValue;
            if ((vessel.situation & Vessel.Situations.FLYING) != 0)
                return vessel.altitude < body.scienceValues.flyingAltitudeThreshold ? body.scienceValues.FlyingLowDataValue : body.scienceValues.FlyingHighDataValue;
            return vessel.altitude < body.scienceValues.InSpaceHighDataValue ? body.scienceValues.InSpaceLowDataValue : body.scienceValues.InSpaceHighDataValue;
        }

        #endregion

        #region TRAINING

        static List<float> trainingCaps;

        /// <summary>
        /// Max amount of stress reduced by training depending on Astronaut Complex's level
        /// </summary>
        public static float KSCTrainingCap => trainingCaps[(int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) * 2)];

        public const float InFlightTrainingCap = 1;

        /// <summary>
        /// Returns a list of unique part modules that are used in training & stress calculations
        /// </summary>
        public static List<ModuleKerbalHealth> GetTrainableModules(this IList<Part> allParts)
        {
            List<ModuleKerbalHealth> res = new List<ModuleKerbalHealth>();
            for (int i = 0; i < allParts.Count; i++)
            {
                List<ModuleKerbalHealth> modules = allParts[i].FindModulesImplementing<ModuleKerbalHealth>();
                for (int j = 0; j < modules.Count; j++)
                    if (modules[j].complexity != 0)
                    {
                        res.Add(modules[j]);
                        break;
                    }
            }
            return res;
        }

        public static bool AnyTrainableParts(IEnumerable<Part> allParts) => allParts.Any(part => part.FindModulesImplementing<ModuleKerbalHealth>().Any(mkh => mkh.complexity != 0));

        #endregion

        #region CONFIG NODE UTILITIES

        public static string GetString(this ConfigNode n, string key, string defaultValue = null)
        {
            string res = defaultValue;
            n.TryGetValue(key, ref res);
            return res;
        }

        public static float GetFloat(this ConfigNode n, string key, float defaultValue = 0) =>
           float.TryParse(n.GetValue(key), out float res) && !float.IsNaN(res) ? res : defaultValue;

        public static double GetDouble(this ConfigNode n, string key, double defaultValue = 0) =>
            double.TryParse(n.GetValue(key), out double res) && !double.IsNaN(res) ? res : defaultValue;

        public static int GetInt(this ConfigNode n, string key, int defaultValue = 0) =>
            int.TryParse(n.GetValue(key), out int res) ? res : defaultValue;

        public static uint GetUInt(this ConfigNode n, string key, uint defaultValue = 0) =>
            uint.TryParse(n.GetValue(key), out uint res) ? res : defaultValue;

        public static bool GetBool(this ConfigNode n, string key, bool defaultValue = false) =>
            bool.TryParse(n.GetValue(key), out bool res) ? res : defaultValue;

        #endregion

        #region MATH & RNG UTILITIES

        /// <summary>
        /// Mod-wide random number generator
        /// </summary>
        internal static System.Random Rand = new System.Random();

        /// <summary>
        /// Returns x*x
        /// </summary>
        public static double Sqr(double x) => x * x;

        /// <summary>
        /// Returns a Gaussian-distributed random value
        /// </summary>
        public static double GetGaussian(double stdDev = 1, double mean = 0) =>
            mean + stdDev * Math.Sqrt(-2 * Math.Log(1 - Rand.NextDouble())) * Math.Sin(2 * Math.PI * (1 - Rand.NextDouble()));

        public static double EventChance(double mtbe, double interval) => 1 - Math.Exp(-interval / mtbe);

        public static bool EventHappens(double mtbe, double interval) => (mtbe > 0 && Rand.NextDouble() < EventChance(mtbe, interval)) || mtbe == 0;

        #endregion

        #region NUMBERS AND STRINGS

        /// <summary>
        /// Returns a string of a value with a mandatory sign (+ or -, unless v = 0)
        /// </summary>
        /// <param name="value">Value to present as a string</param>
        /// <param name="format">String format according to Double.ToString</param>
        public static string SignValue(this double value, string format) => (value > 0 ? "+" : "") + value.ToString(format);

        /// <summary>
        /// Converts a number into a string with a multiplicative character (K, M, G or T), if applicable
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="digits">Number of digits to allow before the prefix (must be 3 or more)</param>
        public static string PrefixFormat(this double value, int digits = 3, bool mandatorySign = false)
        {
            double v = Math.Abs(value);
            if (v < 0.5)
                return "0";
            int n, m = (int)Math.Pow(10, digits);
            if (v < m)
                return mandatorySign ? value.SignValue($"g{digits}") : value.ToString($"g{digits}");
            for (n = -1; v >= m && n < 3; n++)
                v /= 1000;
            return (value < 0 ? "-" : (mandatorySign && value > 0 ? "+" : "")) + v.ToString($"g{digits}") + prefixes[n];
        }

        /// <summary>
        /// Returns a zero-based year in the given timestamp (add 1 for a KSP date year)
        /// </summary>
        public static int GetYear(double time) => (int)Math.Floor(time / KSPUtil.dateTimeFormatter.Year);

        /// <summary>
        /// Parses UT into a string (e.g. "2 d 3 h 15 m 59 s"), hides zero elements
        /// </summary>
        /// <param name="time">Time in seconds</param>
        /// <param name="showSeconds">If false, seconds will be displayed only if time is less than 1 minute; otherwise always</param>
        /// <param name="daysTimeLimit">If time is longer than this number of days, time value will be skipped; -1 to alwys show time</param>
        public static string TimeToString(double time, bool showSeconds = true, int daysTimeLimit = -1)
        {
            if (time <= 0 || double.IsNaN(time))
                return "—";
            if (time > KSPUtil.dateTimeFormatter.Year * 10)
                return ">10 y";
            double t = time;
            int y, d, m, h;
            bool show0 = false;
            StringBuilder res;

            if (t >= KSPUtil.dateTimeFormatter.Year)
            {
                y = GetYear(t);
                t -= y * KSPUtil.dateTimeFormatter.Year;
                res = new StringBuilder($"{y} y ");
                show0 = true;
            }
            else res = new StringBuilder();
            
            if (t >= KSPUtil.dateTimeFormatter.Day || (show0 && t >= 1))
            {
                d = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Day);
                t -= d * KSPUtil.dateTimeFormatter.Day;
                res.Append($"{d} d ");
                show0 = true;
            }
            
            if (daysTimeLimit == -1 || time < KSPUtil.dateTimeFormatter.Day * daysTimeLimit)
            {
                if (t >= 3600 || show0)
                {
                    h = (int)Math.Floor(t / 3600);
                    t -= h * 3600;
                    res.Append($"{h} h ");
                    show0 = true;
                }
                if (t >= 60 || show0)
                {
                    m = (int)Math.Floor(t / 60);
                    t -= m * 60;
                    res.Append($"{m} m ");
                }
                if (time < 60 || (showSeconds && t >= 1))
                    return res.Append($"{t:F0} s").ToStringAndRelease();
            }
            else if (time < KSPUtil.dateTimeFormatter.Day)
                return "0 d";
            
            return res.ToStringAndRelease().TrimEnd();
        }

        #endregion

        public static void ShowMessage(string msg, bool unwarpTime)
        {
            MessageSystem.Instance.AddMessage(new MessageSystem.Message(
                "Kerbal Health",
                $"{KSPUtil.PrintDateCompact(Planetarium.GetUniversalTime(), true)}: {msg}",
                MessageSystemButton.MessageButtonColor.RED,
                MessageSystemButton.ButtonIcons.ALERT));
                // Check the configuration option before setting the time warp rate to 0
            bool shouldUnwarp = HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UnwarpTimeEnabled;
            if (unwarpTime && shouldUnwarp)
            {
                TimeWarp.SetRate(0, false, true);
            }
        }

        public static void ShowMessage(string msg, ProtoCrewMember pcm)
        {
            if (KerbalHealthQuirkSettings.Instance.KSCNotificationsEnabled || (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Available && pcm.rosterStatus != Status_Frozen))
                ShowMessage(msg, pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned);
        }

        /// <summary>
        /// Loads necessary mod data from KerbalHealth.cfg
        /// </summary>
        public static void LoadConfig()
        {
            Log("Loading config...", LogLevel.Important);

            ConfigNode config = GameDatabase.Instance.GetConfigNodes("KERBALHEALTH_CONFIG")[0];

            HealthConditions = new Dictionary<string, HealthCondition>();
            foreach (ConfigNode n in config.GetNodes("HEALTH_CONDITION"))
                HealthConditions.Add(n.GetValue("name"), new HealthCondition(n));
            Log($"{HealthConditions.Count} health conditions loaded.", LogLevel.Important);

            ShieldingResources = new Dictionary<int, double>();
            foreach (ConfigNode n in config.GetNodes("RESOURCE_SHIELDING"))
                AddResourceShielding(n.GetValue("name"), n.GetDouble("shielding"));
            Log($"{ShieldingResources.Count} shielding resource values loaded.", LogLevel.Important);

            Quirks = new List<Quirk>(config.GetNodes("HEALTH_QUIRK").Select(n => new Quirk(n)));
            Log($"{Quirks.Count} quirks loaded.", LogLevel.Important);

            PlanetConfigs = new Dictionary<CelestialBody, PlanetHealthConfig>(FlightGlobals.Bodies.Count);
            foreach (CelestialBody b in FlightGlobals.Bodies)
                PlanetConfigs.Add(b, new PlanetHealthConfig(b));

            int i = 0;
            foreach (ConfigNode n in config.GetNodes("PLANET_HEALTH_CONFIG"))
                GetPlanetConfig(n.GetString("name"))?.Load(n);

            SolarCycleDuration = config.GetFloat("solarCycleDuration", 11) * KSPUtil.dateTimeFormatter.Year;
            SolarCycleStartingPhase = config.GetFloat("solarCycleStartingPhase");
            RadStormMinMBTE = config.GetFloat("radStormMinMBTE", 426);
            RadStormMaxMBTE = config.GetFloat("radStormMaxMBTE", 6390);

            RadStormTypes = new List<RadStormType>();
            i = 0;
            foreach (ConfigNode n in config.GetNodes("RADSTORM_TYPE"))
            {
                RadStormTypes.Add(new RadStormType(n));
                radStormTypesTotalWeight += RadStormTypes[i++].Weight;
            }
            Log($"{i} radstorm types loaded with total weight {radStormTypesTotalWeight}.", LogLevel.Important);

            trainingCaps = new List<float>(3)
            {
                0.30f,
                0.50f,
                0.60f
            };
            foreach (ConfigNode n in config.GetNodes("TRAINING_CAPS"))
            {
                int j = n.GetInt("level");
                if (j <= 0)
                    continue;
                trainingCaps[j - 1] = n.GetFloat("cap");
            }

            ConfigLoaded = true;
        }

        #region LOGGING

        /// <summary>
        /// Current <see cref="LogLevel"/>: either Debug or Important
        /// </summary>
        public static LogLevel Level => KerbalHealthGeneralSettings.Instance.DebugMode ? LogLevel.Debug : LogLevel.Important;

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
        internal static void Log(string message, LogLevel messageLevel = LogLevel.Debug)
        {
            if (IsLogging(messageLevel) && message.Length != 0)
            {
                if (messageLevel == LogLevel.Error)
                    message = $"ERROR: {message}";
                Debug.Log($"[KerbalHealth] {message}");
            }
        }

        #endregion
    }
}
