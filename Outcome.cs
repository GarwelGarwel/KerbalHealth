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
        /// Mean time of the outcome happening, days
        /// </summary>
        public double MTBE { get; set; } = -1;

        /// <summary>
        /// List of all MTBE modifiers for this outcome
        /// </summary>
        public List<MTBEModifier> MTBEModifiers { get; set; }

        public void Load(ConfigNode node)
        {
            Condition = node.GetString("condition", "");
            RemoveOldCondition = node.GetBool("removeOldCondition", true);
            MTBE = node.GetDouble("mtbe", -1);
            MTBEModifiers = new List<MTBEModifier>(node.GetNodes(MTBEModifier.ConfigNodeName).Select(n => new MTBEModifier(n)));
        }

        public Outcome(ConfigNode node) => Load(node);

        /// <summary>
        /// Returns actual chance per day of this outcome considering all modifiers
        /// </summary>
        public double GetMTBE(ProtoCrewMember pcm) => MTBEModifier.Calculate(MTBEModifiers, MTBE, pcm);

        public override string ToString() =>
            $"{(RemoveOldCondition ? (Condition.Length == 0 ? "Remove current" : $"Change to {Condition}") : $"Add {Condition}")} condition with MTBE of {MTBE} days with {MTBEModifiers.Count} MTBE modifiers";
    }
}
