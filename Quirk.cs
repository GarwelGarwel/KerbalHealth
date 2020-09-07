using System.Collections.Generic;
using System.Linq;

namespace KerbalHealth
{
    /// <summary>
    /// Describes a health quirk
    /// </summary>
    public class Quirk
    {
        public string Name { get; set; }

        string title;
        public string Title
        {
            get => title ?? Name;
            set => title = value;
        }

        public string Description { get; set; }

        public bool IsVisible { get; set; } = true;
        public int MinLevel { get; set; } = 0;
        public List<string> IncompatibleQuirks { get; set; } = new List<string>();
        public double CourageWeight { get; set; } = 1;
        public double StupidityWeight { get; set; } = 1;

        public List<HealthEffect> Effects { get; set; } = new List<HealthEffect>();

        /// <summary>
        /// Returns true if this quirk can be assigned to the given kerbal at a certain experience level
        /// </summary>
        /// <param name="khs"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool IsAvailableTo(KerbalHealthStatus khs, int level) =>
            level >= MinLevel && !IncompatibleQuirks.Any(q => khs.Quirks.Contains(Core.GetQuirk(q)));

        /// <summary>
        /// Applies valid effects of this quirk to the given kerbal's HealthModifierSet
        /// </summary>
        /// <param name="khs"></param>
        /// <param name="hms"></param>
        public void Apply(KerbalHealthStatus khs, HealthModifierSet hms)
        {
            Core.Log($"Applying {Name} quirk to {khs.Name}.");
            foreach (HealthEffect eff in Effects.Where(eff => eff.IsApplicable(khs)))
                eff.Apply(hms);
        }

        public override bool Equals(object obj) => (obj != null) && (obj is Quirk quirk) && (quirk.Name == Name);

        public override int GetHashCode() => Name.GetHashCode();

        public override string ToString()
        {
            string res = $"{Title}.";
            if (!string.IsNullOrEmpty(Description))
                res += $"\n{Description}";
            if (Effects.Count == 1)
                res += $"\nEffect: {Effects[0]}";
            if (Effects.Count > 1)
            {
                res += "\nEffects:";
                foreach (HealthEffect he in Effects)
                    res += $"\n{he}";
            }
            return res;
        }

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
            Effects = new List<HealthEffect>(node.GetNodes("EFFECT").Select(n => new HealthEffect(n)));
            Core.Log("Quirk loaded: " + this);
        }

        public Quirk(string name)
        {
            Name = name;
            IsVisible = false;
        }
    }
}
