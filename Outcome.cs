using System.Collections.Generic;

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

        /// <summary>
        /// List of all chance modifiers for this outcome
        /// </summary>
        public List<ChanceModifier> ChanceModifiers { get; set; } = new List<ChanceModifier>();

        /// <summary>
        /// Returns actual chance per day of this outcome considering all modifiers
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public double GetChancePerDay(ProtoCrewMember pcm) => ChanceModifier.Calculate(ChanceModifiers, ChancePerDay, pcm);

        public ConfigNode ConfigNode
        {
            set
            {
                Condition = Core.GetString(value, "condition", "");
                RemoveOldCondition = Core.GetBool(value, "removeOldCondition", true);
                ChancePerDay = Core.GetDouble(value, "chancePerDay");
                foreach (ConfigNode n in value.GetNodes("CHANCE_MODIFIER"))
                    ChanceModifiers.Add(new ChanceModifier(n));
            }
        }

        public override string ToString()
            => RemoveOldCondition
            ? (Condition.Length == 0 ? "Remove current condition" : ("Change to " + Condition + " condition"))
            : ("Add " + Condition + " condition") + " with a chance of " + ChancePerDay + " with " + ChanceModifiers.Count + " chance modifiers";

        public Outcome(ConfigNode node) => ConfigNode = node;
    }
}
