using System;
using System.Collections.Generic;

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
        public void Add(string name)
        {
            if (ContainsKey(name))
                return;
            Core.Log("Registering " + name + ".", LogLevel.Important);
            Add(name, new KerbalHealthStatus(name));
        }

        /// <summary>
        /// Adds a kerbal to the list, unless already exists
        /// </summary>
        /// <param name="khs"></param>
        public void Add(KerbalHealthStatus khs)
        {
            if (!ContainsKey(khs.Name))
                Add(khs.Name, khs);
        }

        /// <summary>
        /// Changes name of a registered kerbal and renames the entry
        /// </summary>
        /// <param name="name1"></param>
        /// <param name="name2"></param>
        public void Rename(string name1, string name2)
        {
            Core.Log("KerbalHealthList.Rename('" + name1 + "', '" + name2 + "')");
            if (name1 == name2)
                return;
            if (ContainsKey(name1))
            {
                this[name1].Name = name2;
                Add(name2, this[name1]);
                Remove(name1);
            }
            else Core.Log("Could not find '" + name1 + "'.", LogLevel.Error);
        }

        /// <summary>
        /// Scans all trackable kerbals and adds them to the list
        /// </summary>
        public void RegisterKerbals()
        {
            Core.Log("Registering kerbals...");
            RemoveUntrackable();
            KerbalRoster kerbalRoster = HighLogic.fetch.currentGame.CrewRoster;
            List<ProtoCrewMember> list = new List<ProtoCrewMember>(kerbalRoster.Crew);
            list.AddRange(kerbalRoster.Tourist);
            Core.Log(list.Count + " total trackable kerbals.", LogLevel.Important);
            foreach (ProtoCrewMember pcm in list)
                if (Core.IsKerbalTrackable(pcm))
                    Add(pcm.name);
            Core.Log("KerbalHealthList updated: " + Count + " kerbals found.", LogLevel.Important);
        }

        void RemoveUntrackable()
        {
            List<string> toRemove = new List<string>();
            foreach (KerbalHealthStatus khs in Values)
            {
                if (!Core.IsKerbalTrackable(khs.PCM) && !khs.IsFrozen)
                {
                    Core.Log(khs.Name + " is not trackable anymore. Marking for removal.");
                    toRemove.Add(khs.Name);
                }
            }
            foreach (string name in toRemove)
                Remove(name);
            if (toRemove.Count > 0)
                Core.Log(toRemove.Count + " kerbal(s) removed from the KerbalHealthList.");
        }

        public void Update(double interval)
        {
            RemoveUntrackable();
            foreach (KerbalHealthStatus khs in Values)
                khs.Update(interval);
        }

        public new KerbalHealthStatus this[string name]
        {
            get => ContainsKey(name) ? base[name] : null;
            set => base[name] = value;
        }

        public KerbalHealthStatus this[ProtoCrewMember pcm]
        {
            get => ContainsKey(pcm.name) ? base[pcm.name] : null;
            set => base[pcm.name] = value;
        }

        /// <summary>
        /// Returns the list of names
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string s = "";
            foreach (string n in Keys)
                s += n + "\r\n";
            return s.Trim();
        }

        public KerbalHealthList() : base(HighLogic.fetch.currentGame.CrewRoster.Count) { }
    }
}
