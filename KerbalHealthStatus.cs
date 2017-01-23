using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class KerbalHealthStatus
    {
        public static float MinHP
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().MinHP; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().MinHP = value; }
        }  // Min allowed value for health

        public static float BaseMaxHP  // Base amount of health (for level 0 kerbal)
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().BaseMaxHP; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().BaseMaxHP = value; }
        }

        public static float HPPerLevel  // Health increase per kerbal level
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().HPPerLevel; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().HPPerLevel = value; }
        }

        public static float ExhaustionStartHealth  // Health % when the kerbal becomes exhausted (i.e. a Tourist). Must be <= ExhaustionEndHealth
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().ExhaustionStartHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().ExhaustionStartHealth = value; }
        }

        public static float ExhaustionEndHealth  // Health % when the kerbal leaves exhausted state (i.e. becomes Crew again). Must be >= ExhaustionStartHealth
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().ExhaustionEndHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().ExhaustionEndHealth = value; }
        }


        public static float DeathHealth  // Health % when the kerbal dies
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().DeathHealth; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<GeneralSettings>().DeathHealth = value; }
        }


        public static float AssignedFactor  // Health change per day when the kerbal is assigned
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().AssignedFactor; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().AssignedFactor = value; }
        }

        public static float LivingSpaceBaseFactor  // Health change per day in a crammed vessel
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().LivingSpaceBaseFactor; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().LivingSpaceBaseFactor = value; }
        }

        public static float NotAloneFactor  // Health change per day when the kerbal has crewmates
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().NotAloneFactor; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().NotAloneFactor = value; }
        }

        public static float KSCFactor  // Health change per day when the kerbal is at KSC (available)
        {
            get { return HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().KSCFactor; }
            set { HighLogic.CurrentGame.Parameters.CustomParams<FactorsSettings>().KSCFactor = value; }
        }


        string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                pcmCached = null;
            }
        }

        protected double hp;
        public double HP
        {
            get { return hp; }
            set
            {
                if (value < MinHP) hp = MinHP;
                else if (value > MaxHP) hp = MaxHP;
                else hp = value;
            }
        }

        public double Health
        {
            get { return (HP - MinHP) / (MaxHP - MinHP) * 100; }
        }

        string trait = null;
        string Trait
        {
            get
            {
                return trait ?? PCM.trait;
            }
            set
            {
                trait = value;
            }
        }

        public enum HealthCondition { OK, Exhausted }
        HealthCondition condition = HealthCondition.OK;
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

        public static double GetMaxHealth(ProtoCrewMember pcm)
        {
            return BaseMaxHP + HPPerLevel * pcm.experienceLevel;
        }

        public double MaxHP
        {
            get { return GetMaxHealth(PCM); }
        }

        public double TimeToValue(double target, bool inEditor = false)
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
                        return TimeToValue(ExhaustionEndHealth * MaxHP, inEditor);
                }
            }
            switch (Condition)
            {
                case HealthCondition.OK:
                    return TimeToValue(ExhaustionStartHealth * MaxHP, inEditor);
                case HealthCondition.Exhausted:
                    return TimeToValue(DeathHealth * MaxHP, inEditor);
            }
            return double.NaN;
        }

        static int GetCrewCount(ProtoCrewMember pcm, bool inEditor = false)
        {
            return inEditor ? ShipConstruction.ShipManifest.CrewCount : (pcm?.seat?.vessel.GetCrewCount() ?? 1);
        }

        static int GetCrewCapacity(ProtoCrewMember pcm, bool inEditor = false)
        {
            return inEditor ? ShipConstruction.ShipManifest.GetAllCrew(true).Count : (pcm?.seat?.vessel.GetCrewCapacity() ?? 1);
        }

        public static double HealthChangePerDay(ProtoCrewMember pcm, bool inEditor = false)
        {
            double change = 0;
            if (pcm == null) return 0;
            if ((pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || inEditor)
            {
                change += AssignedFactor;
                change += LivingSpaceBaseFactor * GetCrewCount(pcm, inEditor) / GetCrewCapacity(pcm, inEditor);
                if (GetCrewCount(pcm, inEditor) > 1) change += NotAloneFactor;
            }
            if (!inEditor && (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available)) change += KSCFactor;
            return change;
        }

        public void Update(double interval)
        {
            Log.Post("Updating " + Name + "'s health.");
            HP += HealthChangePerDay(PCM) / 21600 * interval;
            if (HP <= DeathHealth * MaxHP)
            {
                Log.Post(Name + " dies due to having " + HP + " health.");
                if (PCM.seat != null) PCM.seat.part.RemoveCrewmember(PCM);
                PCM.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                ScreenMessages.PostScreenMessage(Name + " dies of poor health!");
            }
            if (Condition == HealthCondition.OK && HP <= ExhaustionStartHealth * MaxHP)
            {
                Condition = HealthCondition.Exhausted;
                ScreenMessages.PostScreenMessage(Name + " is exhausted!");
            }
            if (Condition == HealthCondition.Exhausted && HP >= ExhaustionEndHealth * MaxHP)
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
                return n;
            }
            set
            {
                Name = value.GetValue("name");
                HP = Double.Parse(value.GetValue("health"));
                Condition = (KerbalHealthStatus.HealthCondition)Enum.Parse(typeof(HealthCondition), value.GetValue("condition"));
                if (Condition == HealthCondition.Exhausted) Trait = value.GetValue("trait");
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
