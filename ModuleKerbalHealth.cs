﻿using ConnectedLivingSpace;
using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

using static KerbalHealth.Core;

namespace KerbalHealth
{
    public class ModuleKerbalHealth : PartModule, IResourceConsumer
    {
        [KSPField]
        // Module title displayed in right-click menu (empty string for auto)
        public string title = "";

        [KSPField]
        // How many raw HP per day every affected kerbal gains
        public float hpChangePerDay = 0;

        [KSPField]
        // Will increase HP by this % of (MaxHP - HP) per day
        public float recuperation = 0;

        [KSPField]
        // Will decrease by this % of (HP - MinHP) per day
        public float decay = 0;

        [KSPField]
        // Name of factor whose effect is multiplied
        public string multiplyFactor = "All";

        [KSPField]
        // How the factor is changed (e.g., 0.5 means factor's effect is halved)
        public float multiplier = 1;

        [KSPField]
        // Max crew this module's multiplier applies to without penalty, 0 for unlimited (a.k.a. free multiplier), -1 for part's crew capacity
        public int crewCap = -1;

        [KSPField]
        // Points of living space provided by the part (used to calculate Confinement factor)
        public float space = 0;

        [KSPField]
        // Number of halving-thicknesses
        public float shielding = 0;

        [KSPField]
        // Radioactive emission, bananas/day
        public float radioactivity = 0;

        [KSPField]
        // Radioactivity is produced when this part's ModuleEngines are active and is multiplied by throttle, bananas/day
        public float engineRadioactivity = 0;

        [KSPField]
        // Determines, which resource is consumed by the module
        public string resource = "ElectricCharge";

        [KSPField]
        // Flat EC (or other resource) consumption in units per second
        public float resourceConsumption = 0;

        [KSPField]
        // EC (or other resource) consumption per affected kerbal (units per second)
        public float resourceConsumptionPerKerbal = 0;

        [KSPField]
        // Does the module affect all ConnectedLivingSpace spaces? Otherwise only Shielding and Radiation apply
        public bool affectsAllCLSSpaces = false;

        [KSPField]
        // 0 if no training needed for this part, 1 for standard training complexity
        public float complexity = 0;

        [KSPField(isPersistant = true)]
        // If not alwaysActive, this determines whether the module is active
        public bool isActive = true;

        [KSPField(isPersistant = true)]
        // Determines whether the module is disabled due to the lack of the resource
        public bool starving = false;

        [KSPField(guiName = "", guiActive = true, guiActiveEditor = true, guiUnits = "#KH_Module_ecPersec")] // /sec
        // Electric Charge usage per second
        public float ecPerSec = 0;

        [KSPField(isPersistant = true)]
        // For modules that have two modes (Living Space or Confinement multiplier), determines if the alternative mode is enabled
        public bool multiplierMode = false;

        [KSPField(isPersistant = true, guiName = "#KH_Module_Config", guiActive = true, guiActiveEditor = true)]
        public string configName = "";

        double lastUpdated;

        List<IEngineStatus> engineModules;

        public float Multiplier => multiplierMode || !IsSwitchable ? multiplier : 1;

        public float Space => multiplierMode ? 0 : space;

        /// <summary>
        /// Returns true if this module has two modes (Living Space and Confinement multipler) that can be switched in the editor
        /// </summary>
        public bool IsSwitchable => space != 0 && multiplyFactor.Equals(ConfinementFactor.Id, StringComparison.OrdinalIgnoreCase);

        public bool IsAlwaysActive => resourceConsumption == 0 && resourceConsumptionPerKerbal == 0;

        public bool IsModuleActive => IsAlwaysActive || (isActive && (!IsInEditor || KerbalHealthEditorReport.HealthModulesEnabled) && !starving);

        /// <summary>
        /// Returns total # of kerbals affected by this module
        /// </summary>
        public int TotalAffectedCrewCount
        {
            get
            {
                if (!affectsAllCLSSpaces && CLS.Enabled)
                {
                    ICLSVessel clsVessel = IsInEditor ? CLS.CLSAddon.Vessel : CLS.CLSAddon.getCLSVessel(vessel);
                    if (clsVessel != null)
                    {
                        ICLSSpace clsSpace = clsVessel.Parts.Find(p => p.Part == part)?.Space;
                        if (clsSpace != null)
                            return clsSpace.GetCrewCount();
                        else Log($"Could not find CLS space for part {part?.name}.", LogLevel.Error);
                    }
                    else Log($"Could not find CLS vessel for part {part?.name} in vessel {vessel?.name ?? "N/A"}.", LogLevel.Error);
                }

                if (IsInEditor)
                    return ShipConstruction.ShipManifest.CrewCount;

                return vessel != null ? vessel.GetCrewCount() : 0;
            }
        }

