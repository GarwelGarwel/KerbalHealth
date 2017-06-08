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
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().useBlizzysToolbar; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().useBlizzysToolbar = value; }
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
        /// Will kerbals die upon reaching negative health?
        /// </summary>
        public static bool DeathEnabled
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().deathEnabled; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().deathEnabled = value; }
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
        {
            return (pcm != null) && ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available));
        }

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
        {
            get
            {
                if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().debugMode) return LogLevel.Debug;
                else return LogLevel.Important;
            }
        }

        /// <summary>
        /// Write into output_log.txt
        /// </summary>
        /// <param name="message">Text to log</param>
        /// <param name="messageLevel"><see cref="LogLevel"/> of the entry</param>
        public static void Log(string message, LogLevel messageLevel = LogLevel.Debug)
        { if (messageLevel <= Level) Debug.Log("[KerbalHealth] " + message); }

        private Core() { }
    }
}
