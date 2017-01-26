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

        public enum Factors { Assigned, LivingSpace, Loneliness, KSC }

        public static float AssignedFactor  // Health change per day when the kerbal is assigned
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().AssignedFactor; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().AssignedFactor = value; }
        }

        public static float LivingSpaceBaseFactor  // Health change per day in a crammed vessel
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().LivingSpaceBaseFactor; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().LivingSpaceBaseFactor = value; }
        }

        public static float LonelinessFactor  // Health change per day when the kerbal has crewmates
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().LonelinessFactor; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().LonelinessFactor = value; }
        }

        public static float KSCFactor  // Health change per day when the kerbal is at KSC (available)
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().KSCFactor; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().KSCFactor = value; }
        }

        public static bool IsInEditor
        { get { return HighLogic.LoadedSceneIsEditor; } }

        public static string ParseUT(double time)
        {
            if (double.IsNaN(time) || (time == 0)) return "N/A";
            return KSPUtil.PrintDateDeltaCompact(time, true, false);
        }

        public enum LogLevel { None, Error, Warning, Debug };
        public static LogLevel Level { get; set; } = LogLevel.Debug;

        public static void Log(string message, LogLevel messageLevel = LogLevel.Debug)
        {
            if (messageLevel <= Level)
                Debug.Log("[KerbalHealth] " + Time.realtimeSinceStartup + ": " + message);
        }
    }
}
