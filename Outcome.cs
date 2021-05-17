using System.Collections.Generic;
using System.Linq;

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
        public List<ChanceModifier> ChanceModifiers { get; set; }

        public void Load(ConfigNode node)
        {
            Condition = node.GetString("condition", "");
            RemoveOldCondition = node.GetBool("removeOldCondition", true);
            ChancePerDay = node.GetDouble("chancePerDay");
            ChanceModifiers = new List<ChanceModifier>(node.GetNodes("CHANCE_MODIFIER").Select(n => new ChanceModifier(n)));
        }

        public Outcome(ConfigNode node) => Load(node);

        /// <summary>
        /// Returns actual chance per day of this outcome considering all modifiers
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public double GetChancePerDay(ProtoCrewMember pcm) => ChanceModifier.Calculate(ChanceModifiers, ChancePerDay, pcm);

        public override string ToString() =>
            $"{(RemoveOldCondition ? (Condition.Length == 0 ? "Remove current" : $"Change to {Condition}") : $"Add {Condition}")} condition with a chance of {ChancePerDay} with {ChanceModifiers.Count} chance modifiers";
    }
}
