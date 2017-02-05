using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class KerbalHealthList : List<KerbalHealthStatus>
    {
        public void Add(string name, double health = double.NaN)
        {
            Core.Log("Registering " + name + " with " + health + " health.");
            if (Contains(name))
            {
                Core.Log(name + " already registered.", Core.LogLevel.Important);
                return;
            }
            KerbalHealthStatus khs;
            if (double.IsNaN(health)) khs = new KerbalHealth.KerbalHealthStatus(name);
            else khs = new KerbalHealth.KerbalHealthStatus(name, health);
            Add(khs);
        }

        public void Add(ProtoCrewMember pcm)
        {
            Core.Log("Trying to add " + pcm.name + " (" + pcm.type + ", " + pcm.rosterStatus + ").");
            if (IsKerbalTrackable(pcm)) Add(pcm.name, KerbalHealthStatus.GetMaxHP(pcm));
        }

        public void RegisterKerbals()
        {
            Core.Log("Registering kerbals...");
            KerbalRoster kerbalRoster = HighLogic.fetch.currentGame.CrewRoster;
            Core.Log("" + kerbalRoster.Count + " kerbals in CrewRoster: " + kerbalRoster.GetActiveCrewCount() + " active, " + kerbalRoster.GetAssignedCrewCount() + " assigned, " + kerbalRoster.GetAvailableCrewCount() + " available.", Core.LogLevel.Important);
            foreach (ProtoCrewMember pcm in kerbalRoster.Crew.Concat(kerbalRoster.Tourist)) Add(pcm);
            Core.Log("" + Count + " kerbal(s) registered.", Core.LogLevel.Important);
        }

        public bool Remove(string name)
        {
            Core.Log("Unregistering " + name + ".", Core.LogLevel.Important);
            foreach (KerbalHealthStatus khs in this)
                if (khs.Name == name)
                {
                    Remove(khs);
                    return true;
                }
            Core.Log("Failed to remove " + name + "!", Core.LogLevel.Error);
            return false;
        }

        public void Remove(ProtoCrewMember pcm)
        { Remove(pcm.name); }

        public void Update(double interval)
        {
            for (int i = 0; i < Count; i++)
            {
                KerbalHealthStatus khs = this[i];
                ProtoCrewMember pcm = khs.PCM;
                if (IsKerbalTrackable(pcm)) khs.Update(interval);
                else
                {
                    if (pcm != null) Core.Log(khs.Name + " (" + pcm.type + ", " + pcm.rosterStatus + ") is not trackable anymore. Removing.");
                    else Core.Log(khs.Name + " is not trackable anymore. Removing.");
                    RemoveAt(i);
                    i--;
                }

            }
            if (HighLogic.fetch.currentGame.CrewRoster.GetAssignedCrewCount() + HighLogic.fetch.currentGame.CrewRoster.GetAvailableCrewCount() != Count)
                RegisterKerbals();
        }

        bool IsKerbalTrackable(ProtoCrewMember pcm)
        {
            return (pcm != null) && ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available));
        }

        public KerbalHealthStatus Find(ProtoCrewMember pcm)
        {
            foreach (KerbalHealthStatus khs in this) if (khs.Name == pcm.name) return khs;
            return null;
        }

        public bool Contains(string name)
        {
            foreach (KerbalHealthStatus khs in this) if (khs.Name == name) return true;
            return false;
        }

        public bool Contains(ProtoCrewMember pcm)
        {
            return Contains(pcm.name);
        }
    }
}
