using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    /// <summary>
    /// Random health event class. To add a new event, extend this class and add it to Core.Events
    /// </summary>
    public abstract class Event
    {
        /// <summary>
        /// Tells whether to skip notification about the event
        /// </summary>
        protected virtual bool IsSilent
        { get { return false; } }

        /// <summary>
        /// Returns system name of the event
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Returns human-readable title of the event. Same as Name by default
        /// </summary>
        public virtual string Title
        { get { return Name; } }

        /// <summary>
        /// Returns the message text when the event happens (null if no message)
        /// </summary>
        /// <returns></returns>
        public virtual string Message()
        { return Title + " event has happened to " + khs.Name + "."; }

        /// <summary>
        /// Returns true if the event can happen to this kerbal at the moment
        /// </summary>
        /// <returns></returns>
        public abstract bool Condition();

        /// <summary>
        /// Returns chance (0 to 1) of the event happening per day
        /// </summary>
        /// <returns></returns>
        public abstract double ChancePerDay();

        /// <summary>
        /// Applies the event's effects to the current kerbal
        /// </summary>
        protected abstract void Run();

        /// <summary>
        /// Checks condition and chance, runs the event and displays the message (if applicable). To be called once a day for every event class, for every kerbal
        /// </summary>
        /// <param name="status">Kerbal to process the event for</param>
        public void Process(KerbalHealthStatus status)
        {
            khs = status;
            if (Condition() && (Core.rand.NextDouble() < ChancePerDay()))
            {
                Core.Log(Name + " event has fired for " + khs.Name + ".", Core.LogLevel.Important);
                string msg = Message();
                if ((msg != null) && !IsSilent) Core.ShowMessage(msg, khs.PCM);
                Run();
            }
        }

        protected KerbalHealthStatus khs;
    }
}
