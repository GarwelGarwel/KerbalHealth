using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalHealth
{
    /// <summary>
    /// Provides general static methods and fields for KerbalHealth
    /// </summary>
    public class Core
    {
        public static bool Loaded = false;

        /// <summary>
        /// List of all tracked kerbals
        /// </summary>
        public static KerbalHealthList KerbalHealthList { get; set; } = new KerbalHealthList();

        static List<HealthFactor> factors = new List<HealthFactor>() {
            new AssignedFactor(),
            new CrowdedFactor(),
            new LonelinessFactor(),
            new MicrogravityFactor(),
            new EVAFactor(),
            new SicknessFactor(),
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

        static List<Event> events = new List<Event>()
        {
            new AccidentEvent(),
            new PanicAttackEvent(),
            new CureEvent(),
            new GetSickEvent(),
            new GetInfectedEvent(),
            new LoseImmunityEvent()
        };

        /// <summary>
        /// List of all possible health events to be checked
        /// </summary>
        public static List<Event> Events
        {
            get => events;
            set => events = value;
        }

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

        public static void LoadConfig()
        {
            Log("Loading config...", LogLevel.Important);

            HealthConditions = new Dictionary<string, HealthCondition>();
            foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("HEALTH_CONDITION"))
                HealthConditions.Add(n.GetValue("name"), new HealthCondition(n));
            Core.Log(HealthConditions.Count + " health conditions loaded:");
            foreach (HealthCondition hc in HealthConditions.Values)
                Core.Log(hc.ToString());

            ResourceShielding = new Dictionary<int, double>();
            foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("RESOURCE_SHIELDING"))
                AddResourceShielding(n.GetValue("name"), GetDouble(n, "shielding"));
            Log(ResourceShielding.Count + " resource shielding values loaded.", LogLevel.Important);

            Quirks = new List<Quirk>();
            foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("HEALTH_QUIRK"))
                Quirks.Add(new Quirk(n));
            Core.Log(Quirks.Count + " quirks loaded.", LogLevel.Important);

            PlanetConfigs = new Dictionary<CelestialBody, PlanetHealthConfig>(FlightGlobals.Bodies.Count);
            foreach (CelestialBody b in FlightGlobals.Bodies) PlanetConfigs.Add(b, new PlanetHealthConfig(b));
            int i = 0;
            foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("PLANET_HEALTH_CONFIG"))
            {
                PlanetHealthConfig bc = GetPlanetConfig(GetString(n, "name"));
                if (bc != null)
                {
                    bc.ConfigNode = n;
                    i++;
                }
            }
            Core.Log(i + " planet configs out of " + PlanetConfigs.Count + " bodies loaded.", LogLevel.Important);

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
        /// Use message system as opposed to displaying screen messages
        /// </summary>
        public static bool UseMessageSystem
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().useMessageSystem;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().useMessageSystem = value;
        }

        /// <summary>
        /// Use Blizzy's Toolbar mod instead of stock app launcher
        /// </summary>
        public static bool UseBlizzysToolbar
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UseBlizzysToolbar;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UseBlizzysToolbar = value;
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
        /// Random events can happen
        /// </summary>
        public static bool EventsEnabled
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().EventsEnabled;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().EventsEnabled = value;
        }

        /// <summary>
        /// Sickness-related events can fire, Sickness factor affects kerbals
        /// </summary>
        public static bool SicknessEnabled
        {
            get => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().SicknessEnabled;
            set => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().SicknessEnabled = value;
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
        public static float StratoCoefficient
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
        public static bool IsKerbalTrackable(ProtoCrewMember pcm) => (pcm != null) && ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available));

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
            if ((pcm == null) || (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available)) return null;
            if (kerbalVesselsCache.ContainsKey(pcm.name)) return kerbalVesselsCache[pcm.name];
            foreach (Vessel v in FlightGlobals.Vessels)
                foreach (ProtoCrewMember k in v.GetVesselCrew())
                    if (k == pcm)
                    {
                        kerbalVesselsCache.Add(pcm.name, v);
                        return v;
                    }
            if (DFWrapper.InstanceExists && DFWrapper.DeepFreezeAPI.FrozenKerbals.ContainsKey(pcm.name))
            {
                Vessel v = FlightGlobals.FindVessel(DFWrapper.DeepFreezeAPI.FrozenKerbals[pcm.name].vesselID);
                Log(pcm.name + " found in FrozenKerbals.");
                kerbalVesselsCache.Add(pcm.name, v);
                return v;
            }
            Log(pcm.name + " is " + pcm.rosterStatus + " and was not found in any of the " + FlightGlobals.Vessels.Count + " vessels!", LogLevel.Important);
            return null;
        }

        public static bool IsPlanet(CelestialBody body) => body?.orbit?.referenceBody == Sun.Instance.sun;

        public static CelestialBody GetPlanet(CelestialBody body) => ((body == null) || IsPlanet(body)) ? body : GetPlanet(body?.orbit?.referenceBody);

        public static string GetString(ConfigNode n, string key, string defaultValue = null) => n.HasValue(key) ? n.GetValue(key) : defaultValue;

        public static double GetDouble(ConfigNode n, string key, double defaultValue = 0)
        {
            double res;
            try { res = Double.Parse(n.GetValue(key)); }
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
        /// Returns a string representing value v with a mandatory sign (+ or -, unless v = 0)
        /// </summary>
        /// <param name="v">Value to present as a string</param>
        /// <param name="format">String format according to Double.ToString</param>
        /// <returns></returns>
        public static string SignValue(double v, string format) => ((v > 0) ? "+" : "") + v.ToString(format);

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

        public static void ShowMessage(string msg, bool useMessageSystem, bool unwarpTime)
        {
            if (useMessageSystem) KSP.UI.Screens.MessageSystem.Instance.AddMessage(new KSP.UI.Screens.MessageSystem.Message("Kerbal Health", KSPUtil.PrintDateCompact(Planetarium.GetUniversalTime(), true) + ": " + msg, KSP.UI.Screens.MessageSystemButton.MessageButtonColor.RED, KSP.UI.Screens.MessageSystemButton.ButtonIcons.ALERT));
            else ScreenMessages.PostScreenMessage(msg);
            if (unwarpTime) TimeWarp.SetRate(0, false, useMessageSystem);
        }

        public static void ShowMessage(string msg, bool unwarpTime) => ShowMessage(msg, UseMessageSystem, unwarpTime);

        public static void ShowMessage(string msg, ProtoCrewMember pcm)
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().KSCNotificationsEnabled && (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available)) return;
            ShowMessage(msg, UseMessageSystem, pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned);
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

        private Core() { }
    }
}
