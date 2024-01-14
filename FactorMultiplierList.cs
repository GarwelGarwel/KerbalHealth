using System;
using System.Collections.Generic;
using System.Text;

namespace KerbalHealth
{
    public class FactorMultiplierList : List<FactorMultiplier>
    {
        public FactorMultiplier this[HealthFactor factor]
        {
            get => Find(factor) ?? new FactorMultiplier(factor);

            set
            {
                if (factor != value.Factor)
                {
                    Core.Log($"Failed assignment operation for factor multiplier for {value.FactorName}. Index factor is {factor.Name}.", LogLevel.Error);
                    throw new IndexOutOfRangeException();
                }

                for (int i = 0; i < Count; i++)
                    if (this[i].Factor == value.Factor)
                    {
                        this[i] = value;
                        return;
                    }
                base.Add(value);
            }
        }

        public FactorMultiplierList()
        {
            for (int i = 0; i < Core.Factors.Count; i++)
                base.Add(new FactorMultiplier(Core.Factors[i]));
            base.Add(new FactorMultiplier());
        }

        public FactorMultiplierList(FactorMultiplierList list)
            : base(list)
        { }

        public new void Add(FactorMultiplier factorMultiplier)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].Factor == factorMultiplier.Factor)
                {
                    this[i].CombineWith(factorMultiplier);
                    return;
                }
            base.Add(factorMultiplier);
        }

        public FactorMultiplier Find(HealthFactor factor) => Find(fm => fm.Factor == factor);

        public double GetMultiplier(HealthFactor healthFactor) => this[healthFactor].Multiplier * this[null].Multiplier;

        public Dictionary<HealthFactor, double> ApplyToFactors(Dictionary<HealthFactor, double> factors)
        {
            Dictionary<HealthFactor, double> res = new Dictionary<HealthFactor, double>(factors);
            double allMultiplier = this[null].Multiplier;
            foreach (KeyValuePair<HealthFactor, double> kvp in factors)
                res[kvp.Key] *= this[kvp.Key].Multiplier * allMultiplier;
            return res;
        }

        public FactorMultiplierList CombineWith(FactorMultiplierList list)
        {
            for (int i = 0; i < list.Count; i++)
                this[list[i].Factor]?.CombineWith(list[i]);
            return this;
        }

        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            for (int i = 0; i < Count; i++)
                if (!this[i].IsTrivial)
                    res.AppendLine(this[i].ToString());
            return res.ToStringAndRelease();
        }
    }
}
