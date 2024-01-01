using KSP.Localization;
using System;

namespace KerbalHealth
{
    public class FactorMultiplier : IConfigNode
    {
        internal const string ConfigNodeName = "FACTOR_MULTIPLIER";

        double freeMultiplier = 1;

        public string FactorName
        {
            get => Factor?.Name ?? "All";
            set => Factor = Core.GetHealthFactor(value);
        }

        public HealthFactor Factor { get; set; }

        public double BonusSum { get; set; } = 0;

        public double FreeMultiplier
        {
            get => freeMultiplier;
            set
            {
                freeMultiplier = value;
                if (MinMultiplier > value)
                    MinMultiplier = value;
                else if (MaxMultiplier < value)
                    MaxMultiplier = value;
            }
        }

        public double MinMultiplier { get; set; } = 1;

        public double MaxMultiplier { get; set; } = 1;

        /// <summary>
        /// Final multiplier for this factor
        /// </summary>
        public double Multiplier => UtilMath.Clamp(FreeMultiplier * (1 - BonusSum), MinMultiplier, MaxMultiplier);

        public bool IsTrivial =>
            BonusSum == 0
            && FreeMultiplier == 1
            && MinMultiplier >= 1
            && MaxMultiplier <= 1;

        public void Save(ConfigNode node)
        {
            if (Factor != null)
                node.AddValue("multiplyFactor", FactorName);
            if (BonusSum != 0)
                node.AddValue("bonusSum", BonusSum);
            if (FreeMultiplier != 1)
                node.AddValue("multiplier", FreeMultiplier);
            if (MinMultiplier < 1)
                node.AddValue("minMultiplier", MinMultiplier);
            if (MaxMultiplier > 1)
                node.AddValue("maxMultiplier", MaxMultiplier);
        }

        public void Load(ConfigNode node)
        {
            FactorName = node.GetString("multiplyFactor");
            BonusSum = node.GetDouble("bonusSum");
            MinMultiplier = node.GetDouble("minMultiplier", 1);
            MaxMultiplier = node.GetDouble("maxMultiplier", 1);
            FreeMultiplier = node.GetDouble("multiplier", 1);
        }

        public FactorMultiplier(HealthFactor factor = null) => Factor = factor;

        public FactorMultiplier(ConfigNode configNode) => Load(configNode);

        /// <summary>
        /// Combines two factor multipliers into one, adding bonus sums and multiplying their multipliers
        /// </summary>
        /// <param name="fm2"></param>
        /// <returns></returns>
        public static FactorMultiplier Combine(FactorMultiplier fm1, FactorMultiplier fm2) => new FactorMultiplier(fm1.Factor).CombineWith(fm2);

        /// <summary>
        /// Adds a free (i.e. not restricted by crew cap) multiplier
        /// </summary>
        /// <param name="multiplier"></param>
        public void AddMultiplier(double multiplier)
        {
            FreeMultiplier *= multiplier;
            if (multiplier < MinMultiplier)
                MinMultiplier = multiplier;
            else if (multiplier > MaxMultiplier)
                MaxMultiplier = multiplier;
        }

        public void AddMultiplier(double multiplier, int crewCap, int crew)
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
            MinMultiplier = Math.Min(MinMultiplier, fm.MinMultiplier);
            MaxMultiplier = Math.Max(MaxMultiplier, fm.MaxMultiplier);
            FreeMultiplier *= fm.FreeMultiplier;
            return this;
        }

        public override string ToString() =>
            IsTrivial
                ? string.Empty
                : Localizer.Format("#KH_FactorMultiplier_desc", Factor?.Name ?? Localizer.Format("#KH_FactorMultiplier_All"), Multiplier.ToString("P0"));
    }
}
