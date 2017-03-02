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

        public override string Message(KerbalHealthStatus khs)
        {
            return khs.Name + " is having a panic attack!";
        }

        public override bool Condition(KerbalHealthStatus khs)
        {
            return (khs.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) && (khs.Condition == KerbalHealthStatus.HealthCondition.OK) && (khs.Health < 0.5);
        }

        static double avgChancePerDay = 0.1;
        public override double ChancePerDay(KerbalHealthStatus khs)
        {
            return avgChancePerDay * (0.5 - khs.Health) / 0.5 * (1 - khs.PCM.courage);
        }

        public override void Run(KerbalHealthStatus khs)
        {
            //Core.Log(khs.Name + " is having a panic attack and is being disabled for 1 hour from " + KSPUtil.PrintTimeCompact(Planetarium.GetUniversalTime(), true));
            khs.PCM.SetInactive(3600);
        }
    }
}
