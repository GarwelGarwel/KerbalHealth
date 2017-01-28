using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalHealth
{
    public abstract class HealthFactor
    {
        abstract public string Id { get; }

        virtual public string Name { get { return Id; } }

        // Should only be checked for loaded kerbals
        virtual public bool LoadedOnly { get { return true; } }

        // Returns factor's HP change per day as set in the Settings (use FactorSettings.[factorName])
        abstract public double BaseChangePerDay { get; }

        // Returns actual factor's HP change per day for a given kerbal, before factor multipliers
        abstract public double ChangePerDay(ProtoCrewMember pcm);

        public HealthFactor()
        { }
    }
}