        /// <summary>
        /// Returns # of kerbals affected by this module, capped by crewCap
        /// </summary>
        public int CappedAffectedCrewCount
        {
            get
            {
                int crewCount = TotalAffectedCrewCount;
                return crewCap > 0 ? Math.Min(crewCount, crewCap) : crewCount;
            }
        }

        public float TotalResourceConsumption => resourceConsumption + resourceConsumptionPerKerbal * CappedAffectedCrewCount;

        public double RecuperationPower => crewCap > 0 ? recuperation * Math.Min((double)crewCap / TotalAffectedCrewCount, 1) : recuperation;

        public double DecayPower => crewCap > 0 ? decay * Math.Min((double)crewCap / TotalAffectedCrewCount, 1) : decay;

        /// <summary>
        /// Total current radioactive emission of this module
        /// </summary>
        public float Radioactivity => IsModuleActive ? radioactivity + (engineModules != null ? engineRadioactivity * engineModules.Sum(me => me.throttleSetting) : 0) : 0;

        public string Title
        {
            get => GetTitle(true);
            set => title = value;
        }

        public string PartName => part?.partInfo?.name;
        
        public string PartTitle => part?.partInfo?.title;
        
        PartResourceDefinition ResourceDefinition
        {
            get => PartResourceLibrary.Instance.GetDefinition(resource);
            set => resource = value?.name;
        }

        #region KERBALISM

