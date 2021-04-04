using System;
using System.Collections.Generic;
using System.Linq;
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
    public enum LogLevel { None = 0, Error, Important, Debug };

    /// <summary>
    /// Provides general static methods and fields for KerbalHealth
    /// </summary>
    public static class Core
    {
        public static bool ConfigLoaded = false;

        /// <summary>
        /// List of all possible health conditions
        /// </summary>
        public static Dictionary<string, HealthCondition> HealthConditions;

        /// <summary>
        /// Mod-wide random number generator
        /// </summary>
        internal static System.Random rand = new System.Random();

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

        static double radStormTypesTotalWeight = 0;

        static Dictionary<string, Vessel> kerbalVesselsCache = new Dictionary<string, Vessel>();

        static List<double> trainingCaps;

        static string[] prefixes = { "", "K", "M", "G", "T" };

        /// <summary>
        /// List of all tracked kerbals
        /// </summary>
        public static KerbalHealthList KerbalHealthList { get; set; } = new KerbalHealthList();

        /// <summary>
        /// List of all factors to be checked
        /// </summary>
        public static List<HealthFactor> Factors
        {
            get => factors;
            set => factors = value;
        }

        /// <summary>
        /// Keeps data about all resources that provide Shielding. Key is resource id, value is amount of shielding provided by 1 unit
        /// </summary>
        public static Dictionary<int, double> ResourceShielding { get; set; } = new Dictionary<int, double>();

        public static List<Quirk> Quirks { get; set; } = new List<Quirk>();

        public static Dictionary<CelestialBody, PlanetHealthConfig> PlanetConfigs { get; set; }

        public static List<RadStormType> RadStormTypes { get; set; }

        public static double SolarCycleDuration { get; set; }

        public static double SolarCycleStartingPhase { get; set; }

        public static double RadStormMinChancePerDay { get; set; }

        public static double RadStormMaxChancePerDay { get; set; }

        public static double SolarCyclePhase => (SolarCycleStartingPhase + Planetarium.GetUniversalTime() / SolarCycleDuration) % 1;

        public static double RadStormChance =>
            RadStormMinChancePerDay + (RadStormMaxChancePerDay - RadStormMinChancePerDay) * (Math.Sin(2 * Math.PI * (SolarCyclePhase + 0.75)) + 1) / 2;

        /// <summary>
        /// True if the current scene is Editor (VAB or SPH)
        /// </summary>
        public static bool IsInEditor => HighLogic.LoadedSceneIsEditor;

        /// <summary>
        /// Max amount of stress reduced by training depending on Astronaut Complex's level
        /// </summary>
        public static double TrainingCap =>
            trainingCaps[(int)Math.Round(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) * 2)];

        /// <summary>
        /// Current <see cref="LogLevel"/>: either Debug or Important
        /// </summary>
        public static LogLevel Level => KerbalHealthGeneralSettings.Instance.DebugMode ? LogLevel.Debug : LogLevel.Important;

        /// <summary>
        /// Returns factor with a given id
        /// </summary>
        /// <param name="id">Factor id</param>
        /// <returns></returns>
        public static HealthFactor GetHealthFactor(string id) => Factors.FirstOrDefault(f => f.Name == id);

        public static HealthCondition GetHealthCondition(string s) => HealthConditions.ContainsKey(s) ? HealthConditions[s] : null;

        public static void AddResourceShielding(string name, double shieldingPerTon)
        {
            PartResourceDefinition prd = PartResourceLibrary.Instance?.GetDefinition(name);
            if (prd == null)
            {
                Log($"Can't find ResourceDefinition for {name}.");
                return;
            }
            ResourceShielding.Add(prd.id, shieldingPerTon * prd.density);
        }

        public static Quirk GetQuirk(string name) => Quirks.Find(q => string.Compare(name, q.Name, true) == 0);

        public static PlanetHealthConfig GetPlanetConfig(string name)
        {
            CelestialBody cb = FlightGlobals.GetBodyByName(name);
            return (cb == null) || !PlanetConfigs.ContainsKey(cb) ? null : PlanetConfigs[cb];
        }

        public static RadStormType GetRandomRadStormType()
        {
            double d = rand.NextDouble() * radStormTypesTotalWeight;
            foreach (RadStormType rst in RadStormTypes)
            {
                d -= rst.Weight;
                if (d < 0)
                    return rst;
            }
            return null;
        }

        /// <summary>
        /// Loads necessary mod data from KerbalHealth.cfg and
        /// </summary>
        public static void LoadConfig()
        {
            Log("Loading config...", LogLevel.Important);

            ConfigNode config = GameDatabase.Instance.GetConfigNodes("KERBALHEALTH_CONFIG")[0];

            HealthConditions = new Dictionary<string, HealthCondition>();
            foreach (ConfigNode n in config.GetNodes("HEALTH_CONDITION"))
                HealthConditions.Add(n.GetValue("name"), new HealthCondition(n));
            Log($"{HealthConditions.Count} health conditions loaded.", LogLevel.Important);

            ResourceShielding = new Dictionary<int, double>();
            foreach (ConfigNode n in config.GetNodes("RESOURCE_SHIELDING"))
                AddResourceShielding(n.GetValue("name"), n.GetDouble("shielding"));
            Log($"{ResourceShielding.Count} resource shielding values loaded.", LogLevel.Important);

            Quirks = new List<Quirk>(config.GetNodes("HEALTH_QUIRK").Select(n => new Quirk(n)));
            Log($"{Quirks.Count} quirks loaded.", LogLevel.Important);

            PlanetConfigs = new Dictionary<CelestialBody, PlanetHealthConfig>(FlightGlobals.Bodies.Count);
            foreach (CelestialBody b in FlightGlobals.Bodies)
                PlanetConfigs.Add(b, new PlanetHealthConfig(b));

            int i = 0;
            foreach (ConfigNode n in config.GetNodes("PLANET_HEALTH_CONFIG"))
            {
                PlanetHealthConfig bc = GetPlanetConfig(n.GetString("name"));
                if (bc != null)
                {
                    bc.ConfigNode = n;
                    i++;
                }
            }
            Log($"{i} planet configs out of {PlanetConfigs.Count} bodies loaded.", LogLevel.Important);

            SolarCycleDuration = config.GetDouble("solarCycleDuration", 11) * KSPUtil.dateTimeFormatter.Year;
            SolarCycleStartingPhase = config.GetDouble("solarCycleStartingPhase");
            RadStormMinChancePerDay = config.GetDouble("radStormMinChance", 0.00015);
            RadStormMaxChancePerDay = config.GetDouble("radStormMaxChance", 0.00229);

            RadStormTypes = new List<RadStormType>();
            i = 0;
            foreach (ConfigNode n in config.GetNodes("RADSTORM_TYPE"))
            {
                RadStormTypes.Add(new RadStormType(n));
                radStormTypesTotalWeight += RadStormTypes[i++].Weight;
            }
            Log($"{i} radstorm types loaded with total weight {radStormTypesTotalWeight}.", LogLevel.Important);

            trainingCaps = new List<double>(3) { 0.6, 0.75, 0.85 };
            foreach (ConfigNode n in config.GetNodes("TRAINING_CAPS"))
            {
                int j = n.GetInt("level");
                if (j == 0)
                    continue;
                trainingCaps[j - 1] = n.GetDouble("cap");
            }

            ConfigLoaded = true;
        }

        /// <summary>
        /// Returns number of current crew in a vessel the kerbal is in or in the currently constructed vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static int GetCrewCount(ProtoCrewMember pcm)
        {
            if (IsInEditor)
                return ShipConstruction.ShipManifest.CrewCount;
            Vessel vessel = pcm.GetVessel();
            return vessel != null ? vessel.GetCrewCount() : 1;
        }

        /// <summary>
        /// Returns number of maximum crew in a vessel the kerbal is in or in the currently constructed vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static int GetCrewCapacity(ProtoCrewMember pcm) =>
            IsInEditor ? ShipConstruction.ShipManifest.GetAllCrew(true).Count : (pcm.IsLoaded() ? Math.Max(pcm.GetVessel().GetCrewCapacity(), 1) : 1);

        /// <summary>
        /// Returns Part where ProtoCrewMember is currently located or null if none
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static Part GetCrewPart(this ProtoCrewMember pcm) =>
            IsInEditor ? KSPUtil.GetPartByCraftID(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.GetPartForCrew(pcm).PartID) : pcm?.seat?.part;

        /// <summary>
        /// Returns true if the kerbal is in a loaded vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static bool IsLoaded(this ProtoCrewMember pcm) => pcm.GetVessel()?.loaded ?? false;

        /// <summary>
        /// Returns true if kerbal exists and is either assigned or available
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static bool IsTrackable(this ProtoCrewMember pcm) =>
            pcm != null
            && (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned
            || pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available
            || pcm.rosterStatus == (ProtoCrewMember.RosterStatus﻿)9001);

        /// <summary>
        /// Clears kerbal vessels cache, to be called on every list update or when necessary
        /// </summary>
        public static void ClearCache()
        {
            kerbalVesselsCache.Clear();
            HealthEffect.VesselCache.Clear();
        }

        /// <summary>
        /// Returns <see cref="Vessel"/> the kerbal is in or null if the kerbal is not assigned
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static Vessel GetVessel(this ProtoCrewMember pcm)
        {
            if (pcm == null || pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
                return null;

            if (kerbalVesselsCache.ContainsKey(pcm.name))
                return kerbalVesselsCache[pcm.name];

            if (DFWrapper.InstanceExists && DFWrapper.DeepFreezeAPI.FrozenKerbals.ContainsKey(pcm.name))
            {
                Vessel v = FlightGlobals.FindVessel(DFWrapper.DeepFreezeAPI.FrozenKerbals[pcm.name].vesselID);
                Log($"{pcm.name} found frozen in {v?.vesselName ?? "NULL"}.");
                kerbalVesselsCache.Add(pcm.name, v);
                return v;
            }

            foreach (Vessel v in FlightGlobals.Vessels)
                if (v.GetVesselCrew().Contains(pcm))
                {
                    kerbalVesselsCache.Add(pcm.name, v);
                    return v;
                }

            Log($"{pcm.name} is {pcm.rosterStatus} and was not found in any of the {FlightGlobals.Vessels.Count} vessels!", LogLevel.Important);
            return null;
        }

        public static double GetDistanceToSun(this Vessel v) =>
            v.mainBody == Sun.Instance.sun
            ? v.altitude + Sun.Instance.sun.Radius
            : ((v.distanceToSun > 0) ? v.distanceToSun : v.mainBody.GetPlanet().orbit.altitude + Sun.Instance.sun.Radius);

        /// <summary>
        /// Returns list of IDs of parts that are used in training and stress calculations
        /// </summary>
        /// <param name="allParts"></param>
        /// <returns></returns>
        public static List<ModuleKerbalHealth> GetTrainingCapableParts(List<Part> allParts)
        {
            List<ModuleKerbalHealth> res = new List<ModuleKerbalHealth>();
            foreach (Part p in allParts)
                res.AddRange(p.FindModulesImplementing<ModuleKerbalHealth>().Where(mkh => mkh.complexity != 0));
            return res;
        }

        public static float GetInternalFacilityLevel(int displayFacilityLevel) => (float)(displayFacilityLevel - 1) / 2;

        public static bool IsPlanet(this CelestialBody body) => body?.orbit?.referenceBody == Sun.Instance.sun;

        public static CelestialBody GetPlanet(this CelestialBody body) =>
            (body == null || body.IsPlanet()) ? body : body.orbit?.referenceBody?.GetPlanet();

        public static string GetString(this ConfigNode n, string key, string defaultValue = null) =>
            n.HasValue(key) ? n.GetValue(key) : defaultValue;

        public static double GetDouble(this ConfigNode n, string key, double defaultValue = 0) =>
            double.TryParse(n.GetValue(key), out double res) && !double.IsNaN(res) ? res : defaultValue;

        public static int GetInt(this ConfigNode n, string key, int defaultValue = 0) =>
            int.TryParse(n.GetValue(key), out int res) ? res : defaultValue;

        public static uint GetUInt(this ConfigNode n, string key, uint defaultValue = 0) =>
            uint.TryParse(n.GetValue(key), out uint res) ? res : defaultValue;

        public static bool GetBool(this ConfigNode n, string key, bool defaultValue = false) =>
            bool.TryParse(n.GetValue(key), out bool res) ? res : defaultValue;

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
        public static double GetGaussian(double stdDev = 1, double mean = 0) =>
            mean + stdDev * Math.Sqrt(-2 * Math.Log(1 - rand.NextDouble())) * Math.Sin(2 * Math.PI * (1 - rand.NextDouble()));

        /// <summary>
        /// Returns a string of a value with a mandatory sign (+ or -, unless v = 0)
        /// </summary>
        /// <param name="value">Value to present as a string</param>
        /// <param name="format">String format according to Double.ToString</param>
        /// <returns></returns>
        public static string SignValue(double value, string format) => ((value > 0) ? "+" : "") + value.ToString(format);

        /// <summary>
        /// Converts a number into a string with a multiplicative character (K, M, G or T), if applicable
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="digits">Number of digits to allow before the prefix (must be 3 or more)</param>
        /// <returns></returns>
        public static string PrefixFormat(double value, int digits = 3, bool mandatorySign = false)
        {
            double v = Math.Abs(value);
            if (v < 0.5)
                return "0";
            int n, m = (int)Math.Pow(10, digits);
            for (n = 0; (v >= m) && (n < prefixes.Length - 1); n++)
                v /= 1000;
            return (value < 0 ? "-" : (mandatorySign ? "+" : "")) + v.ToString("F" + (digits - Math.Truncate(Math.Log10(v)) - 1)) + prefixes[n];
        }

        /// <summary>
        /// Returns a zero-based year in the given timestamp (add 1 for a KSP date year)
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static int GetYear(double time) => (int)Math.Floor(time / KSPUtil.dateTimeFormatter.Year);

        /// <summary>
        /// Parses UT into a string (e.g. "2 d 3 h 15 m 59 s"), hides zero elements
        /// </summary>
        /// <param name="time">Time in seconds</param>
        /// <param name="showSeconds">If false, seconds will be displayed only if time is less than 1 minute; otherwise always</param>
        /// <param name="daysTimeLimit">If time is longer than this number of days, time value will be skipped; -1 to alwys show time</param>
        /// <returns></returns>
        public static string ParseUT(double time, bool showSeconds = true, int daysTimeLimit = -1)
        {
            if (Double.IsNaN(time) || (time == 0))
                return "—";
            if (time > KSPUtil.dateTimeFormatter.Year * 10)
                return "10y+";
            double t = time;
            int y, d, m, h;
            string res = "";
            bool show0 = false;
            if (t >= KSPUtil.dateTimeFormatter.Year)
            {
                y = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Year);
                t -= y * KSPUtil.dateTimeFormatter.Year;
                res = $"{y} y";
                show0 = true;
            }
            if ((t >= KSPUtil.dateTimeFormatter.Day) || (show0 && (t >= 1)))
            {
                d = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Day);
                t -= d * KSPUtil.dateTimeFormatter.Day;
                res = $"{res} {d} d";
                show0 = true;
            }
            if ((daysTimeLimit == -1) || (time < KSPUtil.dateTimeFormatter.Day * daysTimeLimit))
            {
                if ((t >= 3600) || show0)
                {
                    h = (int)Math.Floor(t / 3600);
                    t -= h * 3600;
                    res = $"{res} {h} h";
                    show0 = true;
                }
                if ((t >= 60) || show0)
                {
                    m = (int)Math.Floor(t / 60);
                    t -= m * 60;
                    res = $"{res} {m} m";
                }
                if ((time < 60) || (showSeconds && (Math.Floor(t) > 0)))
                    res = $"{res} {t:F0} s";
            }
            else if (time < KSPUtil.dateTimeFormatter.Day)
                res = "0 d";
            return res.TrimEnd();
        }

        public static void ShowMessage(string msg, bool unwarpTime)
        {
            KSP.UI.Screens.MessageSystem.Instance.AddMessage(new KSP.UI.Screens.MessageSystem.Message(
                "Kerbal Health",
                $"{KSPUtil.PrintDateCompact(Planetarium.GetUniversalTime(), true)}: {msg}",
                KSP.UI.Screens.MessageSystemButton.MessageButtonColor.RED,
                KSP.UI.Screens.MessageSystemButton.ButtonIcons.ALERT));
            if (unwarpTime)
                TimeWarp.SetRate(0, false, true);
        }

        public static void ShowMessage(string msg, ProtoCrewMember pcm)
        {
            if (KerbalHealthQuirkSettings.Instance.KSCNotificationsEnabled || (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Available && pcm.rosterStatus != (ProtoCrewMember.RosterStatus﻿)9001))
                ShowMessage(msg, pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned);
        }

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
            if (IsLogging(messageLevel) && (message.Length != 0))
            {
                if (messageLevel == LogLevel.Error)
                    message = $"ERROR: {message}";
                Debug.Log($"[KerbalHealth] {message}");
            }
        }
    }
}
