using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static float NotAloneFactor  // Health change per day when the kerbal has crewmates
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().NotAloneFactor; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().NotAloneFactor = value; }
        }

        public static float KSCFactor  // Health change per day when the kerbal is at KSC (available)
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().KSCFactor; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().KSCFactor = value; }
        }
    }
}
