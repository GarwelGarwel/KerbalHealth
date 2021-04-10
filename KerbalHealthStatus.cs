using KerbalHealth.Wrappers;
using KSP.Localization;
using Smooth.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KerbalHealth
{
    /// <summary>
    /// Contains data about a kerbal's health
    /// </summary>
    public class KerbalHealthStatus
    {
        #region BASIC PROPERTIES

        string name;
        string trait = null;

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
            $"{Name}{(KerbalHealthGeneralSettings.Instance.ShowTraitLevel ? $" ({Localizer.Format($"#KH_TraitSymbol_{PCM.trait}")}{PCM.experienceLevel})" : "")}";

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
        public ProtoCrewMember PCM
        {
            get
            {
                if (pcmCached != null)
                    return pcmCached;
                try { return pcmCached = HighLogic.fetch.currentGame.CrewRoster[Name]; }
                catch (ArgumentOutOfRangeException)
                {
                    Core.Log($"Could not find ProtoCrewMember for {Name}. KerbalHealth kerbal list contains {Core.KerbalHealthList.Count} records.");
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

                switch (PCM.rosterStatus)
                {
                    case ProtoCrewMember.RosterStatus.Available:
                        return Localizer.Format("#KH_Location_status1");//"KSC"
                    case ProtoCrewMember.RosterStatus.Dead:
                        return Localizer.Format("#KH_Location_status2");//"Dead"
                    case ProtoCrewMember.RosterStatus.Missing:
                        return Localizer.Format("#KH_Location_status3");//"Unknown"
                    case (ProtoCrewMember.RosterStatus﻿)9001:
                        return Localizer.Format("#KH_Location_status4");//"On Vacation"
                }

                Vessel v = PCM.GetVessel();
                if (v == null)
                    return "???";
                if (v.isEVA)
                    return Localizer.Format("#KH_Location_status5", v.mainBody.bodyName);//"EVA (" +  + ")"
                return v.vesselName;
            }
        }

        /// <summary>
        /// Returns saved kerbal's trait or current trait if nothing is saved
        /// </summary>
        string Trait
        {
            get => trait ?? PCM.trait;
            set => trait = value;
        }

        #endregion BASIC PROPERTIES

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
            Core.Log($"CalculateLocationEffectInFlight for {Name}");
            if (PCM.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
            {
                Core.Log($"{Name} is not loaded.");
                locationEffect = null;
                return;
            }

            if (!PCM.IsLoaded())
                return;

            if (IsOnEVA)
            {
                // The kerbal is on EVA => hard-coded vesselEffect
                Core.Log($"{Name} is on EVA => setting exposure to appropriate value.");
                locationEffect = new HealthEffect()
                { ExposureMultiplier = KerbalHealthRadiationSettings.Instance.EVAExposure };
            }
            else
            {
                // The kerbal is in a vessel => recalculate vesselEffect & partEffect
                Vessel v = Core.GetVessel(PCM);
                Core.Log($"{Name} is in {v.vesselName}. It is {(v.loaded ? "" : "NOT ")}loaded.");
                locationEffect = new HealthEffect(v);
                locationEffect.ProcessPart(PCM.GetCrewPart(), v.GetCrewCount(), true);
            }
        }

        void CalculateLocationEffectInEditor()
        {
            if (ShipConstruction.ShipManifest == null || !ShipConstruction.ShipManifest.Contains(PCM))
                return;
            Core.Log($"CalculateLocationEffectInEditor for {Name}");
            locationEffect = new HealthEffect(EditorLogic.SortedShipList, ShipConstruction.ShipManifest.CrewCount);
            Part p = EditorLogic.SortedShipList.Find(part => part.protoModuleCrew.Contains(PCM));
            if (p != null)
            {
                Core.Log($"{Name} is in part {p.partName}.");
                locationEffect.ProcessPart(p, ShipConstruction.ShipManifest.CrewCount, true);
            }
            Core.Log($"Resulting location effect:\n{locationEffect}");
        }

        void CalculateQuirkEffects()
        {
            Core.Log($"CalculateQuirkEffects for {Name}");
            quirksEffect = new HealthEffect();
            foreach (HealthEffect effect in Quirks.SelectMany(q => q.GetApplicableEffects(this)))
            {
                Core.Log($"Applying quirk effect: {effect}");
                quirksEffect.CombineWith(effect);
                Core.Log($"Quirks effect:\n{quirksEffect}");
            }
        }

        void CalculateEffects()
        {
            Core.Log($"Calculating all effects for {Name}.");
            effectsDirty = false;
            if (Core.IsInEditor)
                CalculateLocationEffectInEditor();
            else CalculateLocationEffectInFlight();
            if (KerbalHealthQuirkSettings.Instance.QuirksEnabled)
                CalculateQuirkEffects();
            else quirksEffect = new HealthEffect();
            totalEffect = HealthEffect.Combine(LocationEffect, QuirksEffect);
            Core.Log($"Total effect:\n{totalEffect}");
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

        /// <summary>
        /// Whether the kerbal is frozen by DeepFreeze mod (has 'Frozen' condition)
        /// </summary>
        public bool IsFrozen
        {
            get => HasCondition(Condition_Frozen);
            set
            {
                if (value)
                    AddCondition(Condition_Frozen);
                else RemoveCondition(Condition_Frozen);
            }
        }

        /// <summary>
        /// Returns a comma-separated list of visible conditions or "OK" if there are no visible conditions
        /// </summary>
        public string ConditionString
        {
            get
            {
                string res = "";
                foreach (HealthCondition hc in Conditions.Where(hc => hc.Visible))
                {
                    if (res.Length != 0)
                        res += ", ";
                    res += hc.Title;
                }
                if (res.Length == 0)
                    res = Localizer.Format("#KH_NoConditions");
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
        public double ExhaustionStart => KerbalHealthGeneralSettings.Instance.ExhaustionStartHealth * HealthEffects.ExhaustedStart;

        /// <summary>
        /// HP for Exhaustion condition to kick in
        /// </summary>
        public double ExhaustionStartHP => ExhaustionStart * MaxHP;

        /// <summary>
        /// Health level (in percentage) for Exhaustion condition to end
        /// </summary>
        public double ExhaustionEnd => KerbalHealthGeneralSettings.Instance.ExhaustionEndHealth * HealthEffects.ExhaustedEnd;

        /// <summary>
        /// HP for Exhaustion condition to end
        /// </summary>
        public double ExhaustionEndHP => ExhaustionEnd * MaxHP;

        /// <summary>
        /// Returns the condition with a given name, if present (null otherwise)
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public HealthCondition GetCondition(string condition) => Conditions.Find(hc => hc.Name == condition);

        /// <summary>
        /// Returns true if a given condition exists for the kerbal
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public bool HasCondition(string condition) => Conditions.Exists(hc => hc.Name == condition);

        /// <summary>
        /// Returns true if a given condition exists for the kerbal
        /// </summary>
        /// <param name="hc"></param>
        /// <returns></returns>
        public bool HasCondition(HealthCondition hc) => Conditions.Contains(hc);

        /// <summary>
        /// Adds a new health condition
        /// </summary>
        /// <param name="condition">Condition to add</param>
        public void AddCondition(HealthCondition condition)
        {
            if (condition == null)
                return;
            Core.Log($"Adding {condition.Name} condition to {Name}...");
            if (!condition.Stackable && HasCondition(condition))
                return;

            Conditions.Add(condition);
            if (KerbalHealthQuirkSettings.Instance.ConditionsEnabled)
                HP += condition.HP * KerbalHealthQuirkSettings.Instance.ConditionsEffect;
            Core.Log($"{condition.Name} condition added to {Name}.", LogLevel.Important);
            if (condition.Incapacitated)
                MakeIncapacitated();
            if (condition.Visible)
                Core.ShowMessage(Localizer.Format("#KH_Condition_Acquired", Name, condition.Title) + condition.Description, PCM);// "<color=\"white\">" + " has acquired " +  + "</color> condition!\r\n\n"
        }

        public void AddCondition(string condition) => AddCondition(Core.GetHealthCondition(condition));

        /// <summary>
        /// Removes a condition with from the kerbal
        /// </summary>
        /// <param name="condition">Condition to remove</param>
        /// <param name="removeAll">If true, all conditions with the same name will be removed. Makes sense for additive conditions. Default is false</param>
        public void RemoveCondition(HealthCondition condition, bool removeAll = false)
        {
            if (condition == null)
                return;
            Core.Log($"Removing {condition.Name} condition from {Name}.", LogLevel.Important);

            int n = 0;
            if (removeAll)
            {
                while (Conditions.Remove(condition))
                    n++;
                Core.Log($"{n} instance(s) of {condition.Name} removed.", LogLevel.Important);
            }
            else n = Conditions.Remove(condition) ? 1 : 0;
            if (KerbalHealthQuirkSettings.Instance.ConditionsEnabled && condition.RestoreHP)
                HP -= condition.HP * n * KerbalHealthQuirkSettings.Instance.ConditionsEffect;
            if (n > 0 && condition.Incapacitated && IsCapable)
                MakeCapable();
            if (n > 0 && condition.Visible)
                Core.ShowMessage(Localizer.Format("#KH_Condition_Lost", Name, condition.Title), PCM);
        }

        public void RemoveCondition(string condition, bool removeAll = false) => RemoveCondition(Core.GetHealthCondition(condition), removeAll);

        //void LogInventory() =>
        //    Core.Log($"{Name}'s ({PCM.trait}) inventory node contains {PCM.KerbalInventoryModule.InventoryItemCount} items: {PCM.InventoryNode.GetNodes("STOREDPART").Select(node => $"{Core.GetString(node, "partName", "N/A")} ")}");


        /// <summary>
        /// Turn a kerbal into a Tourist
        /// </summary>
        void MakeIncapacitated()
        {
            if (Trait != null && PCM.type == ProtoCrewMember.KerbalType.Tourist)
            {
                Core.Log($"{Name} is already incapacitated.", LogLevel.Important);
                return;
            }
            //LogInventory();
            Core.Log($"{Name} ({Trait}) is incapacitated.", LogLevel.Important);
            Trait = PCM.trait;
            PCM.type = ProtoCrewMember.KerbalType.Tourist;
            KerbalRoster.SetExperienceTrait(PCM, KerbalRoster.touristTrait);
            //LogInventory();
        }

        /// <summary>
        /// Revives a kerbal after being incapacitated
        /// </summary>
        void MakeCapable()
        {
            // Check if the kerbal has already been revived by another mod
            if (PCM.type != ProtoCrewMember.KerbalType.Tourist)
                return;
            //LogInventory();
            Core.Log($"{Name} is becoming {Trait ?? "something strange"} again.", LogLevel.Important);
            if (Trait != null && Trait != "Tourist")
            {
                PCM.type = ProtoCrewMember.KerbalType.Crew;
                KerbalRoster.SetExperienceTrait(PCM, Trait);
            }
            Trait = null;
            //LogInventory();
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
        public void AddQuirk(string quirk)
        {
            Quirk q = Core.GetQuirk(quirk);
            if (q == null)
                q = new Quirk(quirk);
            Core.Quirks.Add(q);
            AddQuirk(q);
        }

        public Quirk GetRandomQuirk(int level)
        {
            List<Quirk> availableQuirks = new List<Quirk>();
            List<double> weights = new List<double>();
            double weightSum = 0;
            foreach (Quirk q in Core.Quirks.Where(q => q.IsVisible && q.IsAvailableTo(this, level) && !Quirks.Contains(q)))
            {
                availableQuirks.Add(q);
                double w = KerbalHealthQuirkSettings.Instance.StatsAffectQuirkWeights
                    ? GetQuirkWeight(PCM.courage, q.CourageWeight) * GetQuirkWeight(PCM.stupidity, q.StupidityWeight)
                    : 1;
                weightSum += w;
                weights.Add(w);
                Core.Log($"Available quirk: {q.Name} (weight {w})");
            }

            if ((availableQuirks.Count == 0) || (weightSum <= 0))
            {
                Core.Log($"No available quirks found for {Name} (level {level}).", LogLevel.Important);
                return null;
            }

            double r = Core.rand.NextDouble() * weightSum;
            Core.Log($"Quirk selection roll: {r} out of {weightSum}.");
            for (int i = 0; i < availableQuirks.Count; i++)
            {
                r -= weights[i];
                if (r < 0)
                {
                    Core.Log($"Quirk {availableQuirks[i].Name} selected.");
                    return availableQuirks[i];
                }
            }
            Core.Log("Something is terribly wrong with quirk selection!", LogLevel.Error);
            return null;
        }

        public Quirk AddRandomQuirk(int level)
        {
            Quirk q = GetRandomQuirk(level);
            if (q != null)
            {
                Quirks.Add(q);
                Core.ShowMessage(Localizer.Format("#KH_Condition_Quirk", Name, q), PCM);//"<color="white"><<1>></color> acquired a new quirk: <<2>>
            }
            return q;
        }

        public Quirk AddRandomQuirk() => AddRandomQuirk(PCM.experienceLevel);

        public void CheckForAvailableQuirks()
        {
            if (KerbalHealthQuirkSettings.Instance.AwardQuirksOnMissions || (PCM.rosterStatus == ProtoCrewMember.RosterStatus.Available))
            {
                for (int l = QuirkLevel + 1; l <= PCM.experienceLevel; l++)
                {
                    if (Quirks.Count >= KerbalHealthQuirkSettings.Instance.MaxQuirks)
                        break;
                    if (Core.rand.NextDouble() < KerbalHealthQuirkSettings.Instance.QuirkChance)
                    {
                        Core.Log($"A quirk will be added to {Name} (level {l}).");
                        AddRandomQuirk(l);
                    }
                    else Core.Log($"No quirks will be added to {Name} (level {l}).");
                }
                QuirkLevel = PCM.experienceLevel;
            }
        }

        static double GetQuirkWeight(double val, double k) => val * (2 - 4 / (k + 1)) + 2 / (k + 1);

        #endregion QUIRKS

        #region TRAINING

        /// <summary>
        /// List of part ids and their training levels
        /// </summary>
        public Dictionary<uint, double> TrainingLevels { get; set; } = new Dictionary<uint, double>();

        /// <summary>
        /// List of part type ids (e.g. mk1pod) the kerbal has completed training for at least once (speeds up further training for similar types)
        /// </summary>
        public HashSet<string> FamiliarPartTypes { get; set; } = new HashSet<string>();

        /// <summary>
        /// List of vessels the kerbal has trained for (information only)
        /// </summary>
        public Dictionary<string, double> TrainedVessels { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Ids of parts the kerbal is currently training for
        /// </summary>
        public List<TrainingPart> TrainingFor { get; set; } = new List<TrainingPart>();

        public bool IsTraining => HasCondition(Condition_Training);

        /// <summary>
        /// Name of the vessel the kerbal is currently training for (information only)
        /// </summary>
        public string TrainingVessel { get; set; }

        /// <summary>
        /// Returns true if the kerbal satisfies all conditions to be trained at KSC
        /// </summary>
        public bool CanTrainAtKSC => (PCM.rosterStatus == ProtoCrewMember.RosterStatus.Available) && (Health >= 0.9);

        public double TrainingPerDay => Core.TrainingCap /
                    (double)((PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned)
                    ? KerbalHealthFactorsSettings.Instance.InFlightTrainingTime
                    : KerbalHealthFactorsSettings.Instance.KSCTrainingTime)
                    / (1 + PCM.stupidity * KerbalHealthFactorsSettings.Instance.StupidityPenalty);

        /// <summary>
        /// Estimated time (in seconds) until training for all parts is complete
        /// </summary>
        public double TrainingETA =>
            TrainingFor.Sum(tp => (Core.TrainingCap - TrainingLevels[tp.Id]) * GetPartTrainingComplexity(tp)) / TrainingPerDay * KSPUtil.dateTimeFormatter.Day;

        public double TrainingLevel
        {
            get
            {
                if (KerbalHealthFactorsSettings.Instance.TrainingEnabled)
                {
                    double totalTraining = TrainingFor.Sum(tp => TrainingLevels[tp.Id] * tp.Complexity);
                    double totalComplexity = TrainingFor.Sum(tp => tp.Complexity);
                    totalTraining = (totalComplexity != 0) ? totalTraining / totalComplexity : Core.TrainingCap;
                    if (TrainingVessel != null)
                        TrainedVessels[TrainingVessel] = totalTraining;
                    return totalTraining;
                }
                return Core.TrainingCap;
            }
        }

        /// <summary>
        /// Returns true if the kerbal has completed at least one training for the given part type
        /// </summary>
        /// <param name="partName"></param>
        /// <returns></returns>
        public bool IsFamiliarWithPartType(string partName) => FamiliarPartTypes.Contains(partName);

        public double TrainingLevelForPart(uint id) => TrainingLevels.ContainsKey(id) ? TrainingLevels[id] : 0;

        public double GetPartTrainingComplexity(TrainingPart tp) =>
            IsFamiliarWithPartType(tp.Name) ? tp.Complexity * (1 - KerbalHealthFactorsSettings.Instance.FamiliarityBonus) : tp.Complexity;

        public double GetPartTrainingComplexity(ModuleKerbalHealth mkh) =>
            IsFamiliarWithPartType(mkh.part.name) ? mkh.complexity * (1 - KerbalHealthFactorsSettings.Instance.FamiliarityBonus) : mkh.complexity;

        /// <summary>
        /// Start training the kerbal for a set of parts; also abandons all previous trainings
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="vesselName"></param>
        public void StartTraining(List<Part> parts, string vesselName)
        {
            Core.Log($"KerbalHealthStatus.StartTraining({parts.Count} parts, '{vesselName}') for {name}");
            TrainingFor.Clear();
            if (IsOnEVA)
            {
                TrainingVessel = null;
                return;
            }

            foreach (ModuleKerbalHealth mkh in Core.GetTrainingCapableParts(parts))
            {
                if (!TrainingLevels.ContainsKey(mkh.id))
                    TrainingLevels.Add(mkh.id, 0);
                TrainingFor.Add(new TrainingPart(mkh.id, mkh.part.name, mkh.complexity));
                Core.Log($"Now training for {mkh.part.name} with id {mkh.id}.");
            }

            TrainingVessel = vesselName;
            TrainedVessels[vesselName] = TrainingLevel;
            if (TrainedVessels[vesselName] >= Core.TrainingCap)
                FinishTraining(true);
            Core.Log($"Training {name} for {vesselName} ({TrainingFor.Count} parts).");
        }

        public void FinishTraining(bool silent = false)
        {
            Core.Log($"Training of {name} is complete.");
            if (!silent)
                Core.ShowMessage(Localizer.Format("#KH_TrainingComplete", name, TrainingVessel), PCM);//Training of " +  + " for " +  + " is complete!
            RemoveCondition(Condition_Training);
            TrainingFor.Clear();
            TrainingVessel = null;
        }

        void Train(double interval)
        {
            Core.Log($"KerbalHealthStatus.Train({interval}) for {name}");
            if (IsOnEVA)
            {
                Core.Log($"{name} is on EVA. No training.");
                return;
            }
            Core.Log($"{name} is training for {TrainingFor.Count} parts.");

            // Step 1: Calculating training complexity of all not yet trained-for parts
            double totalComplexity = TrainingFor.Where(tp => TrainingLevels[tp.Id] < Core.TrainingCap).Sum(tp => GetPartTrainingComplexity(tp));
            if (totalComplexity == 0)
            {
                Core.Log("No parts in need of training found.", LogLevel.Important);
                FinishTraining();
                return;
            }
            bool trainingComplete = true;
            double totalTraining = 0, trainingProgress = interval * TrainingPerDay / KSPUtil.dateTimeFormatter.Day / totalComplexity;  // Training progress is inverse proportional to total complexity of parts
            Core.Log($"Training progress: {trainingProgress:P}. Training cap: {Core.TrainingCap:P0}.");

            // Step 1: Updating parts' training progress and calculating their base complexity to update vessel's training level
            totalComplexity = 0;
            foreach (TrainingPart tp in TrainingFor)
            {
                TrainingLevels[tp.Id] = Math.Min(TrainingLevels[tp.Id] + trainingProgress, Core.TrainingCap);
                totalTraining += TrainingLevels[tp.Id] * tp.Complexity;
                totalComplexity += tp.Complexity;
                if (TrainingLevels[tp.Id] < Core.TrainingCap)
                    trainingComplete = false;
                else
                {
                    Core.Log($"Training for part {tp.Name} with id {tp.Id} complete.");
                    FamiliarPartTypes.Add(tp.Name);
                }
                Core.Log($"Training level for part {tp.Name} with id {tp.Id} is {TrainingLevels[tp.Id]:P2} with complexity {tp.Complexity:P0}.");
            }
            if (TrainingVessel != null)
                TrainedVessels[TrainingVessel] = totalTraining / totalComplexity;
            if (trainingComplete)
                FinishTraining();
        }

        #endregion TRAINING

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
                hp = value < 0
                    ? 0 :
                    (value > MaxHP ? MaxHP : value);
                if (!IsWarned && Health < KerbalHealthGeneralSettings.Instance.LowHealthAlert)
                {
                    Core.ShowMessage(Localizer.Format("#KH_Condition_LowHealth", Name), PCM);//"<color=\"white\">" +  + "</color>'s health is dangerously low!"
                    IsWarned = true;
                }
                else if (IsWarned && Health >= KerbalHealthGeneralSettings.Instance.LowHealthAlert)
                    IsWarned = false;
            }
        }

        /// <summary>
        /// Returns the max number of HP for the kerbal (including the modifier)
        /// </summary>
        public double MaxHP => (GetDefaultMaxHP(PCM) + HealthEffects.MaxHPBonus) * HealthEffects.MaxHP * RadiationMaxHPModifier;

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

        public double HPChangeFactors => Factors.Sum(kvp => kvp.Value);

        public double Recuperation => HealthEffects.EffectiveRecuperation;

        public double Decay => HealthEffects.Decay;

        public double HPChangeMarginal => (Recuperation / 100) * (MaxHP - HP) - (Decay / 100) * HP;

        public double HPChangeTotal => HPChangeFactors + HPChangeMarginal;

        public void CalculateFactors()
        {
            factorsDirty = false;
            bool isLoaded = PCM.IsLoaded(), inEditor = Core.IsInEditor;
            if (!inEditor && PCM.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
            {
                FactorsOriginal.Clear();
                if (IsFrozen || IsDecontaminating)
                    return;
            }

            // Getting factors' HP change per day for non-constant factors only, unless the kerbal is loaded or the scene is editor
            foreach (HealthFactor f in Core.Factors.Where(f => isLoaded || inEditor || !f.ConstantForUnloaded))
            {
                FactorsOriginal[f] = f.ChangePerDay(this);
                Core.Log($"{f.Name} factor is {FactorsOriginal[f]:F1} HP/day.");
            }
            Core.Log($"Factors HP change before effects: {FactorsOriginal.Sum(kvp => kvp.Value)} HP/day.");
        }

        public double GetFactorHPChange(HealthFactor factor) => Factors.ContainsKey(factor) ? Factors[factor] : 0;

        /// <summary>
        /// How many seconds left until HP reaches the given level, at the current HP change rate
        /// </summary>
        /// <param name="target">Target HP level</param>
        /// <returns></returns>
        public double TimeToValue(double target)
        {
            if (HPChangeTotal == 0)
                return double.NaN;
            double res = (target - HP) / HPChangeTotal;
            return res >= 0 ? res * KSPUtil.dateTimeFormatter.Day : double.NaN;
        }

        /// <summary>
        /// Returns HP number for the next condition (OK, Exhausted or death)
        /// </summary>
        /// <returns></returns>
        public double NextConditionHP()
        {
            if (HPChangeTotal > 0)
                return HasCondition(Condition_Exhausted) ? ExhaustionEndHP : MaxHP;
            if (HPChangeTotal < 0)
                return HasCondition(Condition_Exhausted) ? 0 : ExhaustionStartHP;
            return double.NaN;
        }

        /// <summary>
        /// Returns number of seconds until the next condition is reached
        /// </summary>
        /// <returns></returns>
        public double TimeToNextCondition() => TimeToValue(NextConditionHP());

        /// <summary>
        /// Returns HP level when marginal HP change balances out "fixed" change. If <= 0, no such level
        /// </summary>
        /// <returns></returns>
        public double GetBalanceHP()
        {
            //Core.Log($"{Name}'s last change: {LastChange}. Recuperation: {LastRecuperation}%. Decay: {LastDecay}%.");
            return Recuperation + Decay == 0
                ? (HPChangeFactors < 0 ? 0 : MaxHP)
                : (MaxHP * Recuperation + HPChangeFactors * 100) / (Recuperation + Decay);
        }

        #endregion HP CHANGE

        #region RADIATION

        /// <summary>
        /// Lifetime absorbed dose of ionizing radiation, in banana equivalent doses (BEDs, 1 BED = 1e-7 Sv)
        /// </summary>
        public double Dose { get; set; }

        /// <summary>
        /// Returns the fraction of max HP that the kerbal has considering radiation effects. 1e7 of RadiationDose = -25% of MaxHP
        /// </summary>
        public double RadiationMaxHPModifier =>
            KerbalHealthRadiationSettings.Instance.RadiationEnabled ? 1 - Dose * 1e-7 * KerbalHealthRadiationSettings.Instance.RadiationEffect : 1;

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
            (PCM.rosterStatus == ProtoCrewMember.RosterStatus.Available)
            && (Health >= 1)
            && (Conditions.Count == 0)
            && ((HighLogic.CurrentGame.Mode != Game.Modes.CAREER) || Funding.CanAfford(KerbalHealthRadiationSettings.Instance.DecontaminationFundsCost))
            && ((HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX) || ResearchAndDevelopment.CanAfford(KerbalHealthRadiationSettings.Instance.DecontaminationScienceCost))
            && (!KerbalHealthRadiationSettings.Instance.RequireUpgradedFacilityForDecontamination || ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) >= Core.GetInternalFacilityLevel(KerbalHealthRadiationSettings.Instance.DecontaminationAstronautComplexLevel))
            && (!KerbalHealthRadiationSettings.Instance.RequireUpgradedFacilityForDecontamination || ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment) >= Core.GetInternalFacilityLevel(KerbalHealthRadiationSettings.Instance.DecontaminationRNDLevel));

        /// <summary>
        /// Returns true if the kerbal is currently decontaminating (i.e. has 'Decontaminating' condition)
        /// </summary>
        public bool IsDecontaminating => HasCondition(Condition_Decontaminating);

        /// <summary>
        /// Proportion of solar radiation that reaches a vessel at a given distance from the Sun (before applying magnetosphere, atmosphere and exposure effects)
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static double GetSolarRadiationProportion(double distance) => Core.Sqr(FlightGlobals.GetHomeBody().orbit.radius / distance);

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
                if (Core.PlanetConfigs[b].Magnetosphere != 0)
                    if (altitude < b.scienceValues.spaceAltitudeThreshold)
                        cosmicRadiationRate *= Math.Pow(KerbalHealthRadiationSettings.Instance.InSpaceLowCoefficient, Core.PlanetConfigs[b].Magnetosphere);
                    else cosmicRadiationRate *= Math.Pow(KerbalHealthRadiationSettings.Instance.InSpaceHighCoefficient, Core.PlanetConfigs[b].Magnetosphere);
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
                Core.Log("Vessel is null. No radiation added.", LogLevel.Important);
                return 0;
            }

            if (v.mainBody == Sun.Instance.sun)
                return 1;

            double cosmicRadiationRate = GetMagnetosphereCoefficient(v);
            if (v.mainBody.atmosphere && (Core.PlanetConfigs[v.mainBody].AtmosphericAbsorption != 0))
                if (v.altitude < v.mainBody.scienceValues.flyingAltitudeThreshold)
                    cosmicRadiationRate *= Math.Pow(KerbalHealthRadiationSettings.Instance.TroposphereCoefficient, Core.PlanetConfigs[v.mainBody].AtmosphericAbsorption);
                else if (v.altitude < v.mainBody.atmosphereDepth)
                    cosmicRadiationRate *= Math.Pow(KerbalHealthRadiationSettings.Instance.StratoCoefficient, Core.PlanetConfigs[v.mainBody].AtmosphericAbsorption);
            double occlusionCoefficient = (Math.Sqrt(1 - Core.Sqr(v.mainBody.Radius) / Core.Sqr(v.mainBody.Radius + Math.Max(v.altitude, 0))) + 1) / 2;
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
        public static double GetNaturalRadiation(Vessel v) =>
            Core.PlanetConfigs[v.mainBody].Radioactivity * Core.Sqr(v.mainBody.Radius / (v.mainBody.Radius + v.altitude));

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
                if (Core.IsLogging())
                {
                    Core.Log($"Kerbalism environment radiaiton: {Kerbalism.GetRadiation(v) * 3600:N3} rad/h = {Kerbalism.RadPerSecToBEDPerDay(Kerbalism.GetRadiation(v))} BED/day. Kerbalism exposure: {Kerbalism.GetHabitatRadiation(v) / Kerbalism.GetRadiation(v):P1}");
                    Core.Log($"Kerbal Health radiation: {(HealthEffects.Radioactivity + GetCosmicRadiation(v)) * KSPUtil.dateTimeFormatter.Day / 21600:N1} BED/day.");
                }
                return Kerbalism.RadPerSecToBEDPerDay(Kerbalism.GetRadiation(v)) * KerbalHealthRadiationSettings.Instance.KerbalismRadiationRatio;
            }
            return (HealthEffects.Radioactivity + GetCosmicRadiation(v)) * KSPUtil.dateTimeFormatter.Day / 21600;
        }

        public double GetRadiation()
        {
            if (PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned || IsFrozen)
            {
                Vessel v = PCM.GetVessel();
                if (v == null)
                {
                    Core.Log($"Vessel for {Name} not found!", LogLevel.Error);
                    return 0;
                }

                double bedPerDay = GetVesselRadiation(v);
                Core.Log($"{Name}'s vessel receives {bedPerDay:N1} BED/day @ {Exposure:P1} exposure for a radiation level of {Radiation:N1} BED/day. Total accumulated dose is {Dose:N} BEDs.");

                return Exposure * bedPerDay;
            }
            return IsDecontaminating ? -KerbalHealthRadiationSettings.Instance.DecontaminationRate : 0;
        }

        public void StartDecontamination()
        {
            Core.Log($"StartDecontamination for {Name}");
            if (!IsReadyForDecontamination)
            {
                Core.Log($"{Name} is {PCM.rosterStatus}; HP: {HP}/{MaxHP}; has {Conditions.Count} condition(s)", LogLevel.Error);
                return;
            }
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                Core.Log($"Taking {KerbalHealthRadiationSettings.Instance.DecontaminationFundsCost} funds our of {Funding.Instance.Funds:N0} available for decontamination.");
                Funding.Instance.AddFunds(-KerbalHealthRadiationSettings.Instance.DecontaminationFundsCost, TransactionReasons.None);
            }
            if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER) || (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
            {
                Core.Log($"Taking {KerbalHealthRadiationSettings.Instance.DecontaminationScienceCost} science points for decontamination.");
                ResearchAndDevelopment.Instance.AddScience(-KerbalHealthRadiationSettings.Instance.DecontaminationScienceCost, TransactionReasons.None);
            }
            HP *= 1 - KerbalHealthRadiationSettings.Instance.DecontaminationHealthLoss;
            AddCondition(Condition_Decontaminating);
            Radiation = -KerbalHealthRadiationSettings.Instance.DecontaminationRate;
        }

        public void StopDecontamination()
        {
            Core.Log($"StopDecontamination for {Name}");
            RemoveCondition(Condition_Decontaminating);
        }

        #endregion RADIATION

        #region HEALTH UPDATE

        public void SetDirty()
        {
            effectsDirty = true;
            factorsDirty = true;
        }

        /// <summary>
        /// Updates kerbal's HP and status
        /// </summary>
        /// <param name="interval">Number of seconds since the last update</param>
        public void Update(double interval)
        {
            Core.Log($"Updating {Name}'s health.");

            if (PCM == null)
            {
                Core.Log($"{Name} ProtoCrewMember record not found. Aborting health update.", LogLevel.Error);
                return;
            }

            SetDirty();

            if (PCM.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
                IsOnEVA = false;

            if (KerbalHealthQuirkSettings.Instance.QuirksEnabled)
                CheckForAvailableQuirks();

            if (KerbalHealthRadiationSettings.Instance.RadiationEnabled)
            {
                Radiation = GetRadiation();
                AddDose(Radiation * interval / KSPUtil.dateTimeFormatter.Day);
                if (IsDecontaminating)
                {
                    if (Dose <= 0)
                    {
                        Dose = 0;
                        StopDecontamination();
                    }
                    if (PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned)
                        StopDecontamination();
                }
            }

            HP += HPChangeTotal * interval / KSPUtil.dateTimeFormatter.Day;

            if ((HP <= 0) && KerbalHealthGeneralSettings.Instance.DeathEnabled)
            {
                Core.Log($"{Name} dies due to having {HP} health.", LogLevel.Important);
                if (PCM.seat != null)
                    PCM.seat.part.RemoveCrewmember(PCM);
                PCM.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
                Vessel.CrewWasModified(PCM.GetVessel());
                Core.ShowMessage(Localizer.Format("#KH_Condition_KerbalDied", Name), true);//"<color=\"white\">" +  + "</color> has died of poor health!"
            }

            // If KSC training no longer possible, stop it
            if (IsTraining && !CanTrainAtKSC)
            {
                Core.ShowMessage(Localizer.Format("#KH_TrainingStopped", name), PCM);
                RemoveCondition(Condition_Training);
                if (PCM.rosterStatus != ProtoCrewMember.RosterStatus.Assigned)
                    TrainingFor.Clear();
            }

            // Stop training after the kerbal has been recovered
            if (TrainingFor.Count > 0 && PCM.rosterStatus != ProtoCrewMember.RosterStatus.Assigned && !IsTraining)
            {
                TrainingFor.Clear();
                TrainingVessel = null;
            }

            // Train
            if ((TrainingFor.Count > 0 && PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned)
                || (PCM.rosterStatus == ProtoCrewMember.RosterStatus.Available && IsTraining))
                Train(interval);

            if (HasCondition(Condition_Exhausted))
            {
                if (HP >= ExhaustionEndHP)
                {
                    RemoveCondition(Condition_Exhausted);
                    Core.ShowMessage(Localizer.Format("#KH_Condition_ExhastionEnd", Name), PCM);//"<color=\"white\">" +  + "</color> is no longer exhausted."
                }
            }
            else if (HP < ExhaustionStartHP)
            {
                AddCondition(Condition_Exhausted);
                Core.ShowMessage(Localizer.Format("#KH_Condition_ExhastionStart", Name), PCM);//"<color=\"white\">" +  + "</color> is exhausted!"
            }
        }

        #endregion HEALTH UPDATE

        #region SAVING, LOADING, INITIALIZING ETC.

        public ConfigNode ConfigNode
        {
            get
            {
                Core.Log($"Saving {Name}'s KerbalHealthStatus into a config node.");
                ConfigNode n = new ConfigNode("KerbalHealthStatus");
                n.AddValue("name", Name);
                n.AddValue("health", HP);
                foreach (KeyValuePair<HealthFactor, double> f in factorsOriginal.Where(kvp => kvp.Value != 0))
                {
                    ConfigNode factorNode = new ConfigNode("FACTOR");
                    factorNode.AddValue("name", f.Key.Name);
                    factorNode.AddValue("change", f.Value);
                    n.AddNode(factorNode);
                }
                if (locationEffect != null)
                    n.AddNode(locationEffect.ConfigNode);
                n.AddValue("dose", Dose);
                if (Radiation != 0)
                    n.AddValue("radiation", Radiation);
                foreach (HealthCondition hc in Conditions)
                    n.AddValue("condition", hc.Name);
                foreach (Quirk q in Quirks)
                    n.AddValue("quirk", q.Name);
                if (QuirkLevel != 0)
                    n.AddValue("quirkLevel", QuirkLevel);
                if (!IsCapable)
                    n.AddValue("trait", Trait);
                if (IsOnEVA)
                    n.AddValue("onEva", true);
                foreach (KeyValuePair<uint, double> t in TrainingLevels)
                {
                    ConfigNode n2 = new ConfigNode("TRAINED_PART");
                    n2.AddValue("id", t.Key);
                    n2.AddValue("trainingLevel", t.Value);
                    n.AddNode(n2);
                }
                foreach (string s in FamiliarPartTypes)
                    n.AddValue("familiarPartType", s);
                foreach (KeyValuePair<string, double> kvp in TrainedVessels)
                {
                    ConfigNode n2 = new ConfigNode("TRAINED_VESSEL");
                    n2.AddValue("name", kvp.Key);
                    n2.AddValue("trainingLevel", kvp.Value);
                    n.AddNode(n2);
                }
                foreach (TrainingPart tp in TrainingFor)
                    n.AddNode(tp.ConfigNode);
                if (TrainingVessel != null)
                    n.AddValue("trainingVessel", TrainingVessel);
                return n;
            }

            set
            {
                name = value.GetValue("name");
                hp = value.GetDouble("health", GetDefaultMaxHP(PCM));
                foreach (ConfigNode factorNode in value.GetNodes("FACTOR"))
                    factorsOriginal[Core.GetHealthFactor(factorNode.GetValue("name"))] = factorNode.GetDouble("change");
                if (value.HasNode(HealthEffect.ConfigNodeName))
                    locationEffect = new HealthEffect(value.GetNode(HealthEffect.ConfigNodeName));
                Dose = value.GetDouble("dose");
                Radiation = value.GetDouble("radiation");
                Conditions = new List<HealthCondition>(value.GetValues("condition").Select(s => Core.GetHealthCondition(s)));
                Conditions.AddRange(value.GetNodes("HealthCondition").Select(n => Core.GetHealthCondition(n.GetString("name"))));
                if (!KerbalHealthFactorsSettings.Instance.TrainingEnabled && IsTraining)
                    RemoveCondition(Condition_Training, true);
                foreach (string s in value.GetValues("quirk"))
                    AddQuirk(s);
                QuirkLevel = value.GetInt("quirkLevel");
                Trait = value.GetValue("trait");
                IsOnEVA = value.GetBool("onEva");
                TrainingLevels.Clear();
                foreach (ConfigNode n in value.GetNodes("TRAINED_PART"))
                {
                    uint id = n.GetUInt("id");
                    if (id != 0)
                        TrainingLevels[id] = n.GetDouble("trainingLevel");
                }
                FamiliarPartTypes.AddAll(value.GetValues("familiarPartType"));
                foreach (ConfigNode n in value.GetNodes("TRAINED_VESSEL"))
                {
                    string name = n.GetString("name");
                    if (name != null)
                        TrainedVessels.Add(name, n.GetDouble("trainingLevel"));
                }
                TrainingFor = new List<TrainingPart>(value.GetNodes("TRAINING_PART").Select(n => new TrainingPart(n)));
                TrainingVessel = value.GetString("trainingVessel");
                Core.Log($"{Name} loaded.");
            }
        }

        public KerbalHealthStatus(string name)
        {
            Name = name;
            HP = GetDefaultMaxHP(PCM);
            Core.Log($"Created record for {name} with {HP} HP.");
        }

        public KerbalHealthStatus(ConfigNode node) => ConfigNode = node;

        public override bool Equals(object obj) => ((KerbalHealthStatus)obj).Name.Equals(Name);

        public override int GetHashCode() => ConfigNode.GetHashCode();

        public KerbalHealthStatus Clone() => (KerbalHealthStatus)MemberwiseClone();

        #endregion SAVING, LOADING, INITIALIZING ETC.
    }
}
