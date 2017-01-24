using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class KerbalHealthStatus
    {
        public enum HealthCondition { OK, Exhausted }  // conditions

        string name;
        double hp;
        double lastHPChange;  // HP change per day due to vessel-wide and part-wide factors (updated by ModuleKerbalHealth)
        HealthCondition condition = HealthCondition.OK;
        string trait = null;
        bool onEva = false;  // True if kerbal is on EVA

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                pcmCached = null;
            }
        }

        public double HP
        {
            get { return hp; }
            set
            {
                if (value < Core.MinHP) hp = Core.MinHP;
                else if (value > MaxHP) hp = MaxHP;
                else hp = value;
            }
        }

        public double Health { get { return (HP - Core.MinHP) / (MaxHP - Core.MinHP); } }  // % of health relative to MaxHealth

        public double LastHPChange
        {
            get { return lastHPChange; }
            set { lastHPChange = value; }
        }

        public HealthCondition Condition
        {
            get { return condition; }
            set
            {
                if (value == condition) return;
                switch (value)
                {
                    case HealthCondition.OK:
                        Log.Post("Reviving " + Name + " as " + Trait + "...");
                        PCM.type = ProtoCrewMember.KerbalType.Crew;
                        PCM.trait = Trait;
                        break;
                    case HealthCondition.Exhausted:
                        Log.Post(Name + " (" + Trait + ") is exhausted.");
                        Trait = PCM.trait;
                        PCM.type = ProtoCrewMember.KerbalType.Tourist;
                        break;
                }
                condition = value;
            }
        }

        string Trait
        {
            get { return trait ?? PCM.trait; }
            set { trait = value; }
        }

        public bool IsOnEVA
        {
            get { return onEva; }
            set { onEva = value; }
        }

        ProtoCrewMember pcmCached;
        public ProtoCrewMember PCM
        {
            get
            {
                //if (pcmCached != null) return pcmCached;
                foreach (ProtoCrewMember pcm in HighLogic.fetch.currentGame.CrewRoster.Crew)
                    if (pcm.name == Name)
                    {
                        pcmCached = pcm;
                        return pcm;
                    }
                foreach (ProtoCrewMember pcm in HighLogic.fetch.currentGame.CrewRoster.Tourist)
                    if (pcm.name == Name)
                    {
                        pcmCached = pcm;
                        return pcm;
                    }
                return null;
            }
            set
            {
                Name = value.name;
                pcmCached = value;
            }
        }

        public static double GetMaxHP(ProtoCrewMember pcm)
        {
            return Core.BaseMaxHP + Core.HPPerLevel * pcm.experienceLevel;
        }

        public double MaxHP
        {
            get { return GetMaxHP(PCM); }
        }

        public double TimeToValue(double target, bool inEditor)
        {
            double change = HealthChangePerDay(PCM, inEditor);
            if (change == 0) return double.NaN;
            double res = (target - HP) / change;
            if (res < 0) return double.NaN;
            return res * 21600;
        }

        public double TimeToNextCondition(bool inEditor = false)
        {
            if (HealthChangePerDay(PCM, inEditor) > 0)
            {
                switch (Condition)
                {
                    case HealthCondition.OK:
                        return TimeToValue(MaxHP, inEditor);
                    case HealthCondition.Exhausted:
                        return TimeToValue(Core.ExhaustionEndHealth * MaxHP, inEditor);
                }
            }
            switch (Condition)
            {
                case HealthCondition.OK:
                    return TimeToValue(Core.ExhaustionStartHealth * MaxHP, inEditor);
                case HealthCondition.Exhausted:
                    return TimeToValue(Core.DeathHealth * MaxHP, inEditor);
            }
            return double.NaN;
        }

        static int GetCrewCount(ProtoCrewMember pcm, bool inEditor)
        {
            return inEditor ? ShipConstruction.ShipManifest.CrewCount : (pcm?.seat?.vessel.GetCrewCount() ?? 1);
        }

        static int GetCrewCapacity(ProtoCrewMember pcm, bool inEditor)
        {
            return inEditor ? ShipConstruction.ShipManifest.GetAllCrew(true).Count : (pcm?.seat?.vessel.GetCrewCapacity() ?? 1);
        }

        static bool isKerbalLoaded(ProtoCrewMember pcm)
        { return pcm?.seat?.vessel != null; }

        public static double HealthChangePerDay(ProtoCrewMember pcm, bool inEditor)
        {
            double change = 0;
            if (pcm == null) return 0;
            KerbalHealthStatus khs = Core.KerbalHealthList.Find(pcm);
            if (khs == null)
            {
                Log.Post("Error: " + pcm.name + " not found in KerbalHealthList during update!", Log.LogLevel.Error);
                return 0;
            }
            if ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned && isKerbalLoaded(pcm)) || inEditor || khs.IsOnEVA)
            {
                if (isKerbalLoaded(pcm)) khs.IsOnEVA = false;
                change += Core.AssignedFactor;
                change += Core.LivingSpaceBaseFactor * GetCrewCount(pcm, inEditor) / GetCrewCapacity(pcm, inEditor);
                if (!khs.IsOnEVA)
                {
                    if (GetCrewCount(pcm, inEditor) > 1) change += Core.NotAloneFactor;
                    if (inEditor)
                        foreach (PartCrewManifest p in ShipConstruction.ShipManifest.PartManifests)
                        {
                            ModuleKerbalHealth mkh = p.PartInfo.partPrefab.FindModuleImplementing<ModuleKerbalHealth>();
                            //Log.Post(p.PartInfo.name + " has " + (mkh == null ? "no " : "") + " ModuleKerbalHealth and crew: " + p.GetPartCrew());
                            if (ModuleKerbalHealth.IsModuleApplicable(p, pcm)) change += mkh.hpChangePerDay;
                        }
                    else foreach (Part p in pcm.seat.vessel.Parts)
                        {
                            ModuleKerbalHealth mkh = p.FindModuleImplementing<ModuleKerbalHealth>();
                            if (ModuleKerbalHealth.IsModuleApplicable(mkh, pcm)) change += mkh.hpChangePerDay;
                        }
                }
                khs.LastHPChange = change;
            }
            else if (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned && !isKerbalLoaded(pcm))
            {
                //Log.Post(pcm.name + " is assigned, but not loaded. Seat: " + pcm?.seat + " (id " + pcm?.seatIdx + "). Using last cached HP change: " + khs.LastHPChange);
                change += khs.LastHPChange;
            }
            else if (!inEditor && (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available)) change += Core.KSCFactor;
            return change;
        }

        public void Update(double interval)
        {
            Log.Post("Updating " + Name + "'s health.");
            HP += HealthChangePerDay(PCM, false) / 21600 * interval;
            if (HP <= Core.DeathHealth * MaxHP)
            {
                Log.Post(Name + " dies due to having " + HP + " health.");
                if (PCM.seat != null) PCM.seat.part.RemoveCrewmember(PCM);
                PCM.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                ScreenMessages.PostScreenMessage(Name + " dies of poor health!");
            }
            if (Condition == HealthCondition.OK && HP <= Core.ExhaustionStartHealth * MaxHP)
            {
                Condition = HealthCondition.Exhausted;
                ScreenMessages.PostScreenMessage(Name + " is exhausted!");
            }
            if (Condition == HealthCondition.Exhausted && HP >= Core.ExhaustionEndHealth * MaxHP)
            {
                Condition = HealthCondition.OK;
                ScreenMessages.PostScreenMessage(Name + " has revived.");
            }
        }

        public ConfigNode ConfigNode
        {
            get
            {
                ConfigNode n = new ConfigNode("KerbalHealthStatus");
                n.AddValue("name", Name);
                n.AddValue("health", HP);
                n.AddValue("condition", Condition);
                if (Condition == HealthCondition.Exhausted) n.AddValue("trait", Trait);
                if (LastHPChange != 0) n.AddValue("lastHPChange", LastHPChange);
                if (IsOnEVA) n.AddValue("onEva", true);
                return n;
            }
            set
            {
                Name = value.GetValue("name");
                HP = Double.Parse(value.GetValue("health"));
                Condition = (KerbalHealthStatus.HealthCondition)Enum.Parse(typeof(HealthCondition), value.GetValue("condition"));
                if (Condition == HealthCondition.Exhausted) Trait = value.GetValue("trait");
                try { LastHPChange = double.Parse(value.GetValue("lastHPChange")); }
                catch (Exception) { LastHPChange = 0; }
                try { IsOnEVA = bool.Parse(value.GetValue("onEva")); }
                catch (Exception) { IsOnEVA = false; }
            }
        }

        public override bool Equals(object obj)
        {
            return ((KerbalHealthStatus)obj).Name.Equals(Name);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public KerbalHealthStatus() { }

        public KerbalHealthStatus(string name)
        {
            Name = name;
            HP = MaxHP;
        }

        public KerbalHealthStatus(string name, double health)
        {
            Name = name;
            HP = health;
        }

        public KerbalHealthStatus(ConfigNode node)
        {
            ConfigNode = node;
        }
    }
}
