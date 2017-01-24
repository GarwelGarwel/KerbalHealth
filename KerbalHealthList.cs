using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class KerbalHealthList : List<KerbalHealthStatus>
    {
        public void Add(string name, double health = -1)
        {
            Log.Post("Registering " + name + " with " + health + " health.");
            if (Contains(name))
            {
                Log.Post("Kerbal already registered.", Log.LogLevel.Warning);
                return;
            }
            KerbalHealthStatus khs;
            if (health == -1) khs = new KerbalHealth.KerbalHealthStatus(name); else khs = new KerbalHealth.KerbalHealthStatus(name, health);
            Add(khs);
        }

        public void Add(ProtoCrewMember pcm)
        {
            Log.Post("Trying to add " + pcm.name + " (" + pcm.type + ", " + pcm.rosterStatus + ").");
            if (IsKerbalTrackable(pcm)) Add(pcm.name, KerbalHealthStatus.GetMaxHP(pcm));
        }

        public void RegisterKerbals()
        {
            Log.Post("Registering kerbals...");
            KerbalRoster kerbalRoster = HighLogic.fetch.currentGame.CrewRoster;
            Log.Post("" + kerbalRoster.Count + " kerbals in CrewRoster: " + kerbalRoster.GetActiveCrewCount() + " active, " + kerbalRoster.GetAssignedCrewCount() + " assigned, " + kerbalRoster.GetAvailableCrewCount() + " available.");
            foreach (ProtoCrewMember pcm in kerbalRoster.Kerbals(ProtoCrewMember.KerbalType.Crew))
                Add(pcm);
            Log.Post("" + Count + " kerbal(s) registered.");
        }

        public bool Remove(string name)
        {
            Log.Post("Unregistering " + name + ".");
            foreach (KerbalHealthStatus khs in this)
                if (khs.Name == name)
                {
                    Remove(khs);
                    return true;
                }
            Log.Post("Failed to remove " + name + "!", Log.LogLevel.Error);
            return false;
        }

        public void Remove(ProtoCrewMember pcm)
        {
            Remove(pcm.name);
        }

        public void Update(double interval)
        {
            for (int i = 0; i < Count; i++)
            {
                KerbalHealthStatus khs = this[i];
                ProtoCrewMember pcm = khs.PCM;
                if (IsKerbalTrackable(pcm)) khs.Update(interval);
                else
                {
                    if (pcm != null) Log.Post(khs.Name + " (" + pcm.type + ", " + pcm.rosterStatus + ") is not trackable anymore. Removing.");
                    else Log.Post(khs.Name + " is not trackable anymore. Removing.");
                    RemoveAt(i);
                    i--;
                }

            }
            if (HighLogic.fetch.currentGame.CrewRoster.GetAssignedCrewCount() + HighLogic.fetch.currentGame.CrewRoster.GetAvailableCrewCount() != Count)
                RegisterKerbals();
        }

        bool IsKerbalTrackable(ProtoCrewMember pcm)
        {
            return (pcm != null) && ((pcm.type == ProtoCrewMember.KerbalType.Crew) || (pcm.type == ProtoCrewMember.KerbalType.Tourist)) && (pcm.rosterStatus != ProtoCrewMember.RosterStatus.Dead);
        }

        public KerbalHealthStatus Find(ProtoCrewMember pcm)
        {
            foreach (KerbalHealthStatus khs in this)
                if (khs.Name == pcm.name) return khs;
            return null;
        }

        public bool Contains(ProtoCrewMember pcm)
        {
            foreach (KerbalHealthStatus khs in this)
                if (khs.Name == pcm.name) return true;
            return false;
        }

        public bool Contains(string name)
        {
            foreach (KerbalHealthStatus khs in this)
                if (khs.Name == name) return true;
            return false;
        }
    }
}
