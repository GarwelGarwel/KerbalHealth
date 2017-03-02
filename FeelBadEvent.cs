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

        public override string Message(KerbalHealthStatus khs)
        { return khs.Name + " is suddenly feeling bad."; }

        public override bool Condition(KerbalHealthStatus khs)
        { return true; }

        public override double ChancePerDay(KerbalHealthStatus khs)
        { return 0.1; }

        static double minDamage = 0, maxDamage = 20;
        double Damage(double x)
        {
            return minDamage + (maxDamage - minDamage) * x;
        }

        public override void Run(KerbalHealthStatus khs)
        {
            khs.HP -= Damage(Core.rand.NextDouble());
        }
    }
}
