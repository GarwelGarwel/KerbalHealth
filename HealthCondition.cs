using System.Collections.Generic;
using System.Linq;

namespace KerbalHealth
{
    /// <summary>
    /// Holds information about a certain health condition (such as exhaustion, sickness, etc.)
    /// </summary>
    public class HealthCondition
    {
        string title;

        /// <summary>
        /// Internal name of the condition
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Displayable name of the condition (similar to Name by default)
        /// </summary>
        public string Title
        {
            get => ((title == null) || (title.Length == 0)) ? Name : title;
            set => title = value;
        }

        /// <summary>
        /// Text description of the condition, shown when it is acquired
        /// </summary>
        public string Description { get; set; } = "";

            /// <summary>
        /// Whether this condition should be visible to the player
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Whether this condition can be added multiple times
        /// </summary>
        public bool Stackable { get; set; } = false;

        /// <summary>
        /// If either of these conditions exist, this one will not be randomly acquired
        /// </summary>
        public List<string> IncompatibleConditions { get; set; } = new List<string>();

        public bool IsCompatibleWith(string condition) => !IncompatibleConditions.Contains(condition);

        public bool IsCompatibleWith(List<HealthCondition> conditions) => !conditions.Any(hc => IncompatibleConditions.Contains(hc.Name));

        /// <summary>
        /// Logic required for this health condition to randomly appear
        /// </summary>
        public Logic Logic { get; set; } = new Logic();

        /// <summary>
        /// HP change per day when this condition is active
        /// </summary>
        public double HPChangePerDay { get; set; } = 0;

        /// <summary>
        /// While this condition is active, kerbal's HP is changed by this amount
        /// </summary>
        public double HP { get; set; } = 0;

        /// <summary>
        /// Whether to bring HP back to its original level when the condition is removed
        /// </summary>
        public bool RestoreHP { get; set; } = false;

        /// <summary>
        /// Whether this condition turns the kerbal into a Tourist
        /// </summary>
        public bool Incapacitated { get; set; } = false;

        /// <summary>
        /// Base chance of this condition randomly appearing every day
        /// </summary>
        public double ChancePerDay { get; set; } = 0;

        /// <summary>
        /// List of all chance modifiers for this condition
        /// </summary>
        public List<ChanceModifier> ChanceModifiers { get; set; }

        /// <summary>
        /// Returns actual chance per day of this condition considering all modifiers
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public double GetChancePerDay(ProtoCrewMember pcm) => ChanceModifier.Calculate(ChanceModifiers, ChancePerDay, pcm);

        /// <summary>
        /// Possible outcomes of the condition; it is recommended to have at least one so that it may disappear
        /// </summary>
        public List<Outcome> Outcomes { get; set; } = new List<Outcome>();

        public override string ToString() => Title + " (" + Name + "): " + Description;

        public ConfigNode ConfigNode
        {
            set
            {
                Name = value.GetValue("name");
                Title = value.GetString("title");
                Description = value.GetString("description", "");
                Visible = value.GetBool("visible", true);
                Stackable = value.GetBool("stackable");
                IncompatibleConditions.AddRange(value.GetValues("incompatibleCondition"));
                Logic.ConfigNode = value;
                HPChangePerDay = value.GetDouble("hpChangePerDay");
                HP = value.GetDouble("hp");
                RestoreHP = value.GetBool("restoreHP");
                Incapacitated = value.GetBool("incapacitated");
                ChancePerDay = value.GetDouble("chancePerDay");
                ChanceModifiers = new List<ChanceModifier>(value.GetNodes("CHANCE_MODIFIER").Select(n => new ChanceModifier(n)));
                //foreach (ConfigNode n in value.GetNodes("CHANCE_MODIFIER"))
                //    ChanceModifiers.Add(new ChanceModifier(n));
                Outcomes = new List<Outcome>(value.GetNodes("OUTCOME").Select(n => new Outcome(n)));
                //foreach (ConfigNode n in value.GetNodes("OUTCOME"))
                //    Outcomes.Add(new Outcome(n));
            }
        }

        public HealthCondition(ConfigNode n) => ConfigNode = n;
    }
}
