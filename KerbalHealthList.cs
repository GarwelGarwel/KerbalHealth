using System.Collections.Generic;
using System.Linq;

namespace KerbalHealth
{
    /// <summary>
    /// List of all tracked kerbals
    /// </summary>
    public class KerbalHealthList : Dictionary<string, KerbalHealthStatus>
    {
        public new KerbalHealthStatus this[string name]
        {
            get => TryGetValue(name, out KerbalHealthStatus res) ? res : null;
            set => base[name] = value;
        }

        public KerbalHealthStatus this[ProtoCrewMember pcm]
        {
            get => this[pcm.name];
            set => base[pcm.name] = value;
        }

        public KerbalHealthList()
            : base(HighLogic.fetch.currentGame.CrewRoster.Count)
        { }

        public List<KerbalHealthStatus> List => Values.ToList();

        /// <summary>
        /// Adds a kerbal to the list, unless already exists
        /// </summary>
        public void Add(ProtoCrewMember pcm)
        {
            if (pcm == null)
            {
                Core.Log($"Trying to register a KerbalHealthStatus for a null ProtoCrewMember!", LogLevel.Error);
                return;
            }
            if (ContainsKey(pcm.name))
                return;
            Core.Log($"Registering {pcm.name}.", LogLevel.Important);
            Add(pcm.name, new KerbalHealthStatus(pcm));
        }

        /// <summary>
        /// Adds a kerbal to the list, unless already exists
        /// </summary>
        public void Add(KerbalHealthStatus khs)
        {
            if (!ContainsKey(khs.Name))
                Add(khs.Name, khs);
        }

        /// <summary>
        /// Changes name of a registered kerbal and renames the entry
        /// </summary>
        public void Rename(string name1, string name2)
        {
            Core.Log($"KerbalHealthList.Rename('{name1}', '{name2}')");
            if (name1 == name2)
                return;
            if (ContainsKey(name1))
            {
                this[name1].Name = name2;
                Add(name2, this[name1]);
                Remove(name1);
            }
            else Core.Log($"Could not find '{name1}'.", LogLevel.Error);
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
            Core.Log($"{list.Count} total trackable kerbals.", LogLevel.Important);
            foreach (ProtoCrewMember pcm in list.Where(pcm => pcm.IsTrackable()))
                Add(pcm);
            Core.Log($"KerbalHealthList updated: {Count} kerbals found.", LogLevel.Important);
        }

        public void Update(float interval)
        {
            RemoveUntrackable();
            foreach (KerbalHealthStatus khs in Values)
                khs.Update(interval);
        }

        /// <summary>
        /// Returns the list of names
        /// </summary>
        public override string ToString()
        {
            string s = "";
            foreach (string n in Keys)
                s += $"{n}\r\n";
            return s.Trim();
        }

        void RemoveUntrackable()
        {
            List<string> toRemove = new List<string>(Values
                .Where(khs => !khs.ProtoCrewMember.IsTrackable() && !khs.IsFrozen)
                .Select(khs => khs.Name));
            foreach (string name in toRemove)
            {
                Core.Log($"{name} is not trackable anymore. Marking for removal.");
                Remove(name);
            }
        }
    }
}
