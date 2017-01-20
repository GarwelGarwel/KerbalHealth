using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class KerbalHealthStatus
    {
        public static double MinHealth { get; set; } = 0;  // Min allowed value for health
        public static double BaseHealth { get; set; } = 100;  // Base amount of health (for level 0 kerbal)
        public static double HealthPerLevel { get; set; } = 10;  // Health increase per kerbal level

        public static double ExhaustionStartHealth { get; set; } = 0.20;  // Health % when a kerbal becomes exhausted (i.e. a Tourist). Must be <= ExhaustionEndHealth
        public static double ExhaustionEndHealth { get; set; } = 0.25;  // Health % when a kerbal leaves exhausted state (i.e. becomes Crew again). Must be >= ExhaustionStartHealth
        public static double DeathHealth { get; set; } = 0;  // Health % when kerbal dies

        public static double AssignedHealthChange { get; set; } = 0;  // Health change per day when kerbal is assigned
        public static double LivingSpaceBaseChange { get; set; } = -20;  // Health change per day in a crammed vessel
        public static double KSCHealthChange { get; set; } = 100;  // Health change per day when kerbal is at KSC (available)

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

        protected double health;
        public double Health
        {
            get { return health; }
            set
            {
                if (value < MinHealth) health = MinHealth;
                else if (value > MaxHealth) health = MaxHealth;
                else health = value;
            }
        }

        public double HealthPercentage
        {
            get { return (Health - MinHealth) / (MaxHealth - MinHealth) * 100; }
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
            return BaseHealth + HealthPerLevel * pcm.experienceLevel;
        }

        public double MaxHealth
        {
            get { return GetMaxHealth(PCM); }
        }

        public double TimeToValue(double target)
        {
            double change = HealthChangePerDay(PCM);
            if (change == 0) return double.NaN;
            double res = (target - Health) / change;
            if (res < 0) return double.NaN;
            return res * 21600;
        }

        public double TimeToNextCondition()
        {
            if (HealthChangePerDay(PCM) > 0)
            {
                switch (Condition)
                {
                    case HealthCondition.OK:
                        return TimeToValue(MaxHealth);
                    case HealthCondition.Exhausted:
                        return TimeToValue(ExhaustionEndHealth * MaxHealth);
                }
            }
            switch (Condition)
            {
                case HealthCondition.OK:
                    return TimeToValue(ExhaustionStartHealth * MaxHealth);
                case HealthCondition.Exhausted:
                    return TimeToValue(DeathHealth * MaxHealth);
            }
            return double.NaN;
        }

        static double GetLivingSpaceFactor(ProtoCrewMember pcm)
        {
            if (pcm?.seat?.vessel == null) return 1;
            int capacity = pcm.seat.vessel.GetCrewCapacity();
            if (capacity == 0) return 1;
            return (double) pcm.seat.vessel.GetCrewCount() / capacity;
        }

        public static double HealthChangePerDay(ProtoCrewMember pcm)
        {
            double change = 0;
            //Log.Post(pcm.name + " is " + pcm.rosterStatus);
            if (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Assigned)
            {
                change += AssignedHealthChange;
                //Log.Post(pcm.name + " has LivingFactor of " + GetLivingSpaceFactor(pcm));
                change += LivingSpaceBaseChange * GetLivingSpaceFactor(pcm);
            }
            if (pcm.rosterStatus == ProtoCrewMember.RosterStatus.Available) change += KSCHealthChange;
            return change;
        }

        public void Update(double interval)
        {
            Log.Post("Updating " + Name + "'s health.");
            Health += HealthChangePerDay(PCM) / 21600 * interval;
            if (Health <= DeathHealth * MaxHealth)
            {
                Log.Post(Name + " dies due to having " + Health + " health.");
                if (PCM.seat != null) PCM.seat.part.RemoveCrewmember(PCM);
                PCM.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                ScreenMessages.PostScreenMessage(Name + " dies of poor health!");
            }
            if (Condition == HealthCondition.OK && Health <= ExhaustionStartHealth * MaxHealth)
            {
                Condition = HealthCondition.Exhausted;
                ScreenMessages.PostScreenMessage(Name + " is exhausted!");
            }
            if (Condition == HealthCondition.Exhausted && Health >= ExhaustionEndHealth * MaxHealth)
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
                n.AddValue("health", Health);
                n.AddValue("condition", Condition);
                if (Condition == HealthCondition.Exhausted) n.AddValue("trait", Trait);
                return n;
            }
            set
            {
                Name = value.GetValue("name");
                Health = Double.Parse(value.GetValue("health"));
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
            Health = MaxHealth;
        }

        public KerbalHealthStatus(string name, double health)
        {
            Name = name;
            Health = health;
        }

        public KerbalHealthStatus(ConfigNode node)
        {
            ConfigNode = node;
        }
    }
}
