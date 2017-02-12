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
            new CrowdedFactor(),
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

        public static void AddFactor(HealthFactor f)
        { Factors.Add(f); }

        public static HealthFactor FindFactor(string id)
        {
            foreach (HealthFactor f in Factors)
                if (f.Id == id) return f;
            return null;
        }

        public static float UpdateInterval  // # of game seconds between updates
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UpdateInterval; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().UpdateInterval = value; }
        }

        public static float MinHP  // Min allowed value for health
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().MinHP; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().MinHP = value; }
        }

        public static float BaseMaxHP  // Base amount of health (for level 0 kerbal)
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().BaseMaxHP; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().BaseMaxHP = value; }
        }

        public static float HPPerLevel  // Health increase per kerbal level
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().HPPerLevel; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().HPPerLevel = value; }
        }

        public static float ExhaustionStartHealth  // Health % when the kerbal becomes exhausted (i.e. a Tourist). Must be <= ExhaustionEndHealth
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionStartHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionStartHealth = value; }
        }

        public static float ExhaustionEndHealth  // Health % when the kerbal leaves exhausted state (i.e. becomes Crew again). Must be >= ExhaustionStartHealth
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionEndHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().ExhaustionEndHealth = value; }
        }

        public static float DeathHealth  // Health % when the kerbal dies
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().DeathHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().DeathHealth = value; }
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
            if (IsInEditor) return ShipConstruction.ShipManifest.CrewCount;
            Vessel v = pcm?.KerbalRef?.InVessel;
            if (v == null) return 0;
            return v.GetCrewCount();
        }

        public static int GetCrewCapacity(ProtoCrewMember pcm)
        {
            if (IsInEditor) return ShipConstruction.ShipManifest.GetAllCrew(true).Count;
            Vessel v = pcm?.KerbalRef?.InVessel;
            if ((v == null) || (v.GetCrewCapacity() < 1)) return 1;
            return v.GetCrewCapacity();
        }

        public static bool IsKerbalLoaded(ProtoCrewMember pcm)
        { return pcm?.seat?.vessel != null; }

        public static Vessel KerbalVessel(ProtoCrewMember pcm)
        { return pcm?.seat?.vessel; }

        public enum LogLevel { None, Error, Important, Debug };
        public static LogLevel Level
        {
            get { if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().debugMode) return LogLevel.Debug; else return LogLevel.Important; }
        }

        public static void Log(string message, LogLevel messageLevel = LogLevel.Debug)
        {
            if (messageLevel <= Level)
                Debug.Log("[KerbalHealth] " + Time.realtimeSinceStartup + ": " + message);
        }
    }
}
