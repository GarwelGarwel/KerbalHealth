using System.Collections.Generic;

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
            get => ((title == null) || (title == "")) ? Name : title;
            set => title = value;
        }

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

        public bool IsCompatibleWith(List<HealthCondition> conditions)
        {
            foreach (HealthCondition hc in conditions)
                if (IncompatibleConditions.Contains(hc.Name)) return false;
            return true;
        }

        /// <summary>
        /// Logic required for this health condition to randomly appear
        /// </summary>
        public Logic Logic { get; set; } = new Logic();

        /// <summary>
        /// HP change per day when this condition is active
        /// </summary>
        public double HPPerDay { get; set; } = 0;

        /// <summary>
        /// Whether this condition turns the kerbal into a Tourist
        /// </summary>
        public bool Incapacitated { get; set; } = false;

        /// <summary>
        /// Base chance of this condition randomly appearing every day
        /// </summary>
        public double ChancePerDay { get; set; } = 0;

        /// <summary>
        /// Possible outcomes of the condition; it is recommended to have at least one so that it may disappear
        /// </summary>
        public List<Outcome> Outcomes { get; set; } = new List<Outcome>();

        public override string ToString() => Title + " (" + Name + ")\r\nVisible: " + Visible + "\r\nHP change/day: " + HPPerDay;

        public ConfigNode ConfigNode
        {
            set
            {
                Name = value.GetValue("name");
                if (value.HasValue("title")) Title = value.GetValue("title");
                Visible = Core.GetBool(value, "visible", true);
                Stackable = Core.GetBool(value, "stackable");
                foreach (string s in value.GetValues("incompatibleCondition"))
                    IncompatibleConditions.Add(s);
                Logic.ConfigNode = value;
                HPPerDay = Core.GetDouble(value, "hpPerDay");
                Incapacitated = Core.GetBool(value, "incapacitated");
                ChancePerDay = Core.GetDouble(value, "chancePerDay");
                foreach (ConfigNode n in value.GetNodes("OUTCOME"))
                    Outcomes.Add(new Outcome(n));
            }
        }

        public HealthCondition(ConfigNode n) => ConfigNode = n;
    }
}
