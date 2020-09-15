using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalHealth
{
    public class FactorMultiplierList : List<FactorMultiplier>
    {
        public new FactorMultiplier Add(FactorMultiplier factorMultiplier)
        {
            if (Find(factorMultiplier.Factor) == null)
                Add(factorMultiplier);
            return factorMultiplier;
        }

        public FactorMultiplier Find(HealthFactor factor) => Find(fm => fm.Factor == factor);

        public FactorMultiplier this[HealthFactor factor]
        {
            get => Find(fm => fm.Factor == factor) ?? new FactorMultiplier(factor);
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
                Add(value);
            }
        }

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
            foreach (FactorMultiplier fm in list)
                this[fm.Factor].CombineWith(fm);
            return this;
        }

        public FactorMultiplierList()
        {
            foreach (HealthFactor f in Core.Factors)
                base.Add(new FactorMultiplier(f));
            base.Add(new FactorMultiplier());
        }

        public FactorMultiplierList(FactorMultiplierList list)
            : base(list)
        { }
    }
}
