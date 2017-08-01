using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class LoseImmunityEvent : Event
    {
        public override string Name
        { get { return "LoseImmunity"; } }

        protected override bool IsSilent
        { get { return true; } }

        public override bool Condition()
        { return Core.SicknessEnabled && khs.HasCondition("Immune"); }

        public override double ChancePerDay()
        { return Math.Min(1 / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().ImmunityDuration, 1); }

        protected override void Run()
        {
            Core.Log("Removing immunity from " + khs.Name + "...");
            khs.RemoveCondition("Immune");
        }
    }
}
