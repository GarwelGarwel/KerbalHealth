using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class FeelBadEvent : Event
    {
        public override string Name
        { get { return "FeelBad"; } }

        public override string Message()
        { return khs.Name + " is suddenly feeling bad."; }

        public override bool Condition()
        { return true; }

        public override double ChancePerDay()
        { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().FeelBadChance; }

        float MinDamage
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().FeelBadMinDamage; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().FeelBadMinDamage = value; }
        }

        float MaxDamage
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().FeelBadMaxDamage; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().FeelBadMaxDamage = value; }
        }

        //static double minDamage = 0.2, maxDamage = 0.5;  // Fraction of health that the kerbal loses
        double Damage(double x)
        {
            return 1 - MinDamage - (MaxDamage - MinDamage) * x;
        }

        public override void Run()
        {
            khs.HP *= Damage(Core.rand.NextDouble());
        }
    }
}
