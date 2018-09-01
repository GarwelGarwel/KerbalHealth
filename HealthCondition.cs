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

        public override string ToString() => Title + " (" + Name + ")\r\nVisible: " + Visible + "\r\nHP change/day: " + HPPerDay;

        public virtual ConfigNode ConfigNode
        {
            set
            {
                Name = value.GetValue("name");
                if (value.HasValue("title")) Title = value.GetValue("title");
                Visible = Core.GetBool(value, "visible", true);
                Stackable = Core.GetBool(value, "stackable");
                HPPerDay = Core.GetDouble(value, "hpPerDay");
                Incapacitated = Core.GetBool(value, "incapacitated");
                ChancePerDay = Core.GetDouble(value, "chancePerDay");
            }
        }

        public HealthCondition(ConfigNode n) => ConfigNode = n;
    }
}
