using System;

namespace KerbalHealth
{
    public class LoseImmunityEvent : Event
    {
        public override string Name => "LoseImmunity";

        protected override bool IsSilent => true;

        public override bool Condition() => Core.SicknessEnabled && khs.HasCondition("Immune");

        public override double ChancePerDay()
        {
            double c = Math.Min(1 / HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().ImmunityDuration, 1);
            if (Core.QuirksEnabled)
                foreach (Quirk q in khs.Quirks)
                    foreach (HealthEffect he in q.Effects)
                        if (he.IsApplicable(khs)) c *= he.LoseImmunityChance;
            return c;
        }

        protected override void Run()
        {
            Core.Log("Removing immunity from " + khs.Name + "...");
            khs.RemoveCondition("Immune");
        }
    }
}
