using System.Collections.Generic;
using System.Linq;

namespace KerbalHealth
{
    /// <summary>
    /// List of all tracked kerbals
    /// </summary>
    public class KerbalHealthList : Dictionary<string, KerbalHealthStatus>
    {
        /// <summary>
        /// Adds a kerbal to the list, unless already exists
        /// </summary>
        /// <param name="name">Kerbal's name</param>
        /// <param name="health">Kerbal's current HP, maximum if skipped</param>
        public void Add(string name, double health = double.NaN)
        {
            if (ContainsKey(name)) return;
            Core.Log("Registering " + name + " with " + health + " health.", Core.LogLevel.Important);
            KerbalHealthStatus khs;
            if (double.IsNaN(health)) khs = new KerbalHealth.KerbalHealthStatus(name);
            else khs = new KerbalHealth.KerbalHealthStatus(name, health);
            Add(name, khs);
        }

        /// <summary>
        /// Adds a kerbal to the list, unless already exists
        /// </summary>
        /// <param name="khs"></param>
        public void Add(KerbalHealthStatus khs)
        {
            try { Add(khs.Name, khs); }
            catch (System.ArgumentException) { }
        }

        /// <summary>
        /// Scans all trackable kerbals and adds them to the list
        /// </summary>
        public void RegisterKerbals()
        {
            Core.Log("Registering kerbals...");
            KerbalRoster kerbalRoster = HighLogic.fetch.currentGame.CrewRoster;
            Core.Log("" + kerbalRoster.Count + " kerbals in CrewRoster: " + kerbalRoster.Crew.Count() + " crew, " + kerbalRoster.Tourist.Count() + " tourists.", Core.LogLevel.Important);
            foreach (KerbalHealthStatus khs in Values)
                if (!Core.IsKerbalTrackable(khs.PCM) && !khs.HasCondition("Frozen"))
                    Remove(khs.Name);
            List<ProtoCrewMember> list = kerbalRoster.Crew.Concat(kerbalRoster.Tourist).ToList();
            Core.Log(list.Count + " total kerbals in the roster.");
            foreach (ProtoCrewMember pcm in list)
                if (Core.IsKerbalTrackable(pcm)) Add(pcm.name, KerbalHealthStatus.GetMaxHP(pcm));
            Core.Log("KerbalHealthList updated: " + Count + " kerbals found.", Core.LogLevel.Important);
        }

        public void Update(double interval)
        {
            foreach (KerbalHealthStatus khs in Values)
            {
                ProtoCrewMember pcm = khs.PCM;
                if (khs.HasCondition("Frozen") || Core.IsKerbalTrackable(pcm))
                    khs.Update(interval);
                else
                {
                    Core.Log(khs.Name + " is not trackable anymore. Removing.");
                    Remove(khs.Name);
                }
            }
            //if (HighLogic.fetch.currentGame.CrewRoster.Crew.Count() + HighLogic.fetch.currentGame.CrewRoster.Tourist.Count() != Count) RegisterKerbals();
        }

        /// <summary>
        /// Checks all events for every trackable kerbal
        /// </summary>
        public void ProcessEvents()
        {
            foreach (KerbalHealthStatus khs in Values)
            {
                if (khs.HasCondition("Frozen") || !Core.IsKerbalTrackable(khs.PCM)) continue;
                Core.Log("Processing " + Core.Events.Count + " potential events for " + khs.Name + "...");
                foreach (Event e in Core.Events) e.Process(khs);
            }
        }

        /// <summary>
        /// Returns KerbalHealthStatus for a given kerbal
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public KerbalHealthStatus Find(string name)
        { return ContainsKey(name) ? this[name] : null; }

        /// <summary>
        /// Returns KerbalHealthStatus for a given kerbal
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public KerbalHealthStatus Find(ProtoCrewMember pcm)
        { return Find(pcm.name); }

        public KerbalHealthList() : base(HighLogic.fetch.currentGame.CrewRoster.Count)
        { }
    }
}
