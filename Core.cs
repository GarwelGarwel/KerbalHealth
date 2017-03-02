using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalHealth
{
    public class Core
    {
        public static KerbalHealthList KerbalHealthList { get; set; } = new KerbalHealthList();

        static List<HealthFactor> factors = new List<HealthFactor>() {
            new AssignedFactor(),
            new OverpopulationFactor(),
            new LonelinessFactor(),
            new MicrogravityFactor(),
            new EVAFactor(),
            new ConnectedFactor(),
            new HomeFactor(),
            new KSCFactor()
        };

        public static List<HealthFactor> Factors
        {
            get { return factors; }
            set { factors = value; }
        }

        public static HealthFactor FindFactor(string id)
        {
            foreach (HealthFactor f in Factors)
                if (f.Id == id) return f;
            return null;
        }

        static List<Event> events = new List<Event>()
        {
            new FeelBadEvent(),
            new PanicAttackEvent()
        };

        public static List<Event> Events
        {
            get { return events; }
            set { events = value; }
        }

        public static float UpdateInterval  // # of game seconds between updates
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().UpdateInterval; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().UpdateInterval = value; }
        }

        public static float MinHP  // Min allowed value for health
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().MinHP; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().MinHP = value; }
        }

        public static float BaseMaxHP  // Base amount of health (for level 0 kerbal)
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().BaseMaxHP; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().BaseMaxHP = value; }
        }

        public static float HPPerLevel  // Health increase per kerbal level
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().HPPerLevel; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().HPPerLevel = value; }
        }

        public static float ExhaustionStartHealth  // Health % when the kerbal becomes exhausted (i.e. a Tourist). Must be <= ExhaustionEndHealth
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().ExhaustionStartHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().ExhaustionStartHealth = value; }
        }

        public static float ExhaustionEndHealth  // Health % when the kerbal leaves exhausted state (i.e. becomes Crew again). Must be >= ExhaustionStartHealth
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().ExhaustionEndHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().ExhaustionEndHealth = value; }
        }

        public static float DeathHealth  // Health % when the kerbal dies
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().DeathHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().DeathHealth = value; }
        }

        public static bool IsInEditor
        { get { return HighLogic.LoadedSceneIsEditor; } }

        public static string ParseUT(double time)
        {
            if (double.IsNaN(time) || (time == 0)) return "N/A";
            if (time < 21600 * 100) return KSPUtil.PrintDateDeltaCompact(time, true, false);
            else return KSPUtil.PrintDateDeltaCompact(time, false, false);
        }

        public static int GetCrewCount(ProtoCrewMember pcm)
        {
            return IsInEditor ? ShipConstruction.ShipManifest.CrewCount : (pcm?.seat?.vessel.GetCrewCount() ?? 1);
        }

        public static int GetCrewCapacity(ProtoCrewMember pcm)
        {
            return IsInEditor ? ShipConstruction.ShipManifest.GetAllCrew(true).Count : (pcm?.seat?.vessel.GetCrewCapacity() ?? 1);
        }

        public static bool IsKerbalLoaded(ProtoCrewMember pcm)
        { return pcm?.seat?.vessel != null; }

        public static Vessel KerbalVessel(ProtoCrewMember pcm)
        { return pcm?.seat?.vessel; }

        public static System.Random rand = new System.Random();

        public enum LogLevels { None, Error, Important, Debug };
        public static LogLevels LogLevel
        {
            get { if (HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().debugMode) return LogLevels.Debug; else return LogLevels.Important; }
        }

        public static void Log(string message, LogLevels messageLevel = LogLevels.Debug)
        {
            if (messageLevel <= LogLevel)
                Debug.Log("[KerbalHealth] " + Time.realtimeSinceStartup + ": " + message);
        }
    }
}
