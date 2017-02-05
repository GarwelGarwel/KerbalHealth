using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public abstract class Event
    {
        protected static Random rand = new Random();

        // Returns system name of the event
        public abstract string Name { get; }

        // Returns human-readable title of the event
        public virtual string Title
        { get { return Name; } }

        // Returns the on-screen message when the event happens (null if no message)
        public abstract string Message(KerbalHealthStatus khs);

        // Returns true if the event can happen to this kerbal at the moment
        public abstract bool Condition(KerbalHealthStatus khs);

        // Returns chance (0 to 1) of the event happening per day
        public abstract double ChancePerDay(KerbalHealthStatus khs);

        // Affects the kerbal's health
        public abstract void Run(KerbalHealthStatus khs);

        // Check condition and chance, run the event and display the message. To be called once a day.
        public void Process(KerbalHealthStatus khs)
        {
            if (Condition(khs) && (rand.NextDouble() < ChancePerDay(khs)))
            {
                Core.Log(Name + " event has fired.");
                string msg = Message(khs);
                if (msg != null) ScreenMessages.PostScreenMessage(msg);
                Run(khs);
            }
        }
    }
}
