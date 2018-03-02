using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public bool IsAvailableTo(KerbalHealthStatus khs, int level)
        {
            if (level < MinLevel) return false;
            foreach (string q in IncompatibleQuirks)
                if (khs.Quirks.Contains(Core.GetQuirk(q))) return false;
            return true;
        }

        /// <summary>
        /// Returns true if this quirk can be assigned to the given kerbal at a his/her current experience level
        /// </summary>
        /// <param name="khs"></param>
        /// <returns></returns>
        public bool IsAvailableTo(KerbalHealthStatus khs) => IsAvailableTo(khs, khs.PCM.experienceLevel);

        public void Apply(KerbalHealthStatus khs)
        {
            Core.Log("Applying " + Name + " quirk to " + khs.Name + ".");
            foreach (HealthEffect eff in Effects)
            {
                Core.Log("Applying effect: " + eff);
                eff.Apply(khs);
            }
        }

        public override bool Equals(object obj) => (obj is Quirk) && (obj != null) && (((Quirk)obj).Name == Name);
        public override int GetHashCode() => Name.GetHashCode();

        public override string ToString()
        {
            string res = Title;
            if ((Description != null) && (Description != "")) res += "\n" + Description;
            foreach (HealthEffect he in Effects)
                res += "\n" + he;
            return res;
        }

        public Quirk(ConfigNode node)
        {
            Name = node.GetValue("name");
            Title = node.GetValue("title");
            Description = node.GetValue("description");
            IsVisible = Core.GetBool(node, "visible", true);
            MinLevel = Core.GetInt(node, "minLevel");
            foreach (string t in node.GetValues("incompatibleWith"))
                IncompatibleQuirks.Add(t);
            CourageWeight = Core.GetDouble(node, "courageWeight", 1);
            StupidityWeight = Core.GetDouble(node, "stupidityWeight", 1);
            Effects = new List<HealthEffect>();
            foreach (ConfigNode n in node.GetNodes("EFFECT"))
                Effects.Add(new HealthEffect(n));
            Core.Log("Quirk loaded: " + this);
        }

        public Quirk(string name)
        {
            Name = name;
            IsVisible = false;
        }
    }
}