        /// <summary>
        /// Kerbalism background processing compatibility method
        /// </summary>
        /// <param name="v"></param>
        /// <param name="part_snapshot"></param>
        /// <param name="module_snapshot"></param>
        /// <param name="proto_part_module"></param>
        /// <param name="proto_part"></param>
        /// <param name="availableResources"></param>
        /// <param name="resourceChangeRequest"></param>
        /// <param name="elapsed_s"></param>
        /// <returns></returns>
        public static string BackgroundUpdate(Vessel v, ProtoPartSnapshot part_snapshot, ProtoPartModuleSnapshot module_snapshot, PartModule proto_part_module, Part proto_part, Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest, double elapsed_s)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled)
                return null;
            ModuleKerbalHealth mkh = proto_part_module as ModuleKerbalHealth;
            if (mkh.isActive && (mkh.resourceConsumption > 0 || mkh.resourceConsumptionPerKerbal > 0))
            {
                mkh.part = proto_part;
                mkh.part.vessel = v;
                mkh.ecPerSec = mkh.TotalResourceConsumption;
                double requiredAmount = mkh.ecPerSec * elapsed_s;
                availableResources.TryGetValue(mkh.resource, out double availableAmount);
                if (requiredAmount > 0 && availableAmount <= 0)
                {
                    Log($"{mkh.Title} Module in {proto_part?.name} is starving of {mkh.resource} ({requiredAmount} @ {mkh.ecPerSec} EC/sec needed, {availableAmount} available).");
                    mkh.starving = true;
                }
                resourceChangeRequest.Add(new KeyValuePair<string, double>(mkh.resource, -mkh.ecPerSec));
                if (mkh.resource != "ElectricCharge")
                    mkh.ecPerSec = 0;
            }
            else mkh.ecPerSec = 0;
            return mkh.Title.ToLower();
        }

        /// <summary>
        /// Kerbalism Planner compatibility method
        /// </summary>
        /// <param name="resources">A list of resource names and production/consumption rates. Production is a positive rate, consumption is negatvie. Add all resources your module is going to produce/consume.</param>
        /// <param name="body">The currently selected body in the Kerbalism planner</param>
        /// <param name="environment">Environment variables guesstimated by Kerbalism, based on the current selection of body and vessel situation. See above.</param>
        /// <returns>The title to display in the tooltip of the planner UI.</returns>
        public string PlannerUpdate(List<KeyValuePair<string, double>> resources, CelestialBody body, Dictionary<string, double> environment)
        {
            if (!KerbalHealthGeneralSettings.Instance.modEnabled || !isActive || IsAlwaysActive)
                return null;
            resources.Add(new KeyValuePair<string, double>(resource, -ecPerSec));
            return Title.ToLower();
        }

        #endregion KERBALISM

        public List<PartResourceDefinition> GetConsumedResources() =>
            resourceConsumption != 0 || resourceConsumptionPerKerbal != 0 ? new List<PartResourceDefinition>() { ResourceDefinition } : new List<PartResourceDefinition>();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Log($"ModuleKerbalHealth.OnStart({state}) for {PartName}");
            if (crewCap < 0)
                crewCap = part.CrewCapacity;
            if (IsAlwaysActive)
            {
                isActive = true;
                Events["OnToggleActive"].guiActive = false;
                Events["OnToggleActive"].guiActiveEditor = false;
            }
            if (IsInEditor && resource == "ElectricCharge")
                ecPerSec = TotalResourceConsumption;
            Fields["ecPerSec"].guiName = Localizer.Format("#KH_Module_ECUsage", Title); // + EC Usage:
            if (!IsSwitchable)
            {
                Fields["configName"].guiActive = Fields["configName"].guiActiveEditor = false;
                Events["OnSwitchConfig"].guiActiveEditor = false;
            }
            if (engineRadioactivity != 0)
            {
                engineModules = part.FindModulesImplementing<IEngineStatus>();
                if (engineModules != null)
                    Log($"{PartName} has {engineModules.Count} engine module(s).");
                else Log($"Could not find an engine module for {PartName} although it has engineRadioactivity of {engineRadioactivity}.", LogLevel.Error);
            }
            UpdateGUIName();
            lastUpdated = Planetarium.GetUniversalTime();
        }

        public void FixedUpdate()
        {
            if (IsInEditor || !KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            double time = Planetarium.GetUniversalTime();
            if (isActive && (resourceConsumption > 0 || resourceConsumptionPerKerbal > 0))
            {
                ecPerSec = TotalResourceConsumption;
                double requiredAmount = ecPerSec * (time - lastUpdated), providedAmount;
                if (resource != "ElectricCharge")
                    ecPerSec = 0;
                starving = (providedAmount = vessel.RequestResource(part, ResourceDefinition.id, requiredAmount, false)) * 2 < requiredAmount;
                if (starving)
                    Log($"{Title} Module in {PartName} is starving of {resource} ({requiredAmount} needed, {providedAmount} provided).");
            }
            else ecPerSec = 0;
            lastUpdated = time;
        }

        public string GetTitle(bool configSelected)
        {
            if (!string.IsNullOrEmpty(title))
                return title;
            if (recuperation > 0)
                return Localizer.Format("#KH_Module_type1");//"R&R"
            if (decay > 0)
                return Localizer.Format("#KH_Module_type2");//"Health Poisoning"
            switch (multiplyFactor.ToLower())
            {
                case "stress":
                    return Localizer.Format("#KH_Module_type3");  //"Stress Relief"
                case "confinement":
                    if (multiplierMode || !IsSwitchable)
                        return Localizer.Format("#KH_Module_type4");//"Lounge"
                    break;
                case "loneliness":
                    return Localizer.Format("#KH_Module_type5");//"Meditation"
                case "microgravity":
                    return Localizer.Format("#KH_Module_type6");//"Paragravity"
                case "connected":
                    return Localizer.Format("#KH_Module_type8");//"Broadband Internet"
                case "conditions":
                    return Localizer.Format("#KH_Module_type9");//"Sick Bay"
            }
            if (space > 0 && configSelected && !multiplierMode)
                return Localizer.Format("#KH_Module_type10");//"Living Quarters"
            if (shielding > 0)
                return Localizer.Format("#KH_Module_type11");//"RadShield"
            if (radioactivity > 0)
                return Localizer.Format("#KH_Module_type12");//"Radiation"
            if (engineRadioactivity > 0)
                return Localizer.Format("#KH_Module_type13");
            if (IsSwitchable && !configSelected)
                return Localizer.Format("#KH_Module_Type_Switchable");
            return Localizer.Format("#KH_Module_title");//"Health Module"
        }

        [KSPEvent(name = "OnToggleActive", guiActive = true, guiName = "#KH_Module_Toggle", guiActiveEditor = true)] //Toggle Health Module
        public void OnToggleActive()
        {
            isActive = IsAlwaysActive || !isActive;
            UpdateGUIName();
            if (IsInEditor)
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        [KSPEvent(name = "OnSwitchConfig", guiActiveEditor = true, guiName = "#KH_Module_SwitchConfig")]
        public void OnSwitchConfig()
        {
            Log("ModuleKerbalHealth.OnSwitchConfig");
            if (IsSwitchable && IsInEditor)
                multiplierMode = !multiplierMode;
            UpdateGUIName();
            if (IsInEditor)
                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
        }

        public override string GetModuleDisplayName() => Localizer.Format("#KH_Module_title");

        public override string GetInfo()
        {
            string res = "";
            if (hpChangePerDay != 0)
                res = Localizer.Format("#KH_Module_info1", hpChangePerDay.ToString("F1"));//"\nHealth points: " +  + "/day"
            if (recuperation != 0)
                res += Localizer.Format("#KH_Module_info2", recuperation.ToString("F1"));//"\nRecuperation: " +  + "%/day"
            if (decay != 0)
                res += Localizer.Format("#KH_Module_info3", decay.ToString("F1"));//"\nHealth decay: " +  + "%/day"
            if (IsSwitchable)
                res += Localizer.Format("#KH_Module_Info_Configs", space.ToString("F1"), multiplyFactor, multiplier * 100);
            else
            {
                if (space != 0)
                    res += Localizer.Format("#KH_Module_info6", space.ToString("F1"));//"\nSpace: " +
                if (multiplier != 1)
                    res += Localizer.Format("#KH_Module_info4", multiplyFactor, multiplier * 100);//"\n" +  + "x " +
            }
            if (crewCap > 0)
                res += Localizer.Format("#KH_Module_info5", crewCap);//" for up to " +  + " kerbals
            if (resourceConsumption != 0)
                res += Localizer.Format("#KH_Module_info7", ResourceDefinition.abbreviation, resourceConsumption.ToString("F2"));//"\n" +  + ": " +  + "/sec."
            if (resourceConsumptionPerKerbal != 0)
                res += Localizer.Format("#KH_Module_info8", ResourceDefinition.abbreviation, resourceConsumptionPerKerbal.ToString("F2"));//"\n" +  + " per Kerbal: " +  + "/sec."
            if (shielding != 0)
                res += Localizer.Format("#KH_Module_info9", shielding.ToString("F1"));//"\nShielding rating: " +
            if (radioactivity != 0)
                res += Localizer.Format("#KH_Module_info10", radioactivity.ToString("N0"));//"\nRadioactive emission: " +  + "/day"
            if (engineRadioactivity != 0)
                res += Localizer.Format("#KH_Module_engineRadioactivity", (engineRadioactivity / 1e6).ToString("N1"));
            if (complexity != 0)
                res += Localizer.Format("#KH_Module_info11", (complexity * 100).ToString("N0"));// "\nTraining complexity: " + (complexity * 100).ToString("N0") + "%"
            if (string.IsNullOrEmpty(res))
                return "";
            if (IsSwitchable)
                res += $"\n\n<color=yellow>{Localizer.Format("#KH_Module_Info_Switchable")}</color>";
            return Localizer.Format("#KH_Module_typetitle", GetTitle(false)) + res;//"Module type: " +
        }

        void UpdateGUIName()
        {
            Log($"UpdateGUIName for {Title}. Space = {space}, multiply factor = {multiplyFactor}, multiplierMode is {multiplierMode}. Effective space is {Space}, multiplier {Multiplier}.");
            if (IsSwitchable)
                Fields.SetValue("configName", Title);
            Events["OnToggleActive"].guiName = Localizer.Format(isActive ? "#KH_Module_Disable" : "#KH_Module_Enable", Title);//"Disable ""Enable "
            Fields["ecPerSec"].guiActive = Fields["ecPerSec"].guiActiveEditor = KerbalHealthGeneralSettings.Instance.modEnabled && isActive && ecPerSec != 0;
        }
    }
}
