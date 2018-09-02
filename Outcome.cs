using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    /// <summary>
    /// Represents a possible outcome (consequence) of a HealthCondition
    /// </summary>
    public class Outcome
    {
        /// <summary>
        /// The kerbal acquires this condition when the outcome is activated (no new condition if empty)
        /// </summary>
        public string Condition { get; set; } = "";

        /// <summary>
        /// Whether the original condition should be removed when the outcome is activated
        /// </summary>
        public bool RemoveOldCondition { get; set; } = true;

        /// <summary>
        /// Chance this outcome is chosen every day
        /// </summary>
        public double ChancePerDay { get; set; } = 0;

        public ConfigNode ConfigNode
        {
            set
            {
                Condition = Core.GetString(value, "condition", "");
                RemoveOldCondition = Core.GetBool(value, "removeOldCondition", true);
                ChancePerDay = Core.GetDouble(value, "chancePerDay");
            }
        }

        public override string ToString() => RemoveOldCondition ? (Condition == "" ? "Remove current condition" : ("Change to " + Condition + " condition")) : ("Add " + Condition + " condition") + " with a chance of " + ChancePerDay;

        public Outcome(ConfigNode node) => ConfigNode = node;
    }
}
