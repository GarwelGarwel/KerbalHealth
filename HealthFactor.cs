using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalHealth
{
    public abstract class HealthFactor
    {
        /// <summary>
        /// Internal name of the factor
        /// </summary>
        abstract public string Name { get; }

        /// <summary>
        /// Display name of the factor
        /// </summary>
        virtual public string Title { get { return Name; } }

        /// <summary>
        /// This factor can/should be cached for unloaded kerbals
        /// </summary>
        virtual public bool Cachable { get { return true; } }

        /// <summary>
        /// Is the factor considered when calculating estimated HP change in Health Report
        /// </summary>
        //public bool EnabledInEditor { get; set; }
        bool enabledInEditor = true;
        public bool IsEnabledInEditor() { return enabledInEditor; }
        public void SetEnabledInEditor(bool state) { enabledInEditor = state; }
        virtual public void ResetEnabledInEditor() { SetEnabledInEditor(true); }

        /// <summary>
        /// Returns factor's HP change per day as set in the Settings (use KerbalHealthFactorsSettings.[factorName])
        /// </summary>
        abstract public double BaseChangePerDay { get; }

        /// <summary>
        /// Returns actual factor's HP change per day for a given kerbal, before factor multipliers
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        abstract public double ChangePerDay(ProtoCrewMember pcm);

        public HealthFactor() { ResetEnabledInEditor(); }
    }
}
