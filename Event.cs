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
        // 
        /// <summary>
        /// How to notify the user about the event. Values: Silent - no notification at all; ScreenMessage - a brief message displayed on screen; GameMessage - a message icon using the in-game message system.
        /// </summary>
        protected enum NotificationType { Silent, ScreenMessage, GameMessage };
        protected virtual NotificationType Notification
        {
            get
            {
                if (Core.UseMessageSystem) return NotificationType.GameMessage;
                else return NotificationType.ScreenMessage;
            }
        }

        /// <summary>
        /// Whether to stop timewarp the game on event. By default, stops timewarp only for assigned (active) kerbals and for non-silent events
        /// </summary>
        protected virtual bool UnwarpTime { get { return (Notification != NotificationType.Silent) && (khs.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned); } }

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
                if (msg != null)
                    switch (Notification)
                    {
                        case NotificationType.ScreenMessage: ScreenMessages.PostScreenMessage(msg); break;
                        case NotificationType.GameMessage: KSP.UI.Screens.MessageSystem.Instance.AddMessage(new KSP.UI.Screens.MessageSystem.Message("Kerbal Health", msg, KSP.UI.Screens.MessageSystemButton.MessageButtonColor.RED, KSP.UI.Screens.MessageSystemButton.ButtonIcons.ALERT)); break;
                        case NotificationType.Silent: break;
                    }
                if (UnwarpTime) TimeWarp.SetRate(0, false);
                Run();
            }
        }

        protected KerbalHealthStatus khs;
    }
}
