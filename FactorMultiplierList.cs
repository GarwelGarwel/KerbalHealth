using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace KerbalHealth
{
    class FactorMultiplierList : List<FactorMultiplier>
    {
        public bool Contains(string factorName) => Exists(fm => fm.FactorName.Equals(factorName, System.StringComparison.OrdinalIgnoreCase));

        public new FactorMultiplier Add(FactorMultiplier factorMultiplier)
        {
            FactorMultiplier fm = Find(factorMultiplier.FactorName);
            if (fm == null)
                Add(factorMultiplier);
            else fm = factorMultiplier;
            return factorMultiplier;
        }

        public FactorMultiplier Find(string factorName) =>
            Find(fm => fm.FactorName == factorName || (fm.FactorName == null && factorName.Equals("All", StringComparison.OrdinalIgnoreCase)));

        public FactorMultiplierList()
        {
            foreach (HealthFactor f in Core.Factors)
                base.Add(new FactorMultiplier(f.Name));
            base.Add(new FactorMultiplier());
        }

        public FactorMultiplierList(FactorMultiplierList list)
            : base(list)
        { }
    }
}
