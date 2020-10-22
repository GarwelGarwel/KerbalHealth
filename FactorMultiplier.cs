using System;

namespace KerbalHealth
{
    public class FactorMultiplier
    {
        internal const string ConfigNodeName = "FACTOR_MULTIPLIER";

        public string FactorName
        {
            get => Factor?.Name ?? "All";
            set => Factor = Core.GetHealthFactor(value);
        }

        public HealthFactor Factor { get; set; }

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

        public FactorMultiplier(HealthFactor factor = null) => Factor = factor;

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

        public FactorMultiplier CombineWith(FactorMultiplier fm)
        {
            if (Factor != fm.Factor)
            {
                Core.Log($"Could not combine {FactorName} and {fm.FactorName} multipliers.", LogLevel.Error);
                return this;
            }
            BonusSum += fm.BonusSum;
            FreeMultiplier *= fm.FreeMultiplier;
            MinMultiplier = Math.Min(MinMultiplier, fm.MinMultiplier);
            MaxMultiplier = Math.Max(MaxMultiplier, fm.MaxMultiplier);
            return this;
        }

        /// <summary>
        /// Combines two factor multipliers into one, adding bonus sums and multiplying their multipliers
        /// </summary>
        /// <param name="fm2"></param>
        /// <returns></returns>
        public static FactorMultiplier Combine(FactorMultiplier fm1, FactorMultiplier fm2)
        {
            FactorMultiplier res = new FactorMultiplier(fm1.Factor);
            if (fm1.Factor != fm2.Factor)
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
                : $"{Factor?.Name ?? "All factors "} {Multiplier:P1} (bonus sum: {BonusSum}; free multiplier: {FreeMultiplier}: multipliers {MinMultiplier}..{MaxMultiplier})";
    }
}
