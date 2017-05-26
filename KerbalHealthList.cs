using System.Collections.Generic;
using System.Linq;

namespace KerbalHealth
{
    /// <summary>
    /// List of all tracked kerbals
    /// </summary>
    public class KerbalHealthList : List<KerbalHealthStatus>
    {
        /// <summary>
        /// Adds a kerbal to the list, unless already exists
        /// </summary>
        /// <param name="name">Kerbal's name</param>
        /// <param name="health">Kerbal's current HP, maximum if skipped</param>
        public void Add(string name, double health = double.NaN)
        {
            if (Contains(name))
            {
                Core.Log(name + " already registered.");
                return;
            } else Core.Log("Registering " + name + " with " + health + " health.", Core.LogLevel.Important);
            KerbalHealthStatus khs;
            if (double.IsNaN(health)) khs = new KerbalHealth.KerbalHealthStatus(name);
            else khs = new KerbalHealth.KerbalHealthStatus(name, health);
            Add(khs);
        }

        /// <summary>
        /// Scans all trackable kerbals and adds them to the list
        /// </summary>
        public void RegisterKerbals()
        {
            Core.Log("Registering kerbals...");
            KerbalRoster kerbalRoster = HighLogic.fetch.currentGame.CrewRoster;
            Core.Log("" + kerbalRoster.Count + " kerbals in CrewRoster: " + HighLogic.fetch.currentGame.CrewRoster.Crew.Count() + " crew, " + HighLogic.fetch.currentGame.CrewRoster.Tourist.Count() + " tourists.", Core.LogLevel.Important);
            foreach (ProtoCrewMember pcm in kerbalRoster.Crew.Concat(kerbalRoster.Tourist))
                if (Core.IsKerbalTrackable(pcm)) Add(pcm.name, KerbalHealthStatus.GetMaxHP(pcm));
                else Core.Log(pcm?.name + " is not trackable (" + (pcm == null ? "is null" : ("status: " + pcm.rosterStatus)) + ").");
            Core.Log("" + Count + " kerbal(s) processed.", Core.LogLevel.Important);
        }

        public void Update(double interval)
        {
            for (int i = 0; i < Count; i++)
            {
                KerbalHealthStatus khs = this[i];
                ProtoCrewMember pcm = khs.PCM;
                if (!Core.IsKerbalFrozen(pcm.name))
                    if (Core.IsKerbalTrackable(pcm))
                        khs.Update(interval);
                    else
                    {
                        Core.Log(khs.Name + " is not trackable anymore. Removing.");
                        //Core.Log("DFWrapper is " + (DFWrapper.APIReady ? ("ready. " + DFWrapper.DeepFreezeAPI.FrozenKerbalsList.Count + " items in FrozenKerbalsList.") : "not ready."));
                        RemoveAt(i);
                        i--;
                    }
                else Core.Log(khs.Name + " is frozen, skipping update.");
            }
            if (HighLogic.fetch.currentGame.CrewRoster.Crew.Count() + HighLogic.fetch.currentGame.CrewRoster.Tourist.Count() != Count) RegisterKerbals();
        }

        /// <summary>
        /// Checks all events for every trackable kerbal
        /// </summary>
        public void ProcessEvents()
        {
            foreach (KerbalHealthStatus khs in this)
            {
                if (!Core.IsKerbalTrackable(khs.PCM) || Core.IsKerbalFrozen(khs.Name)) continue;
                Core.Log("Processing " + Core.Events.Count + " potential events for " + khs.Name + "...");
                foreach (Event e in Core.Events) e.Process(khs);
            }
        }

        /// <summary>
        /// Returns KerbalHealthStatus for a given kerbal
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public KerbalHealthStatus Find(ProtoCrewMember pcm)
        {
            foreach (KerbalHealthStatus khs in this) if (khs.Name == pcm.name) return khs;
            return null;
        }

        /// <summary>
        /// Returns true if a kerbal with this name exists in the list
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string name)
        {
            foreach (KerbalHealthStatus khs in this) if (khs.Name == name) return true;
            return false;
        }
    }
}
