using Expansions.Missions;
using System;

namespace KerbalHealth
{
    public class FactorMultiplier
    {
        internal const string ConfigNodeName = "FACTOR_MULTIPLIER";

        public string FactorName { get; set; }

        public HealthFactor Factor
        {
            get => Core.GetHealthFactor(FactorName);
            set => FactorName = value.Name;
        }

        public double BonusSum { get; set; } = 0;

        public double FreeMultiplier { get; set; } = 1;

        public double MinMultiplier { get; set; } = 1;

        public double MaxMultiplier { get; set; } = 1;

        /// <summary>
        /// Final multiplier for this factor
        /// </summary>
        public double Multiplier => UtilMath.Clamp(FreeMultiplier * (1 - BonusSum), MinMultiplier, MaxMultiplier);

        public bool IsTrivial => BonusSum == 0 && FreeMultiplier == 1;

        public ConfigNode ConfigNode
        {
            get
            {
                if (IsTrivial)
                    return null;
                ConfigNode node = new ConfigNode(ConfigNodeName);
                if (Factor != null)
                    node.AddValue("factor", FactorName);
                if (BonusSum != 0)
                    node.AddValue("bonusSum", BonusSum);
                if (FreeMultiplier != 1)
                    node.AddValue("multiplier", FreeMultiplier);
                if (MinMultiplier < 1)
                    node.AddValue("minMultiplier", MinMultiplier);
                if (MaxMultiplier > 1)
                    node.AddValue("maxMultiplier", MaxMultiplier);
                return node;
            }
            set
            {
                FactorName = value.GetString("factor");
                BonusSum = value.GetDouble("bonusSum");
                FreeMultiplier = value.GetDouble("multiplier", 1);
                MinMultiplier = value.GetDouble("minMultiplier", 1);
                MaxMultiplier = value.GetDouble("maxMultiplier", 1);
            }
        }

        public FactorMultiplier(string factor = null, double multiplier = 1)
        {
            if (!string.IsNullOrEmpty(factor) && !factor.Equals("All", System.StringComparison.OrdinalIgnoreCase))
                FactorName = factor;
            AddFreeMultiplier(multiplier);
        }

        public FactorMultiplier(ConfigNode configNode) => ConfigNode = configNode;

        /// <summary>
        /// Adds a free (i.e. not restricted by crew cap) multiplier
        /// </summary>
        /// <param name="v"></param>
        public void AddFreeMultiplier(double v)
        {
            FreeMultiplier *= v;
            if (v < MinMultiplier)
                MinMultiplier = v;
            else if (v > MaxMultiplier)
                MaxMultiplier = v;
        }

        public void AddRestrictedMultiplier(double multiplier, int crewCap, int crew)
        {
            BonusSum += Math.Abs(1 - multiplier) * crewCap / crew;
            if (multiplier < MinMultiplier)
                MinMultiplier = multiplier;
            if (multiplier > MaxMultiplier)
                MaxMultiplier = multiplier;
        }

        /// <summary>
        /// Combines two factor multipliers into one, adding bonus sums and multiplying their multipliers
        /// </summary>
        /// <param name="fm2"></param>
        /// <returns></returns>
        public static FactorMultiplier Combine(FactorMultiplier fm1, FactorMultiplier fm2)
        {
            FactorMultiplier res = new FactorMultiplier(fm1.FactorName);
            if (fm1.FactorName != fm2.FactorName)
            {
                Core.Log($"Could not combine {fm1.FactorName} and {fm2.FactorName} multipliers.", LogLevel.Error);
                return res;
            }
            res.BonusSum = fm1.BonusSum + fm2.BonusSum;
            res.FreeMultiplier = fm1.FreeMultiplier * fm2.FreeMultiplier;
            res.MinMultiplier = Math.Min(fm1.MinMultiplier, fm2.MinMultiplier);
            res.MaxMultiplier = Math.Max(fm1.MaxMultiplier, fm2.MaxMultiplier);
            return res;
        }

        public override string ToString() =>
            IsTrivial
                ? ""
                : $"{Factor?.Name ?? "All factors "} x{Multiplier:P1} (bonus sum: {BonusSum}; free multiplier: {FreeMultiplier}: multipliers {MinMultiplier}..{MaxMultiplier}";
    }
}
