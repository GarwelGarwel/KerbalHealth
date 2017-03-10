using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalHealth
{
    public class PanicAttackEvent : Event
    {
        public override string Name
        { get { return "PanicAttack"; } }

        public override string Message()
        {
            return khs.Name + " is having a panic attack!";
        }

        public override bool Condition()
        {
            return (khs.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) && (khs.Condition == KerbalHealthStatus.HealthCondition.OK);
        }

        public override double ChancePerDay()
        {
            return HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().PanicAttackChance * (1 - (khs.Health - Core.ExhaustionStartHealth) / (1 - Core.ExhaustionStartHealth)) * (1 - khs.PCM.courage); 
        }

        // Make inactive for up to 3 hours
        double inactionTime;
        public override void Run()
        {
            inactionTime = Core.rand.NextDouble() * HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthEventsSettings>().PanicAttackMaxDuration;
            Core.Log(khs.Name + " will be inactive for " + inactionTime + " seconds.");
            khs.PCM.SetInactive(inactionTime);
        }
    }
}
