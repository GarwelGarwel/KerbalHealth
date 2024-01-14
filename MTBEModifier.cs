using System;
using System.Collections.Generic;

namespace KerbalHealth
{
    /// <summary>
    /// Defines how the base chance of a condition or an outcome changes
    /// </summary>
    public class MTBEModifier
    {
        public const string ConfigNodeName = "MTBE_MODIFIER";

        public enum OperationType
        { 
            Multiply, 
            Add, 
            Power 
        };

        /// <summary>
        /// Type of modification: multiply the base chance, add to it or find power of it
        /// </summary>
        public OperationType Modification { get; set; } = OperationType.Multiply;

        /// <summary>
        /// Constant value by which the base chance is modified
        /// </summary>
        public double Value { get; set; } = 1;

        public string UseAttribute { get; set; } = null;

        /// <summary>
        /// Logic that defines if the modifier is applied
        /// </summary>
        public Logic Logic { get; set; } = new Logic();

        public void Load(ConfigNode node)
        {
            string modification = null;
            if (node.TryGetValue("modification", ref modification))
                Modification = (OperationType)Enum.Parse(typeof(OperationType), modification, true);
            Value = node.GetDouble("value", Modification == OperationType.Add ? 0 : 1);
            UseAttribute = node.GetString("useAttribute");
            Logic.Load(node);
        }

        public MTBEModifier(ConfigNode node) => Load(node);

        /// <summary>
        /// Applies all modifiers in the list to baseValue MTBE for PCM and returns resulting chance
        /// </summary>
        public static double Calculate(List<MTBEModifier> modifiers, double baseValue, ProtoCrewMember pcm)
        {
            double v = baseValue;
            for (int i = 0; i < modifiers.Count; i++)
                v = modifiers[i].Calculate(v, pcm);
            return v;
        }

        /// <summary>
        /// Returns the MTBE for PCM modified according to this modifier's rules
        /// </summary>
        public double Calculate(double baseValue, ProtoCrewMember pcm)
        {
            if (!Logic.Test(pcm))
                return baseValue;
            double v = Value;

            if (UseAttribute != null)
                switch (UseAttribute.ToLower())
                {
                    case "courage":
                        v *= pcm.courage;
                        break;

                    case "stupidity":
                        v *= pcm.stupidity;
                        break;
                }

            switch (Modification)
            {
                case OperationType.Multiply:
                    v *= baseValue;
                    break;

                case OperationType.Add:
                    v += baseValue;
                    break;

                case OperationType.Power:
                    v = Math.Pow(baseValue, v);
                    break;
            }

            if (v != baseValue)
                Core.Log($"MTBE for {pcm.name} changed from {baseValue:F0} to {v:F0}.");

            return v;
        }

        public override string ToString()
        {
            string res = "";
            switch (Modification)
            {
                case OperationType.Multiply:
                    res = "Multiply base MTBE by ";
                    break;

                case OperationType.Add:
                    res = "Increase base MTBE by ";
                    break;

                case OperationType.Power:
                    res = "Base MTBE's power of ";
                    break;
            }
            res += $"{Value}\nLogic: {Logic}";
            return res;
        }
    }
}
