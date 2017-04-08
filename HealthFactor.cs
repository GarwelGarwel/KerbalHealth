using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalHealth
{
    public abstract class HealthFactor
    {
        abstract public string Name { get; }

        // Display name of the factor
        virtual public string Title { get { return Name; } }

        // This factor can/should be cached for unloaded kerbals
        virtual public bool Cachable { get { return true; } }

        // Returns factor's HP change per day as set in the Settings (use FactorSettings.[factorName])
        abstract public double BaseChangePerDay { get; }

        // Returns actual factor's HP change per day for a given kerbal, before factor multipliers
        abstract public double ChangePerDay(ProtoCrewMember pcm);
    }
}
