﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    /// <summary>
    /// Describes a health quirk
    /// </summary>
    public class Quirk
    {
        string title;
        string description;

        public string Name { get; set; }

        public string Title
        {
            get => title ?? Name;
            set => title = value;
        }

        public string Description
        {
            get
            {
                if (description == null || description.Length == 0)
                {
                    StringBuilder desc = new StringBuilder();
                    for (int i = 0; i < Effects.Count; i++)
                        desc.AppendLine(Effects[i].ToString());
                    description = desc.ToString();
                    Core.Log($"Quirk {Name} ({Effects.Count} effects):\r\n{description}");
                }
                return description;
            }
            set => description = value;
        }

        public bool IsVisible { get; set; } = true;

        public int MinLevel { get; set; } = 0;

        public List<string> IncompatibleQuirks { get; set; } = new List<string>();

        public double CourageWeight { get; set; } = 1;

        public double StupidityWeight { get; set; } = 1;

        public List<ConditionalEffect> Effects { get; set; } = new List<ConditionalEffect>();

        public Quirk(ConfigNode node)
        {
            Name = node.GetValue("name");
            Title = node.GetValue("title");
            Description = node.GetValue("description");
            IsVisible = node.GetBool("visible", true);
            MinLevel = node.GetInt("minLevel");
            IncompatibleQuirks = new List<string>(node.GetValues("incompatibleWith"));
            CourageWeight = node.GetDouble("courageWeight", 1);
            StupidityWeight = node.GetDouble("stupidityWeight", 1);
            Effects = new List<ConditionalEffect>(node.GetNodes("EFFECT").Select(n => new ConditionalEffect(n)));
            Core.Log($"Quirk loaded: {this}");
        }

        public Quirk(string name) => Name = name;

        /// <summary>
        /// Returns true if this quirk can be assigned to the given kerbal at a certain experience level
        /// </summary>
        /// <param name="khs"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool IsAvailableTo(KerbalHealthStatus khs, int level) =>
            level >= MinLevel && !IncompatibleQuirks.Any(q => khs.Quirks.Contains(Core.GetQuirk(q)));

        public IEnumerable<HealthEffect> GetApplicableEffects(KerbalHealthStatus khs) => Effects.Where(effect => effect.IsApplicable(khs));

        public override bool Equals(object obj) => obj != null && obj is Quirk quirk && quirk.Name == Name;

        public override int GetHashCode() => Name.GetHashCode();

        public override string ToString() => $"{Title}\n{Description}";
    }
}
