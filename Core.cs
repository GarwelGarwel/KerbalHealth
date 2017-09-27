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
            get { return factors; }
            set { factors = value; }
        }

        /// <summary>
        /// Returns factor with a given id
        /// </summary>
        /// <param name="id">Factor id</param>
        /// <returns></returns>
        public static HealthFactor FindFactor(string id)
        {
            foreach (HealthFactor f in Factors) if (f.Name == id) return f;
            return null;
        }

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
            get { return events; }
            set { events = value; }
        }

        /// <summary>
        /// Keeps data about all resources that provide Shielding. Key is resource id, value is amount of shielding provided by 1 unit
        /// </summary>
        public static Dictionary<int, double> ResourceShielding { get; set; } = new Dictionary<int, double>();

        /// <summary>
        /// Is Kerbal Health is enabled via Settings menu?
        /// </summary>
        public static bool ModEnabled
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().modEnabled; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().modEnabled = value; }
        }

        /// <summary>
        /// Use message system as opposed to displaying screen messages
        /// </summary>
        public static bool UseMessageSystem
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().useMessageSystem; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().useMessageSystem = value; }
        }

        /// <summary>
        /// Use Blizzy's Toolbar mod instead of stock app launcher
        /// </summary>
        public static bool UseBlizzysToolbar
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UseBlizzysToolbar; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UseBlizzysToolbar = value; }
        }

        /// <summary>
        /// Number of game seconds between updates
        /// </summary>
        public static float UpdateInterval
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UpdateInterval; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UpdateInterval = value; }
        }

        /// <summary>
        /// Minimum number of real-world seconds between updates (used in high timewarp)
        /// </summary>
        public static float MinUpdateInterval
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().MinUpdateInterval; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().MinUpdateInterval = value; }
        }

        /// <summary>
        /// Base amount of health points (for level 0 kerbal)
        /// </summary>
        public static float BaseMaxHP
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().BaseMaxHP; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().BaseMaxHP = value; }
        }

        /// <summary>
        /// HP increase per kerbal level
        /// </summary>
        public static float HPPerLevel
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().HPPerLevel; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().HPPerLevel = value; }
        }

        /// <summary>
        /// Health level when a low health alert is shown
        /// </summary>
        public static float LowHealthAlert
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().LowHealthAlert; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().LowHealthAlert = value; }
        }

        /// <summary>
        /// Will kerbals die upon reaching negative health?
        /// </summary>
        public static bool DeathEnabled
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().DeathEnabled; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().DeathEnabled = value; }
        }

        /// <summary>
        /// Health % when the kerbal becomes exhausted (i.e. a Tourist). Must be <= <see cref="ExhaustionEndHealth"/>.
        /// </summary>
        public static float ExhaustionStartHealth
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionStartHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionStartHealth = value; }
        }

        /// <summary>
        /// Health % when the kerbal leaves exhausted state (i.e. becomes Crew again). Must be >= <see cref="ExhaustionStartHealth"/>.
        /// </summary>
        public static float ExhaustionEndHealth
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionEndHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionEndHealth = value; }
        }

        /// <summary>
        /// Random events can happen
        /// </summary>
        public static bool EventsEnabled
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().EventsEnabled; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().EventsEnabled = value; }
        }

        /// <summary>
        /// Sickness-related events can fire, Sickness factor affects kerbals
        /// </summary>
        public static bool SicknessEnabled
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().SicknessEnabled; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().SicknessEnabled = value; }
        }

        /// <summary>
        /// Whether to run radiation-related parts of the code
        /// </summary>
        public static bool RadiationEnabled
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().RadiationEnabled; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().RadiationEnabled = value; }
        }

        /// <summary>
        /// Percentage of max health drained by 1e7 (10M) doses. 0 to disable effect
        /// </summary>
        public static float RadiationEffect
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().RadiationEffect; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().RadiationEffect = value; }
        }

        /// <summary>
        /// How much cosmic radiation reaches vessels in high planetary orbits and on moons
        /// </summary>
        public static float InSpaceHighCoefficient
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceHighCoefficient; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceHighCoefficient = value; }
        }

        /// <summary>
        /// How much cosmic radiation reaches vessels in low planetary orbits
        /// </summary>
        public static float InSpaceLowCoefficient
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceLowCoefficient; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceLowCoefficient = value; }
        }

        /// <summary>
        /// How much cosmic radiation reaches outer layers of the atmosphere
        /// </summary>
        public static float StratoCoefficient
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().StratoCoefficient; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().StratoCoefficient = value; }
        }

        /// <summary>
        /// How much cosmic radiation reaches the ground and lower layers of the atmosphere
        /// </summary>
        public static float TroposphereCoefficient
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().TroposphereCoefficient; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().TroposphereCoefficient = value; }
        }

        /// <summary>
        /// How much more radiaiton kerbals receive when on EVA
        /// </summary>
        public static float EVAExposure
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().EVAExposure; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().EVAExposure = value; }
        }

        /// <summary>
        /// Solar radiation in interplanetary space at 1 AU, banana doses/day
        /// </summary>
        public static float SolarRadiation
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().SolarRadiation; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().SolarRadiation = value; }
        }

        /// <summary>
        /// Galactic cosmic radiation in interplanetary space, banana doses/day
        /// </summary>
        public static float GalacticRadiation
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().GalacticRadiation; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().GalacticRadiation = value; }
        }

        /// <summary>
        /// True if the current scene is Editor (VAB or SPH)
        /// </summary>
        public static bool IsInEditor
        { get { return HighLogic.LoadedSceneIsEditor; } }

        /// <summary>
        /// Parses UT as a delta compact time (e.g. "2d 3h 15m"). Time is hidden when days >= 100.
        /// </summary>
        /// <param name="time">Universal time</param>
        /// <returns></returns>
        public static string ParseUT(double time)
        {
            if (double.IsNaN(time) || (time == 0)) return "N/A";
            return KSPUtil.PrintDateDeltaCompact(time, time < KSPUtil.dateTimeFormatter.Day * 100, false);
        }

        /// <summary>
        /// Returns number of current crew in a vessel the kerbal is in or in the currently constructed vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static int GetCrewCount(ProtoCrewMember pcm)
        {
            if (IsInEditor) return ShipConstruction.ShipManifest.CrewCount;
            return IsKerbalLoaded(pcm) ? KerbalVessel(pcm).GetCrewCount() : 1;
        }

        /// <summary>
        /// Returns number of maximum crew in a vessel the kerbal is in or in the currently constructed vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static int GetCrewCapacity(ProtoCrewMember pcm)
        {
            if (IsInEditor) return ShipConstruction.ShipManifest.GetAllCrew(true).Count;
            return IsKerbalLoaded(pcm) ? Math.Max(KerbalVessel(pcm).GetCrewCapacity(), 1) : 1;
        }

        /// <summary>
        /// Returns true if the kerbal is in a loaded vessel
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static bool IsKerbalLoaded(ProtoCrewMember pcm)
        { return (pcm?.seat?.vessel != null) || (KerbalVessel(pcm)?.loaded ?? false); }

        /// <summary>
        /// Returns true if kerbal exists and is either assigned or available
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static bool IsKerbalTrackable(ProtoCrewMember pcm)
        { return (pcm != null) && ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available)); }

        /// <summary>
        /// Returns true if kerbal is currently frozen with DeepFreeze
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <remarks>Currently always returns false since DeepFreeze implementation is bugged</remarks>
        public static bool IsKerbalFrozen(string name)
        {
            //if (!DFWrapper.APIReady) return false;
            //foreach (KeyValuePair<string, DFWrapper.KerbalInfo> el in DFWrapper.DeepFreezeAPI.FrozenKerbalsList)
            //    if (el.Key == name) return true;
            return false;
        }

        /// <summary>
        /// Returns <see cref="Vessel"/> the kerbal is in or null if the kerbal is not assigned
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static Vessel KerbalVessel(ProtoCrewMember pcm)
        {
            if (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Assigned) return null;
            foreach (Vessel v in FlightGlobals.Vessels)
                foreach (ProtoCrewMember k in v.GetVesselCrew())
                    if (k == pcm)
                        return v;
            Log(pcm.name + " is Assigned, but was not found in any of the " + FlightGlobals.Vessels.Count + " vessels!", LogLevel.Error);
            return null;
        }

        public static double GetResourceAmount(List<Part> parts, int resourceId)
        {
            double res = 0;
            foreach (Part p in parts)
            {
                double amount = 0, maxAmount;
                try { p.GetConnectedResourceTotals(resourceId, ResourceFlowMode.NO_FLOW, out amount, out maxAmount); }
                catch (NullReferenceException e) { Core.Log(e.Message + "\r\nNRE in " + e.Source); }
                res += amount;
            }
            return res;
        }

        public static double GetDouble(ConfigNode n, string key, double defaultValue = 0)
        {
            double res;
            try { res = Double.Parse(n.GetValue(key)); }
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

        public static double Sqr(double x)
        { return x * x; }

        public static void ShowMessage(string msg, bool useMessageSystem, bool unwarpTime)
        {
            if (useMessageSystem) KSP.UI.Screens.MessageSystem.Instance.AddMessage(new KSP.UI.Screens.MessageSystem.Message("Kerbal Health", msg, KSP.UI.Screens.MessageSystemButton.MessageButtonColor.RED, KSP.UI.Screens.MessageSystemButton.ButtonIcons.ALERT));
            else ScreenMessages.PostScreenMessage(msg);
            if (unwarpTime) TimeWarp.SetRate(0, false, useMessageSystem);
        }

        public static void ShowMessage(string msg, bool unwarpTime)
        { ShowMessage(msg, UseMessageSystem, unwarpTime); }

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
        public static LogLevel Level
        { get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().DebugMode ? LogLevel.Debug : LogLevel.Important; } }

        /// <summary>
        /// Write into output_log.txt
        /// </summary>
        /// <param name="message">Text to log</param>
        /// <param name="messageLevel"><see cref="LogLevel"/> of the entry</param>
        public static void Log(string message, LogLevel messageLevel = LogLevel.Debug)
        { if ((messageLevel <= Level) && (message != "")) Debug.Log("[KerbalHealth] " + message); }

        private Core() { }
    }
}
