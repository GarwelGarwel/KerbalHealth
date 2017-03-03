using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public abstract class Event
    {
        // How to notify the user about the event
        protected enum NotificationType { Silent, ScreenMessage, GameMessage };
        protected virtual NotificationType Notification { get { return NotificationType.GameMessage; } }

        // Whether to stop timewrap the game on event
        protected virtual bool UnwarpTime { get { return khs.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned; } }

        // Returns system name of the event
        public abstract string Name { get; }

        // Returns human-readable title of the event
        public virtual string Title
        { get { return Name; } }

        // Returns the on-screen message when the event happens (null if no message)
        public abstract string Message();

        // Returns true if the event can happen to this kerbal at the moment
        public abstract bool Condition();

        // Returns chance (0 to 1) of the event happening per day
        public abstract double ChancePerDay();

        // Affects the kerbal's health
        public abstract void Run();

        // Check condition and chance, run the event and display the message. To be called once a day.
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

        //public Event() { }

        //public Event(KerbalHealthStatus khs)
        //{
        //    this.khs = khs;
        //}
    }
}
