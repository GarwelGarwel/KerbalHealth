using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    class FeelBadEvent : Event
    {
        public override string Name
        { get { return "FeelBad"; } }

        public override string Message(KerbalHealthStatus khs)
        { return khs.Name + " is suddenly feeling bad."; }

        public override bool Condition(KerbalHealthStatus khs)
        { return true; }

        public override double ChancePerDay(KerbalHealthStatus khs)
        { return 0.01; }

        public override void Run(KerbalHealthStatus khs)
        {
            khs.HP -= rand.NextDouble() * 10;
        }
    }
}
