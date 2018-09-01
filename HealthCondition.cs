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
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// HP change per day when this condition is active
        /// </summary>
        public double HPPerDay { get; set; } = 0;

        public override string ToString() => Title + " (" + Name + ")\r\nVisible: " + IsVisible + "\r\nHP change/day: " + HPPerDay;

        public virtual ConfigNode ConfigNode
        {
            set
            {
                Name = value.GetValue("name");
                if (value.HasValue("title")) Title = value.GetValue("title");
                IsVisible = Core.GetBool(value, "visible", true);
                HPPerDay = Core.GetDouble(value, "hpPerDay");
            }
        }

        public HealthCondition(ConfigNode n) => ConfigNode = n;
    }
}
