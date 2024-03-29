﻿using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

using static KerbalHealth.Core;

namespace KerbalHealth
{
    /// <summary>
    /// Contains data about a kerbal's health
    /// </summary>
    public class KerbalHealthStatus : IConfigNode
    {
        #region BASIC PROPERTIES

        string name;
        string trait;

        ProtoCrewMember pcmCached;

        /// <summary>
        /// Kerbal's name
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                name = value;
                pcmCached = null;
            }
        }

        public string FullName =>
            $"{Name}{(KerbalHealthGeneralSettings.Instance.ShowTraitLevel ? $" ({Localizer.Format($"#KH_TraitSymbol_{ProtoCrewMember.trait}")}{ProtoCrewMember.experienceLevel})" : "")}";

        /// <summary>
        /// Returns true if the kerbal is marked as being on EVA
        /// </summary>
        public bool IsOnEVA { get; set; } = false;

        /// <summary>
        /// Returns true if a low health alarm has been shown for the kerbal
        /// </summary>
        public bool IsWarned { get; set; } = true;

        /// <summary>
        /// Returns ProtoCrewMember for the kerbal
        /// </summary>
        public ProtoCrewMember ProtoCrewMember
        {
            get
            {
                if (pcmCached != null)
                    return pcmCached;
                try
                {
                    return pcmCached = HighLogic.fetch.currentGame.CrewRoster[Name];
                }
                catch (ArgumentOutOfRangeException)
                {
                    Log($"Could not find ProtoCrewMember for {Name}. KerbalHealth kerbal list: {Core.KerbalHealthList}");
                    return null;
                }
            }

            set
            {
                Name = value.name;
                pcmCached = value;
            }
        }

        public string LocationString
        {
            get
            {
                if (IsFrozen)
                    return Localizer.Format("#KH_Location_frozen");//"Frozen"

                switch (ProtoCrewMember.rosterStatus)
                {
                    case ProtoCrewMember.RosterStatus.Available:
                        return Localizer.Format("#KH_Location_status1");//"KSC"
                    case ProtoCrewMember.RosterStatus.Dead:
                        return Localizer.Format("#KH_Location_status2");//"Dead"
                    case ProtoCrewMember.RosterStatus.Missing:
                        return Localizer.Format("#KH_Location_status3");//"Unknown"
                    case Status_Frozen:
                        return Localizer.Format("#KH_Location_status4");//"On Vacation"
                }

                Vessel v = ProtoCrewMember.GetVessel();
                if (v == null)
                    return Localizer.Format("#KH_NA");
                if (v.isEVA)
                    return Localizer.Format("#KH_Location_status5", v.mainBody.bodyName);//"EVA (" +  + ")"
                if (v.loaded && CLS.Enabled && CLS.CLSAddon.getCLSVessel(v).Spaces.Count > 1 && !string.IsNullOrWhiteSpace(ProtoCrewMember?.GetCLSSpace(v)?.Name))
                    return Localizer.Format("#KH_Location_CLS", v.vesselName, ProtoCrewMember.GetCLSSpace(v).Name);
                return v.vesselName;
            }
        }

        /// <summary>
        /// Returns saved kerbal's trait or current trait if nothing is saved
        /// </summary>
        string Trait
        {
            get => trait ?? ProtoCrewMember.trait;
            set => trait = value;
        }

        #endregion BASIC PROPERTIES

        #region HP

        double hp;

        /// <summary>
        /// Kerbal's health points
        /// </summary>
        public double HP
        {
            get => hp;
            set
            {
                hp = value < 0 ? 0 : Math.Min(value, MaxHP);
                if (!IsWarned && Health < KerbalHealthGeneralSettings.Instance.LowHealthAlert)
                {
                    ShowMessage(Localizer.Format("#KH_Condition_LowHealth", Name), ProtoCrewMember);
                    IsWarned = true;
                }
                else if (IsWarned && Health >= KerbalHealthGeneralSettings.Instance.LowHealthAlert)
                    IsWarned = false;
            }
        }

        /// <summary>
        /// Returns the max number of HP for the kerbal (including the modifier)
        /// </summary>
        public double MaxHP => (GetDefaultMaxHP(ProtoCrewMember) + HealthEffects.MaxHPBonus) * HealthEffects.MaxHP * RadiationMaxHPModifier;

        /// <summary>
        /// Returns kerbal's HP relative to MaxHealth (0 to 1)
        /// </summary>
        public double Health => HP / MaxHP;

        /// <summary>
        /// Returns the max number of HP for the kerbal (not including modifiers)
        /// </summary>
        /// <param name="pcm"></param>
        /// <returns></returns>
        public static double GetDefaultMaxHP(ProtoCrewMember pcm) =>
            KerbalHealthGeneralSettings.Instance.BaseMaxHP + KerbalHealthGeneralSettings.Instance.HPPerLevel * pcm.experienceLevel;

        #endregion HP

        #region HP CHANGE

        Dictionary<HealthFactor, double> factorsOriginal = new Dictionary<HealthFactor, double>();
        bool factorsDirty = true;

        /// <summary>
        /// List of factors' effect on the kerbal before effects of location and quirks
        /// </summary>
        public Dictionary<HealthFactor, double> FactorsOriginal
        {
            get
            {
                if (factorsDirty)
                    CalculateFactors();
                return factorsOriginal;
            }
            protected set => factorsOriginal = value;
        }

        public Dictionary<HealthFactor, double> Factors => HealthEffects.FactorMultipliers.ApplyToFactors(FactorsOriginal);

        public double HPChangeFromFactors => Factors.Sum(kvp => kvp.Value);

        public double Recuperation => HealthEffects.EffectiveRecuperation;

        public double Decay => HealthEffects.Decay;

        public double HPChangeMarginal => (Recuperation / 100) * (MaxHP - HP) - (Decay / 100) * HP;

        public double HPChangeTotal => HPChangeFromFactors + HPChangeMarginal;

        void CalculateFactors()
        {
            factorsDirty = false;
            bool unpacked = ProtoCrewMember.IsUnpacked(), inEditor = IsInEditor;
            if (!inEditor && ProtoCrewMember.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
            {
                FactorsOriginal.Clear();
                if (IsFrozen)
                    return;
            }

            // Getting factors' HP change per day for non-constant factors only, unless the kerbal is loaded or the scene is editor
            for (int i = 0; i < Core.Factors.Count; i++)
                if (unpacked || inEditor || !Core.Factors[i].ConstantForUnloaded)
                {
                    HealthFactor f = Core.Factors[i];
                    FactorsOriginal[f] = f.ChangePerDay(this);
                    if (IsLogging())
                        Log($"{f.Name} factor is {FactorsOriginal[f]:F2} HP/day.");
                }
            if (IsLogging())
                Log($"Factors HP change before effects: {FactorsOriginal.Sum(kvp => kvp.Value):F2} HP/day.");
        }

        public double GetFactorHPChange(HealthFactor factor) => Factors.TryGetValue(factor, out double res) ? res : 0;

        /// <summary>
        /// How many seconds left until HP reaches the given level, at the current HP change rate
        /// </summary>
        /// <param name="target">Target HP level</param>
        /// <returns></returns>
        public double ETAToHP(double target)
        {
            double hpChange = HPChangeTotal;
            if (hpChange == 0)
                return double.NaN;
            double res = (target - HP) / hpChange;
            return res >= 0 ? res * KSPUtil.dateTimeFormatter.Day : double.NaN;
        }

        /// <summary>
        /// Health Points for the next condition (OK, Exhausted or death)
        /// </summary>
        public double NextConditionHP
        {
            get
            {
                double hpChange = HPChangeTotal;
                if (hpChange > 0)
                    return HP < CriticalHP ? CriticalHP : MaxHP;
                if (hpChange < 0)
                    return hp < CriticalHP ? 0 : CriticalHP;
                return double.NaN;
            }
        }

        /// <summary>
        /// Number of seconds until the next condition is reached
        /// </summary>
        public double ETAToNextCondition => ETAToHP(NextConditionHP);

        /// <summary>
        /// Returns HP level when marginal HP change balances out "fixed" change. If <= 0, no such level
        /// </summary>
        /// <returns></returns>
        public double BalanceHP =>
            Recuperation + Decay == 0
                ? (HPChangeFromFactors < 0 ? 0 : MaxHP)
                : (MaxHP * Recuperation + HPChangeFromFactors * 100) / (Recuperation + Decay);

        #endregion HP CHANGE

        #region EFFECTS

        bool effectsDirty = true;

        HealthEffect locationEffect, quirksEffect, totalEffect;

        public HealthEffect LocationEffect
        {
            get
            {
                if (effectsDirty)
                    CalculateEffects();
                return locationEffect;
            }
            set => locationEffect = value;
        }

        public HealthEffect QuirksEffect
        {
            get
            {
                if (effectsDirty)
                    CalculateEffects();
                return quirksEffect;
            }
            set => quirksEffect = value;
        }

        public HealthEffect HealthEffects
        {
            get
            {
                if (effectsDirty)
                    CalculateEffects();
                return totalEffect;
            }
            set => totalEffect = value;
        }

        /// <summary>
        /// Calculates health effects of vessel and part. For use in KSC, Flight, Map scenes (i.e. not Editor)
        /// </summary>
        void CalculateLocationEffectInFlight()
        {
            Log($"CalculateLocationEffectInFlight for {Name}");
            if (ProtoCrewMember.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
            {
                Log($"{Name} is not assigned.");
                LocationEffect = null;
                return;
            }

            if (!ProtoCrewMember.IsUnpacked())
                return;

            if (IsOnEVA)
            {
                // The kerbal is on EVA => hard-coded vesselEffect
                Log($"{Name} is on EVA => setting exposure to appropriate value.");
                LocationEffect = new HealthEffect()
                { ExposureMultiplier = KerbalHealthRadiationSettings.Instance.EVAExposure };
            }
            else
            {
                // The kerbal is in a vessel => recalculate vesselEffect & partEffect
                Vessel v = ProtoCrewMember.GetVessel();
                Log($"{Name} is in {v.vesselName}. It is {(v.loaded ? "" : "NOT ")}loaded.");
                LocationEffect = new HealthEffect(v, CLS.Enabled ? ProtoCrewMember.GetCLSSpace(v) : null);
            }
        }

        void CalculateLocationEffectInEditor()
        {
            if (ShipConstruction.ShipManifest == null || !ShipConstruction.ShipManifest.Contains(ProtoCrewMember))
                return;
            Log($"CalculateLocationEffectInEditor for {Name}");
            ConnectedLivingSpace.ICLSSpace space = CLS.Enabled ? ProtoCrewMember.GetCLSSpace() : null;
            LocationEffect = new HealthEffect(EditorLogic.SortedShipList, Math.Max(space != null ? space.Crew.Count : ShipConstruction.ShipManifest.CrewCount, 1), space);
            Log($"Location effect:\n{locationEffect}");
        }

        void CalculateQuirkEffects()
        {
            Log($"CalculateQuirkEffects for {Name}");
            quirksEffect = new HealthEffect();
            for (int i = 0; i < Quirks.Count; i++)
            {
                List<HealthEffect> effects = Quirks[i].GetApplicableEffects(this).ToList();
                for (int j = 0; j < effects.Count; j++)
                {
                    quirksEffect.CombineWith(effects[j]);
                    if (IsLogging())
                    {
                        Log($"Applied quirk effect: {effects[j]}");
                        Log($"Quirks effect:\n{quirksEffect}");
                    }
                }
            }
        }

        void CalculateEffects()
        {
            Log($"Calculating all effects for {Name}.");
            effectsDirty = false;
            if (IsInEditor)
                CalculateLocationEffectInEditor();
            else CalculateLocationEffectInFlight();
            if (KerbalHealthQuirkSettings.Instance.QuirksEnabled)
                CalculateQuirkEffects();
            else quirksEffect = new HealthEffect();
            totalEffect = HealthEffect.Combine(LocationEffect, QuirksEffect);
            Log($"Total effect:\n{totalEffect}");
        }

        #endregion EFFECTS

        #region CONDITIONS

        public const string Condition_Exhausted = "Exhausted";
        public const string Condition_Decontaminating = "Decontaminating";
        public const string Condition_Training = "Training";
        public const string Condition_Frozen = "Frozen";

        /// <summary>
        /// Returns a list of all active health conditions for the kerbal
        /// </summary>
        public List<HealthCondition> Conditions { get; set; } = new List<HealthCondition>();

        public IEnumerable<HealthCondition> VisibleConditions => Conditions.Where(condition => condition.Visible);

        /// <summary>
        /// Whether the kerbal is frozen by DeepFreeze mod (has 'Frozen' condition)
        /// </summary>
        public bool IsFrozen { get; private set; }

        /// <summary>
        /// Returns a comma-separated list of visible conditions or "OK" if there are no visible conditions
        /// </summary>
        public string ConditionString
        {
            get
            {
                string res = "";
                foreach (HealthCondition hc in VisibleConditions)
                {
                    if (res.Length != 0)
                        res += ", ";
                    res += hc.Title;
                }
                if (res.Length == 0)
                    res = HP < CriticalHP ? Localizer.Format("#KH_Critical") : Localizer.Format("#KH_NoConditions");
                return res;
            }
        }

        /// <summary>
        /// Returns false if at least one of kerbal's current health conditions makes him/her incapacitated (i.e. turns into a Tourist), true otherwise
        /// </summary>
        public bool IsCapable => !Conditions.Any(hc => hc.Incapacitated);

        /// <summary>
        /// Health level (in percentage) for Exhaustion condition to kick in
        /// </summary>
        public double CriticalHealth => KerbalHealthGeneralSettings.Instance.CriticalHealth * HealthEffects.CriticalHealth;

        /// <summary>
        /// HP for Exhaustion condition to kick in
        /// </summary>
        public double CriticalHP => CriticalHealth * MaxHP;

        /// <summary>
        /// Returns the condition with a given name, if present (null otherwise)
        /// </summary>
        public HealthCondition GetCondition(string condition) => Conditions.Find(hc => hc.Name == condition);

        /// <summary>
        /// Returns true if a given condition exists for the kerbal
        /// </summary>
        public bool HasCondition(string condition) => Conditions.Exists(hc => hc.Name == condition);

        /// <summary>
        /// Returns true if a given condition exists for the kerbal
        /// </summary>
        public bool HasCondition(HealthCondition condition) => Conditions.Contains(condition);

        /// <summary>
        /// Adds a new health condition
        /// </summary>
        /// <param name="condition">Condition to add</param>
        public void AddCondition(HealthCondition condition)
        {
            if (condition == null)
                return;
            Log($"Adding {condition.Name} condition to {Name}...");
            if (!condition.Stackable && HasCondition(condition))
                return;

            Conditions.Add(condition);
            if (KerbalHealthQuirkSettings.Instance.ConditionsEnabled)
                HP += condition.HP * KerbalHealthQuirkSettings.Instance.ConditionsEffect;
            Log($"{condition.Name} condition added to {Name}.", LogLevel.Important);
            if (condition.Incapacitated)
                MakeIncapacitated();
            if (condition.Visible)
                ShowMessage(Localizer.Format("#KH_Condition_Acquired", ProtoCrewMember.nameWithGender, condition.Title) + Localizer.Format(condition.Description, ProtoCrewMember.nameWithGender), ProtoCrewMember);// "<color=white>" + " has acquired " +  + "</color> condition!\r\n\n"
            RecalculateConditions();
        }

        public void AddCondition(string condition) => AddCondition(GetHealthCondition(condition));

        /// <summary>
        /// Removes a condition with from the kerbal
        /// </summary>
        /// <param name="condition">Condition to remove</param>
        /// <param name="removeAll">If true, all conditions with the same name will be removed. Makes sense for additive conditions. Default is false</param>
        public void RemoveCondition(HealthCondition condition, bool removeAll = false)
        {
            if (condition == null)
                return;
            Log($"Removing {condition.Name} condition from {Name}.", LogLevel.Important);

            int n = 0;
            if (removeAll)
            {
                while (Conditions.Remove(condition))
                    n++;
                Log($"{n} instance(s) of {condition.Name} removed.", LogLevel.Important);
            }
            else n = Conditions.Remove(condition) ? 1 : 0;
            if (KerbalHealthQuirkSettings.Instance.ConditionsEnabled && condition.RestoreHP)
                HP -= condition.HP * n * KerbalHealthQuirkSettings.Instance.ConditionsEffect;
            if (n > 0 && condition.Incapacitated && IsCapable)
                MakeCapable();
            if (n > 0 && condition.Visible)
                ShowMessage(Localizer.Format("#KH_Condition_Lost", Name, condition.Title), ProtoCrewMember);
            RecalculateConditions();
        }

        public void RemoveCondition(string condition, bool removeAll = false) => RemoveCondition(GetHealthCondition(condition), removeAll);

        void RecalculateConditions()
        {
            IsTrainingAtKSC = HasCondition(Condition_Training);
            IsDecontaminating = HasCondition(Condition_Decontaminating);
            IsFrozen = HasCondition(Condition_Frozen);
        }

        /// <summary>
        /// Turn a kerbal into a Tourist
        /// </summary>
        void MakeIncapacitated()
        {
            if (Trait != null && ProtoCrewMember.type == ProtoCrewMember.KerbalType.Tourist)
            {
                Log($"{Name} is already incapacitated.", LogLevel.Important);
                return;
            }
            Log($"{Name} ({Trait}) is incapacitated.", LogLevel.Important);
            Trait = ProtoCrewMember.trait;
            ProtoCrewMember.type = ProtoCrewMember.KerbalType.Tourist;
            KerbalRoster.SetExperienceTrait(ProtoCrewMember, KerbalRoster.touristTrait);
        }

        /// <summary>
        /// Revives a kerbal after being incapacitated
        /// </summary>
        void MakeCapable()
        {
            // Check if the kerbal has already been revived by another mod
            if (ProtoCrewMember.type != ProtoCrewMember.KerbalType.Tourist)
                return;
            Log($"{Name} is becoming {Trait ?? "something strange"} again.", LogLevel.Important);
            if (Trait != null && Trait != "Tourist")
            {
                ProtoCrewMember.type = ProtoCrewMember.KerbalType.Crew;
                KerbalRoster.SetExperienceTrait(ProtoCrewMember, Trait);
            }
            Trait = null;
        }

        #endregion CONDITIONS

        #region QUIRKS

        /// <summary>
        /// List of this kerbal's quirks
        /// </summary>
        public List<Quirk> Quirks { get; set; } = new List<Quirk>();

        /// <summary>
        /// Last level processed for the kerbal
        /// </summary>
        public int QuirkLevel { get; set; } = 0;

        public bool HasQuirk(string quirk) => Quirks.Any(q => q.Name == quirk);

        /// <summary>
        /// Adds the quirk unless it is already present
        /// </summary>
        /// <param name="quirk"></param>
        public void AddQuirk(Quirk quirk)
        {
            if (quirk != null && !Quirks.Contains(quirk))
                Quirks.Add(quirk);
        }

        /// <summary>
        /// Adds the quirk unless it is already present
        /// </summary>
        /// <param name="quirk"></param>
        public void AddQuirk(string quirk) => AddQuirk(GetQuirk(quirk));

        public Quirk GetRandomQuirk(int level)
        {
            List<Quirk> availableQuirks = new List<Quirk>();
            List<double> weights = new List<double>();
            double weightSum = 0;
            foreach (Quirk q in Core.Quirks.Where(q => q.IsVisible && q.IsAvailableTo(this, level) && !Quirks.Contains(q)))
            {
                availableQuirks.Add(q);
                double w = KerbalHealthQuirkSettings.Instance.StatsAffectQuirkWeights
                    ? GetQuirkWeight(ProtoCrewMember.courage, q.CourageWeight) * GetQuirkWeight(ProtoCrewMember.stupidity, q.StupidityWeight)
                    : 1;
                weightSum += w;
                weights.Add(w);
                Log($"Available quirk: {q.Name} (weight {w}).");
            }

            if (!availableQuirks.Any() || weightSum <= 0)
            {
                Log($"No available quirks found for {Name} (level {level}).", LogLevel.Important);
                return null;
            }

            double r = Rand.NextDouble() * weightSum;
            Log($"Quirk selection roll: {r} out of {weightSum}. Total available quirks: {availableQuirks.Count}.");
            for (int i = 0; i < availableQuirks.Count; i++)
            {
                r -= weights[i];
                if (r < 0)
                {
                    Log($"Quirk {availableQuirks[i].Name} selected.");
                    return availableQuirks[i];
                }
            }
            Log("Something is terribly wrong with quirk selection!", LogLevel.Error);
            return null;
        }

        public Quirk AddRandomQuirk(int level)
        {
            Quirk q = GetRandomQuirk(level);
            if (q != null)
            {
                Quirks.Add(q);
                if (q.IsVisible)
                    ShowMessage(Localizer.Format("#KH_Condition_Quirk", Name, q), ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Assigned);
            }
            return q;
        }

        public Quirk AddRandomQuirk() => AddRandomQuirk(ProtoCrewMember.experienceLevel);

        public void CheckForAvailableQuirks()
        {
            if (KerbalHealthQuirkSettings.Instance.AwardQuirksOnMissions || (ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available))
            {
                for (int l = QuirkLevel; l <= ProtoCrewMember.experienceLevel; l++)
                {
                    if (Quirks.Count >= KerbalHealthQuirkSettings.Instance.MaxQuirks)
                    {
                        Log($"{Name} at level {l} is eligible for a quirk roll, but already has {Quirks.Count} quirks.");
                        break;
                    }
                    if (Rand.NextDouble() < KerbalHealthQuirkSettings.Instance.QuirkChance)
                    {
                        Log($"A quirk will be added to {Name} (level {l}).");
                        AddRandomQuirk(l);
                    }
                    else Log($"No quirks will be added to {Name} (level {l}).");
                }
                QuirkLevel = ProtoCrewMember.experienceLevel + 1;
            }
        }

        static double GetQuirkWeight(double val, double k) => val * (2 - 4 / (k + 1)) + 2 / (k + 1);

        #endregion QUIRKS

        #region TRAINING

        /// <summary>
        /// Names of parts the kerbal is currently training for
        /// </summary>
        public List<PartTrainingInfo> TrainedParts { get; set; } = new List<PartTrainingInfo>();

        /// <summary>
        /// Name of the vessel the kerbal is currently training for (information only)
        /// </summary>
        public string TrainingVessel { get; set; }

        /// <summary>
        /// Whether the kerbal is currently training at KSC (i.e. has Training condition)
        /// </summary>
        public bool IsTrainingAtKSC { get; private set; }

        public float LastRealTrainingPerDay { get; private set; }

        public bool ConditionsPreventKSCTraining => Conditions.Any(condition => condition.Visible && condition.Name != Condition_Training);

        public float StupidityTrainingSpeedFactor =>
            (KerbalHealthFactorsSettings.Instance.StupidityPenalty + 2)
            / (KerbalHealthFactorsSettings.Instance.StupidityPenalty * ProtoCrewMember.stupidity + 1) / 2;

        public float KSCTrainingPerDay => KSCTrainingCap / KerbalHealthFactorsSettings.Instance.TrainingTime * StupidityTrainingSpeedFactor;

        public float KSCTrainingPerSecond => KSCTrainingPerDay / KSPUtil.dateTimeFormatter.Day;

        const float ScienceMultiplierEffect = 2;

        public float InFlightTrainingPerDay
        {
            get
            {
                float scienceMultiplier = ProtoCrewMember.GetVessel().GetScienceMultiplier();
                return StupidityTrainingSpeedFactor * (1 - ScienceMultiplierEffect / (scienceMultiplier + ScienceMultiplierEffect)) / KerbalHealthFactorsSettings.Instance.TrainingTime;
            }
        }

        public float TrainingPerDay => ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Assigned ? InFlightTrainingPerDay : KSCTrainingPerDay;

        public float TrainingPerSecond => TrainingPerDay / KSPUtil.dateTimeFormatter.Day;

        public PartTrainingInfo GetTrainingPart(string name) => TrainedParts.Find(tp2 => tp2.Name == name);

        public float TrainingLevelForModulePart(ModuleKerbalHealth mkh)
        {
            PartTrainingInfo tp = GetTrainingPart(mkh.PartName);
            return tp != null ? tp.Level : 0;
        }

        /// <summary>
        /// Time in seconds until KSC training for all given modules is complete
        /// </summary>
        public float TrainingETAFor(IEnumerable<ModuleKerbalHealth> modules) =>
           modules.Sum(mkh => (float)Math.Max(0, (KSCTrainingCap - TrainingLevelForModulePart(mkh)) * mkh.complexity)) / KSCTrainingPerSecond;

        /// <summary>
        /// Estimated time (in seconds) until training for all parts is complete
        /// </summary>
        public float CurrentTrainingETA => TrainedParts.Sum(tp => (float)Math.Max(0, KSCTrainingCap - tp.Level) * tp.Complexity) / KSCTrainingPerSecond;

        /// <summary>
        /// Returns true if the list of modules contains any that need training at KSC
        /// </summary>
        public bool AnyModuleTrainableAtKSC(IList<ModuleKerbalHealth> modules) => modules.Any(mkh => TrainingLevelForModulePart(mkh) < KSCTrainingCap);

        /// <summary>
        /// Returns weighted training level of the kerbal (for the current vessel, for the currently trained parts or the vessel in the editor)
        /// </summary>
        public float GetTrainingLevel(bool simulateTrained = false)
        {
            float totalTraining = 0, totalComplexity = 0;

            if (IsInEditor)
            {
                List<ModuleKerbalHealth> trainableParts = EditorLogic.SortedShipList.GetTrainableModules();
                for (int i = 0; i < trainableParts.Count; i++)
                {
                    ModuleKerbalHealth mkh = trainableParts[i];
                    totalTraining += (simulateTrained ? Math.Max(KSCTrainingCap, TrainingLevelForModulePart(mkh)) : TrainingLevelForModulePart(mkh)) * mkh.complexity;
                    totalComplexity += mkh.complexity;
                }
            }

            else for (int i = 0; i < TrainedParts.Count; i++)
                    if (TrainedParts[i].TrainingNow)
                    {
                        PartTrainingInfo tp = TrainedParts[i];
                        totalTraining += tp.Complexity * tp.Count * tp.Level;
                        totalComplexity += tp.Complexity * tp.Count;
                    }

            return totalComplexity != 0 ? totalTraining / totalComplexity : KSCTrainingCap;
        }

        /// <summary>
        /// Start training the kerbal for a set of parts; also abandons all previous trainings
        /// </summary>
        public void StartTraining(IList<Part> parts, string vesselName)
        {
            Log($"KerbalHealthStatus.StartTraining({parts.Count} parts, '{vesselName}') for {name}");

            // First stopping training for all parts to prepare for updating the list
            StopTraining(IsTrainingAtKSC && !IsInEditor ? "#KH_TrainingStopped" : null);

            // Restarting training for all currently trainable parts
            int count = 0;
            List<ModuleKerbalHealth> trainableModules = parts.GetTrainableModules();
            for (int i = 0; i < trainableModules.Count; i++)
            {
                ModuleKerbalHealth mkh = trainableModules[i];
                PartTrainingInfo trainingInfo = GetTrainingPart(mkh.PartName);
                if (trainingInfo != null)
                {
                    trainingInfo.Complexity = mkh.complexity;
                    if (IsTrainingAtKSC && trainingInfo.KSCTrainingComplete)
                        continue;
                    else trainingInfo.StartTraining();
                }
                else TrainedParts.Add(new PartTrainingInfo(mkh.PartName, mkh.complexity, 1, KerbalHealthFactorsSettings.Instance.TrainingEnabled ? 0 : KSCTrainingCap));
                Log($"Now training for {mkh.PartName} (complexity: {mkh.complexity}).");
                count++;
            }

            if (count > 0)
            {
                TrainingVessel = vesselName;
                Log($"Training {name} for {vesselName} ({count} untrained parts).");
            }
        }

        public void StopTraining(string messageTag)
        {
            Log($"Stopping training of {name}.");
            if (messageTag != null)
                ShowMessage(Localizer.Format(messageTag, ProtoCrewMember.nameWithGender, TrainingVessel), ProtoCrewMember);
            for (int i = 0; i < TrainedParts.Count; i++)
                TrainedParts[i].StopTraining();
            RemoveCondition(Condition_Training);
            TrainingVessel = null;
            LastRealTrainingPerDay = 0;
        }

        void Train(float interval)
        {
            Log($"KerbalHealthStatus.Train({interval} s) for {name}");
            bool inflight = ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Assigned;

            // Step 1: Calculating training complexity of all yet untrained parts
            List<PartTrainingInfo> untrainedParts = TrainedParts.Where(tp => tp.TrainingNow).ToList();
            float totalComplexity = untrainedParts.Sum(tp => tp.Complexity * tp.Count);
            Log($"{name} is training for {untrainedParts.Count} parts. Total complexity: {totalComplexity}.");
            if (totalComplexity <= 0)
            {
                Log("No parts need training.");
                StopTraining("#KH_TrainingComplete");
                return;
            }
            float trainingProgress = interval * TrainingPerSecond / totalComplexity;
            if (IsLogging())
                Log($"Overall training progress: {TrainingPerDay:P2} per unit of complexity per day.");
            if (trainingProgress <= 0)
            {
                LastRealTrainingPerDay = 0;
                return;
            }

            // Step 2: Updating parts' training progress and calculating their base complexity to update vessel's training level
            bool trainingComplete = true;
            float totalTrainingIncrease = 0;
            for (int i = 0; i < untrainedParts.Count; i++)
            {
                PartTrainingInfo tp = untrainedParts[i];
                float partTrainingProgress = trainingProgress;
                if (inflight)
                {
                    partTrainingProgress *= InFlightTrainingCap - tp.Level;
                    totalTrainingIncrease += InFlightTrainingCap - tp.Level;
                }
                tp.Level += partTrainingProgress;
                Log($"Training level for part {tp.Name} x{tp.Count} with complexity {tp.Complexity} increases by {partTrainingProgress * KSPUtil.dateTimeFormatter.Day / interval:P2} per day and is currently {tp.Level:P3}.");
                if (!inflight && tp.KSCTrainingComplete)
                {
                    Log($"Training for part {tp.Name} complete.");
                    tp.Level = KSCTrainingCap;
                    tp.StopTraining();
                }
                else trainingComplete = false;
            }

            LastRealTrainingPerDay = inflight ? TrainingPerDay * totalTrainingIncrease / untrainedParts.Count / totalComplexity : TrainingPerDay / totalComplexity;
            if (trainingComplete)
                StopTraining("#KH_TrainingComplete");
        }

        #endregion TRAINING

        #region RADIATION

        /// <summary>
        /// Lifetime absorbed dose of ionizing radiation, in banana equivalent doses (BEDs, 1 BED = 1e-7 Sv)
        /// </summary>
        public double Dose { get; set; }

        /// <summary>
        /// Returns the fraction of max HP that the kerbal has considering radiation effects. 1e7 of RadiationDose = -25% of MaxHP
        /// </summary>
        public double RadiationMaxHPModifier => KerbalHealthRadiationSettings.Instance.RadiationEnabled ? 1 - Dose * 1e-7 * KerbalHealthRadiationSettings.Instance.RadiationEffect : 1;

        /// <summary>
        /// Level of background radiation absorbed by the body, in bananas per day
        /// </summary>
        public double Radiation { get; set; }

        /// <summary>
        /// Proportion of radiation that gets absorbed by the kerbal
        /// </summary>
        public double Exposure => HealthEffects.VesselExposure * HealthEffects.ExposureMultiplier;

        /// <summary>
        /// Exposure in radiaiton shelter (used for radstorms)
        /// </summary>
        public double ShelterExposure => HealthEffects.ShelterExposure * HealthEffects.ExposureMultiplier;

        /// <summary>
        /// Returns true if the kerbal can start decontamination now
        /// </summary>
        public bool IsReadyForDecontamination =>
            (ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available || !KerbalHealthRadiationSettings.Instance.DecontaminationOnlyAtKSC)
            && Health >= Math.Max(KerbalHealthRadiationSettings.Instance.DecontaminationMinHealth, KerbalHealthRadiationSettings.Instance.DecontaminationHealthLoss)
            && !Conditions.Any(hc => hc.Visible)
            && (HighLogic.CurrentGame.Mode != Game.Modes.CAREER || Funding.CanAfford(KerbalHealthRadiationSettings.Instance.DecontaminationFundsCost))
            && (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX || ResearchAndDevelopment.CanAfford(KerbalHealthRadiationSettings.Instance.DecontaminationScienceCost))
            && (!KerbalHealthRadiationSettings.Instance.RequireUpgradedFacilityForDecontamination || ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) >= GetInternalFacilityLevel(KerbalHealthRadiationSettings.Instance.DecontaminationAstronautComplexLevel))
            && (!KerbalHealthRadiationSettings.Instance.RequireUpgradedFacilityForDecontamination || ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment) >= GetInternalFacilityLevel(KerbalHealthRadiationSettings.Instance.DecontaminationRNDLevel));

        /// <summary>
        /// Returns true if the kerbal is currently decontaminating (i.e. has 'Decontaminating' condition)
        /// </summary>
        public bool IsDecontaminating { get; private set; }

        /// <summary>
        /// Proportion of solar radiation that reaches a vessel at a given distance from the Sun (before applying magnetosphere, atmosphere and exposure effects)
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static double GetSolarRadiationProportion(double distance) => Sqr(FlightGlobals.GetHomeBody().orbit.radius / distance);

        /// <summary>
        /// Amount of radiation that gets through the magnetosphere to the given vessel
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static double GetMagnetosphereCoefficient(Vessel v)
        {
            double cosmicRadiationRate = 1;
            double altitude = v.altitude;
            for (CelestialBody b = v.mainBody; b != Sun.Instance.sun; b = b.referenceBody)
            {
                if (PlanetConfigs[b].Magnetosphere != 0)
                    if (altitude < b.scienceValues.spaceAltitudeThreshold)
                        cosmicRadiationRate *= Math.Pow(KerbalHealthRadiationSettings.Instance.InSpaceLowCoefficient, PlanetConfigs[b].Magnetosphere);
                    else cosmicRadiationRate *= Math.Pow(KerbalHealthRadiationSettings.Instance.InSpaceHighCoefficient, PlanetConfigs[b].Magnetosphere);
                altitude = b.orbit.altitude;
            }
            return cosmicRadiationRate;
        }

        /// <summary>
        /// Amount of cosmic radiation that gets through the magnetosphere and atmosphere to the given vessel
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static double GetCosmicRadiationRate(Vessel v)
        {
            if (v == null)
            {
                Log("Vessel is null. No radiation added.", LogLevel.Important);
                return 0;
            }

            if (v.mainBody == Sun.Instance.sun)
                return 1;

            double cosmicRadiationRate = GetMagnetosphereCoefficient(v);
            if (v.mainBody.atmosphere && PlanetConfigs[v.mainBody].AtmosphericAbsorption != 0)
                if (v.altitude < v.mainBody.scienceValues.flyingAltitudeThreshold)
                    cosmicRadiationRate *= Math.Pow(KerbalHealthRadiationSettings.Instance.TroposphereCoefficient, PlanetConfigs[v.mainBody].AtmosphericAbsorption);
                else if (v.altitude < v.mainBody.atmosphereDepth)
                    cosmicRadiationRate *= Math.Pow(KerbalHealthRadiationSettings.Instance.StratoCoefficient, PlanetConfigs[v.mainBody].AtmosphericAbsorption);
            double occlusionCoefficient = (Math.Sqrt(1 - Sqr(v.mainBody.Radius) / Sqr(v.mainBody.Radius + Math.Max(v.altitude, 0))) + 1) / 2;
            return cosmicRadiationRate * occlusionCoefficient;
        }

        /// <summary>
        /// Returns level of cosmic radiation reaching the given vessel
        /// </summary>
        /// <returns>Cosmic radiation level in bananas/day</returns>
        public static double GetCosmicRadiation(Vessel v) =>
            GetCosmicRadiationRate(v) * (GetSolarRadiationProportion(v.GetDistanceToSun()) * KerbalHealthRadiationSettings.Instance.SolarRadiation + KerbalHealthRadiationSettings.Instance.GalacticRadiation)
            + GetNaturalRadiation(v);

        /// <summary>
        /// Body-emitted radiation reaching the given vessel
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static double GetNaturalRadiation(Vessel v) => PlanetConfigs[v.mainBody].Radioactivity * Sqr(v.mainBody.Radius / (v.mainBody.Radius + v.altitude));

        /// <summary>
        /// Adds given amount of radiation and reduces curent HP accordingly
        /// </summary>
        /// <param name="d"></param>
        public void AddDose(double d)
        {
            if (d > 0)
                HP -= d * 1e-7 * KerbalHealthRadiationSettings.Instance.RadiationEffect;
            Dose += d;
        }

        public double GetVesselRadiation(Vessel v)
        {
            if (Kerbalism.Found && KerbalHealthRadiationSettings.Instance.UseKerbalismRadiation)
            {
                if (IsLogging())
                {
                    Log($"Kerbalism environment radiaiton: {Kerbalism.GetRadiation(v) * 3600:N3} rad/h = {Kerbalism.RadPerSecToBEDPerDay(Kerbalism.GetRadiation(v))} BED/day. Kerbalism exposure: {Kerbalism.GetHabitatRadiation(v) / Kerbalism.GetRadiation(v):P1}");
                    Log($"Kerbal Health radiation: {(HealthEffects.Radioactivity + GetCosmicRadiation(v)) * KSPUtil.dateTimeFormatter.Day / 21600:N1} BED/day.");
                }
                return Kerbalism.RadPerSecToBEDPerDay(Kerbalism.GetRadiation(v)) * KerbalHealthRadiationSettings.Instance.KerbalismRadiationRatio;
            }
            return (HealthEffects.Radioactivity + GetCosmicRadiation(v)) * KSPUtil.dateTimeFormatter.Day / 21600;
        }

        public double GetRadiation()
        {
            double bedPerDay = IsDecontaminating ? -KerbalHealthRadiationSettings.Instance.DecontaminationRate : 0;
            if (ProtoCrewMember.rosterStatus != ProtoCrewMember.RosterStatus.Assigned && !IsFrozen)
                return bedPerDay;

            Vessel v = ProtoCrewMember.GetVessel();
            if (v == null)
            {
                Log($"Vessel for {Name} not found!", LogLevel.Error);
                return bedPerDay;
            }

            bedPerDay += Exposure * GetVesselRadiation(v);
            Log($"{Name}'s vessel receives {bedPerDay:N1} BED/day @ {Exposure:P1} exposure. Total accumulated dose is {Dose:N} BEDs.");
            return bedPerDay;
        }

        public void StartDecontamination()
        {
            Log($"StartDecontamination for {Name}");
            if (!IsReadyForDecontamination)
            {
                Log($"{Name} is {ProtoCrewMember.rosterStatus}; HP: {HP}/{MaxHP}; has {Conditions.Count} condition(s)", LogLevel.Error);
                return;
            }
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                Log($"Taking {KerbalHealthRadiationSettings.Instance.DecontaminationFundsCost} funds our of {Funding.Instance.Funds:N0} available for decontamination.");
                Funding.Instance.AddFunds(-KerbalHealthRadiationSettings.Instance.DecontaminationFundsCost, TransactionReasons.None);
            }
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)
            {
                Log($"Taking {KerbalHealthRadiationSettings.Instance.DecontaminationScienceCost} science points for decontamination.");
                ResearchAndDevelopment.Instance.AddScience(-KerbalHealthRadiationSettings.Instance.DecontaminationScienceCost, TransactionReasons.None);
            }
            HP *= 1 - KerbalHealthRadiationSettings.Instance.DecontaminationHealthLoss;
            AddCondition(Condition_Decontaminating);
            Radiation = -KerbalHealthRadiationSettings.Instance.DecontaminationRate;
        }

        public void StopDecontamination()
        {
            Log($"StopDecontamination for {Name}");
            RemoveCondition(Condition_Decontaminating);
        }

        #endregion RADIATION

        #region HEALTH UPDATE

        public void SetDirty() => effectsDirty = factorsDirty = true;

        /// <summary>
        /// Updates kerbal's HP and status
        /// </summary>
        /// <param name="interval">Number of seconds since the last update</param>
        public void Update(float interval)
        {
            Log($"Updating {Name}'s health.");

            if (ProtoCrewMember == null)
            {
                Log($"{Name} ProtoCrewMember record not found. Cannot update health.", LogLevel.Error);
                return;
            }

            SetDirty();

            if (ProtoCrewMember.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
                IsOnEVA = false;

            if (KerbalHealthQuirkSettings.Instance.QuirksEnabled)
                CheckForAvailableQuirks();

            if (KerbalHealthRadiationSettings.Instance.RadiationEnabled)
            {
                Radiation = GetRadiation();
                AddDose(Radiation * interval / KSPUtil.dateTimeFormatter.Day);
                if (IsDecontaminating)
                    if (Dose <= 0)
                    {
                        Dose = 0;
                        StopDecontamination();
                    }
                    else if (ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Assigned && KerbalHealthRadiationSettings.Instance.DecontaminationOnlyAtKSC)
                        StopDecontamination();
            }

            double hpChange = HPChangeTotal;
            HP += hpChange * interval / KSPUtil.dateTimeFormatter.Day;
            Log($"Total HP change: {hpChange:F2}");

            // Check if the kerbal dies
            if (HP <= 0 && KerbalHealthGeneralSettings.Instance.DeathEnabled)
            {
                Log($"{Name} dies due to having {HP} health. Their condition was: {ConditionString}.", LogLevel.Important);
                ShowMessage(
                    VisibleConditions.Any()
                        ? Localizer.Format("#KH_Condition_KerbalDied_Conditions", ProtoCrewMember.nameWithGender, ConditionString)
                        : Localizer.Format("#KH_Condition_KerbalDied_NoConditions", ProtoCrewMember.nameWithGender),
                    true);
                ProtoCrewMember.seat?.part.RemoveCrewmember(ProtoCrewMember);
                ProtoCrewMember.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                Vessel.CrewWasModified(ProtoCrewMember.GetVessel());
                return;
            }

            // Training
            if (TrainingVessel != null)
                // If KSC training no longer possible, stop it
                if (IsTrainingAtKSC && ConditionsPreventKSCTraining)
                {
                    Log($"{name} is no longer able to train at KSC (condition: {ConditionString}).");
                    StopTraining("#KH_TrainingStopped");
                }
                // Train
                else if (ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Assigned
                    || (ProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available && IsTrainingAtKSC))
                    Train(interval);
                // Stop training after the kerbal has been recovered
                else if (ProtoCrewMember.rosterStatus != ProtoCrewMember.RosterStatus.Assigned && !IsTrainingAtKSC)
                    StopTraining(null);

            // Adding/removing Exhausted condition
            if (HasCondition(Condition_Exhausted))
            {
                if (HP >= CriticalHP)
                {
                    if (IsLogging())
                        Log($"{Name}'s health is {HP:F2} HP. Exhaustion end MTBE: {CriticalHP / (HP - CriticalHP) * KerbalHealthGeneralSettings.Instance.ExhaustionMaxMTTH:F1} hours.");
                    if (EventHappens(CriticalHP / (HP - CriticalHP) * KerbalHealthGeneralSettings.Instance.ExhaustionMaxMTTH * 3600, interval))
                        RemoveCondition(Condition_Exhausted);
                }
            }
            else if (HP < CriticalHP)
            {
                if (IsLogging())
                    Log($"{Name}'s health is at {Health:P2}. Exhaustion start MTBE: {HP / CriticalHP * KerbalHealthGeneralSettings.Instance.ExhaustionMaxMTTH:F1} hours.");
                if (EventHappens(HP / CriticalHP * KerbalHealthGeneralSettings.Instance.ExhaustionMaxMTTH * 3600, interval))
                    AddCondition(Condition_Exhausted);
            }
        }

        #endregion HEALTH UPDATE

        #region SAVING, LOADING, INITIALIZING ETC.

        public const string ConfigNodeName = "KerbalHealthStatus";

        public void Save(ConfigNode node)
        {
            Log($"Saving {Name}'s KerbalHealthStatus into a config node.");
            ConfigNode n2;
            node.AddValue("name", Name);
            node.AddValue("health", HP);
            int n = 0;
            foreach (KeyValuePair<HealthFactor, double> f in factorsOriginal.Where(kvp => kvp.Value != 0))
            {
                n2 = new ConfigNode(HealthFactor.ConfigNodeName);
                n2.AddValue("name", f.Key.Name);
                n2.AddValue("change", f.Value);
                node.AddNode(n2);
                n++;
            }
            Log($"Saved {n} non-zero factors.");
            if (LocationEffect != null)
            {
                LocationEffect.Save(n2 = new ConfigNode(HealthEffect.ConfigNodeName));
                node.AddNode(n2);
            }
            node.AddValue("dose", Dose);
            if (Radiation != 0)
                node.AddValue("radiation", Radiation);
            for (int i = 0; i < Conditions.Count; i++)
                node.AddValue("condition", Conditions[i].Name);
            for (int i = 0; i < Quirks.Count; i++)
                node.AddValue("quirk", Quirks[i].Name);
            if (QuirkLevel != 0)
                node.AddValue("quirkLevel", QuirkLevel);
            if (!IsCapable)
                node.AddValue("trait", Trait);
            if (IsOnEVA)
                node.AddValue("onEva", true);
            if (TrainingVessel != null)
                node.AddValue("trainingVessel", TrainingVessel);
            for (int i = 0; i < TrainedParts.Count; i++)
            {
                TrainedParts[i].Save(n2 = new ConfigNode(PartTrainingInfo.ConfigNodeName));
                node.AddNode(n2);
            }
        }

        public void Load(ConfigNode node)
        {
            name = node.GetValue("name");
            hp = node.GetDouble("health", GetDefaultMaxHP(ProtoCrewMember));
            foreach (ConfigNode factorNode in node.GetNodes(HealthFactor.ConfigNodeName))
            {
                HealthFactor healthFactor = GetHealthFactor(factorNode.GetValue("name"));
                if (healthFactor != null)
                    factorsOriginal[healthFactor] = factorNode.GetDouble("change");
            }
            if (node.HasNode(HealthEffect.ConfigNodeName))
                LocationEffect = new HealthEffect(node.GetNode(HealthEffect.ConfigNodeName));
            Dose = node.GetDouble("dose");
            Radiation = node.GetDouble("radiation");
            Conditions = node.GetValues("condition").Select(s => GetHealthCondition(s)).ToList();
            RecalculateConditions();
            foreach (string s in node.GetValues("quirk"))
                AddQuirk(s);
            QuirkLevel = node.GetInt("quirkLevel");
            Trait = node.GetValue("trait");
            IsOnEVA = node.GetBool("onEva");
            TrainingVessel = node.GetString("trainingVessel");
            TrainedParts = node.GetNodes(PartTrainingInfo.ConfigNodeName).Select(n => new PartTrainingInfo(n)).ToList();
            // Loading familiar part types from pre-1.6 versions
            foreach (string partName in node.GetValuesList("familiarPartType").Where(partName => PartLoader.getPartInfoByName(partName) != null))
            {
                Log($"Loading training part {partName} from a pre-1.6 save.");
                PartTrainingInfo trainingPart = GetTrainingPart(partName);
                if (trainingPart != null)
                    trainingPart.Level = Math.Max(trainingPart.Level, KSCTrainingCap);
                else TrainedParts.Add(new PartTrainingInfo(partName, 0, 0, KSCTrainingCap));
            }
            if (!KerbalHealthFactorsSettings.Instance.TrainingEnabled && IsTrainingAtKSC)
                StopTraining(null);
            Log($"{Name} loaded.");
        }

        public KerbalHealthStatus(ProtoCrewMember pcm)
        {
            if (pcm == null)
            {
                Log($"Trying to create KerbalHealthStatus for a null ProtoCrewMember!", LogLevel.Error);
                return;
            }
            Name = pcm.name;
            HP = GetDefaultMaxHP(pcm);
            if (IsLogging())
                Log($"Created KerbalHealthStatus record for {pcm.name} with {HP} HP.");
        }

        public KerbalHealthStatus(ConfigNode node) => Load(node);

        public override bool Equals(object obj) => ((KerbalHealthStatus)obj).Name.Equals(Name);

        public override int GetHashCode()
        {
            ConfigNode node = new ConfigNode(ConfigNodeName);
            Save(node);
            return node.GetHashCode();
        }

        public KerbalHealthStatus Clone() => (KerbalHealthStatus)MemberwiseClone();

        #endregion SAVING, LOADING, INITIALIZING ETC.
    }
}
