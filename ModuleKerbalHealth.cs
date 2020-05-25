using System;
using System.Collections.Generic;
using KSP.Localization;

namespace KerbalHealth
{
    public class ModuleKerbalHealth : PartModule, IResourceConsumer
    {
        [KSPField]
        // Module title displayed in right-click menu (empty string for auto)
        public string title = "";

        [KSPField(isPersistant = true)]
        public uint id = 0;

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
        // Does the module affect health of only crew in this part or the entire vessel?
        public bool partCrewOnly = false;

        [KSPField]
        // Name of factor whose effect is multiplied
        public string multiplyFactor = "All";

        [KSPField]
        // How the factor is changed (e.g., 0.5 means factor's effect is halved)
        public float multiplier = 1;

        [KSPField]
        // Max crew this module's multiplier applies to without penalty, 0 for unlimited (a.k.a. free multiplier)
        public int crewCap = 0;

        [KSPField]
        // Points of living space provided by the part (used to calculate Confinement factor)
        public double space = 0;

        [KSPField]
        // Number of halving-thicknesses
        public float shielding = 0;

        [KSPField]
        // Radioactive emission, bananas/day
        public float radioactivity = 0;

        [KSPField]
        // Determines, which resource is consumed by the module
        public string resource = "ElectricCharge";

        [KSPField]
        // Flat EC consumption (units per second)
        public float resourceConsumption = 0;

        [KSPField]
        // EC consumption per affected kerbal (units per second)
        public float resourceConsumptionPerKerbal = 0;

        [KSPField]
        // 0 if no training needed for this part, 1 for standard training complexity
        public float complexity = 0;

        [KSPField(isPersistant = true)]
        // If not alwaysActive, this determines if the module is active
        public bool isActive = true;

        [KSPField(isPersistant = true)]
        // Determines if the module is disabled due to the lack of the resource
        public bool starving = false;

        [KSPField(guiName = "", guiActive = true, guiActiveEditor = true, guiUnits = "#KH_Module_ecPersec")] // /sec
        // Electric Charge usage per second
        public float ecPerSec = 0;

        double lastUpdated;

        public HealthFactor MultiplyFactor
        {
            get => Core.GetHealthFactor(multiplyFactor);
            set => multiplyFactor = value.Name;
        }

        public bool IsAlwaysActive => (resourceConsumption == 0) && (resourceConsumptionPerKerbal == 0);

        public bool IsModuleActive => IsAlwaysActive || (isActive && (!Core.IsInEditor || KerbalHealthEditorReport.HealthModulesEnabled) && !starving);

        /// <summary>
        /// Returns total # of kerbals affected by this module
        /// </summary>
        public int TotalAffectedCrewCount
        {
            get
            {
                if (Core.IsInEditor)
                    if (partCrewOnly)
                    {
                        int r = 0;
                        foreach (ProtoCrewMember pcm in ShipConstruction.ShipManifest.GetPartCrewManifest(part.craftID).GetPartCrew())
                            if (pcm != null)
                                r++;
                        Core.Log(r + " kerbal(s) found in " + part?.name + ".");
                        return r;
                    }
                    else return ShipConstruction.ShipManifest.CrewCount;
                if (part == null)
                {
                    Core.Log("TotalAffectedCrewCount: part is null!", LogLevel.Error);
                    return 0;
                }
                if (part.protoModuleCrew == null)
                {
                    Core.Log("TotalAffectedCrewCount: part.protoModuleCrew is null!", LogLevel.Error);
                    return 0;
                }
                if (vessel == null)
                {
                    Core.Log("TotalAffectedCrewCount: vessel is null!", LogLevel.Error);
                    return 0;
                }
                return partCrewOnly ? part.protoModuleCrew.Count : vessel.GetCrewCount();
            }
        }

        /// <summary>
        /// Returns # of kerbals affected by this module, capped by crewCap
        /// </summary>
        public int CappedAffectedCrewCount => crewCap > 0 ? Math.Min(TotalAffectedCrewCount, crewCap) : TotalAffectedCrewCount;

        public List<PartResourceDefinition> GetConsumedResources()
            => (resourceConsumption != 0 || resourceConsumptionPerKerbal != 0)
            ? new List<PartResourceDefinition>() { ResourceDefinition }
            : new List<PartResourceDefinition>();

        PartResourceDefinition ResourceDefinition
        {
            get => PartResourceLibrary.Instance.GetDefinition(resource);
            set => resource = value?.name;
        }

        public float TotalResourceConsumption => resourceConsumption + resourceConsumptionPerKerbal * CappedAffectedCrewCount;

        public double RecuperationPower => crewCap > 0 ? recuperation * Math.Min((double)crewCap / TotalAffectedCrewCount, 1) : recuperation;

        public double DecayPower => crewCap > 0 ? decay * Math.Min((double)crewCap / TotalAffectedCrewCount, 1) : decay;

        public override void OnStart(StartState state)
        {
            Core.Log("ModuleKerbalHealth.OnStart(" + state + ") for " + part.name);
            base.OnStart(state);
            if ((complexity != 0) && (id == 0))
                id = part.persistentId;
            if (IsAlwaysActive)
            {
                isActive = true;
                Events["OnToggleActive"].guiActive = false;
                Events["OnToggleActive"].guiActiveEditor = false;
            }
            if (Core.IsInEditor && (resource == "ElectricCharge"))
                ecPerSec = TotalResourceConsumption;
            Fields["ecPerSec"].guiName = Localizer.Format("#KH_Module_ECUsage", Title); // + EC Usage:
            UpdateGUIName();
            lastUpdated = Planetarium.GetUniversalTime();
        }

        public void FixedUpdate()
        {
            if (Core.IsInEditor || !KerbalHealthGeneralSettings.Instance.modEnabled)
                return;
            double time = Planetarium.GetUniversalTime();
            if (isActive && ((resourceConsumption != 0) || (resourceConsumptionPerKerbal != 0)))
            {
                ecPerSec = TotalResourceConsumption;
                double requiredAmount = ecPerSec * (time - lastUpdated), providedAmount;
                if (resource != "ElectricCharge")
                    ecPerSec = 0;
                starving = (providedAmount = vessel.RequestResource(part, ResourceDefinition.id, requiredAmount, false)) * 2 < requiredAmount;
                if (starving)
                    Core.Log(Title + " Module is starving of " + resource + " (" + requiredAmount + " needed, " + providedAmount + " provided).");
            }
            else ecPerSec = 0;
            lastUpdated = time;
        }

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
            if (mkh.isActive && ((mkh.resourceConsumption != 0) || (mkh.resourceConsumptionPerKerbal != 0)))
            {
                mkh.part = proto_part;
                mkh.part.vessel = v;
                mkh.ecPerSec = mkh.TotalResourceConsumption;
                double requiredAmount = mkh.ecPerSec * elapsed_s;
                if (mkh.resource != "ElectricCharge")
                    mkh.ecPerSec = 0;
                availableResources.TryGetValue(mkh.resource, out double res2);
                if (res2 < requiredAmount)
                {
                    Core.Log(mkh.Title + " Module is starving of " + mkh.resource + " (" + requiredAmount + " @ " + mkh.ecPerSec + "EC/sec needed, " + res2 + " available.");
                    mkh.starving = true;
                }
                else resourceChangeRequest.Add(new KeyValuePair<string, double>(mkh.resource, -requiredAmount));
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

        public string Title
        {
            get
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
                        return Localizer.Format("#KH_Module_type4");//"Comforts"
                    case "loneliness":
                        return Localizer.Format("#KH_Module_type5");//"Meditation"
                    case "microgravity":
                        return (multiplier <= 0.25) ? Localizer.Format("#KH_Module_type6") : Localizer.Format("#KH_Module_type7");//"Paragravity""Exercise Equipment"
                    case "connected":
                        return Localizer.Format("#KH_Module_type8");//"TV Set"
                    case "conditions":
                        return Localizer.Format("#KH_Module_type9");//"Sick Bay"
                }
                if (space > 0)
                    return Localizer.Format("#KH_Module_type10");//"Living Space"
                if (shielding > 0)
                    return Localizer.Format("#KH_Module_type11");//"RadShield"
                if (radioactivity > 0)
                    return Localizer.Format("#KH_Module_type12");//"Radiation"
                return Localizer.Format("#KH_Module_title");//"Health Module"
            }
            set => title = value;
        }

        void UpdateGUIName()
        {
            Events["OnToggleActive"].guiName = Localizer.Format(isActive ? "#KH_Module_Disable" : "#KH_Module_Enable", Title);//"Disable ""Enable "
            Fields["ecPerSec"].guiActive = Fields["ecPerSec"].guiActiveEditor = KerbalHealthGeneralSettings.Instance.modEnabled && isActive && ecPerSec != 0;
        }
        
        [KSPEvent(name = "OnToggleActive", guiActive = true, guiName = "#KH_Module_Toggle", guiActiveEditor = true)] //Toggle Health Module
        public void OnToggleActive()
        {
            isActive = IsAlwaysActive || !isActive;
            UpdateGUIName();
        }

        public override string GetInfo()
        {
            string res = "";
            if (hpChangePerDay != 0)
                res = Localizer.Format("#KH_Module_info1", hpChangePerDay.ToString("F1"));//"\nHealth points: " +  + "/day"
            if (recuperation != 0)
                res += Localizer.Format("#KH_Module_info2", recuperation.ToString("F1"));//"\nRecuperation: " +  + "%/day"
            if (decay != 0)
                res += Localizer.Format("#KH_Module_info3", decay.ToString("F1"));//"\nHealth decay: " +  + "%/day"
            if (multiplier != 1)
                res += Localizer.Format("#KH_Module_info4", multiplier.ToString("F2"),multiplyFactor);//"\n" +  + "x " + 
            if (crewCap > 0)
                res += Localizer.Format("#KH_Module_info5", crewCap,(crewCap != 1 ? Localizer.Format("#KH_Module_info5_s") : ""));//" for up to " +  + " kerbal" + "s"
            if (space != 0)
                res += Localizer.Format("#KH_Module_info6", space.ToString("F1"));//"\nSpace: " + 
            if (resourceConsumption != 0)
                res += Localizer.Format("#KH_Module_info7", ResourceDefinition.abbreviation,resourceConsumption.ToString("F2"));//"\n" +  + ": " +  + "/sec."
            if (resourceConsumptionPerKerbal != 0)
                res += Localizer.Format("#KH_Module_info8", ResourceDefinition.abbreviation,resourceConsumptionPerKerbal.ToString("F2"));//"\n" +  + " per Kerbal: " +  + "/sec."
            if (shielding != 0)
                res += Localizer.Format("#KH_Module_info9", shielding.ToString("F1"));//"\nShielding rating: " + 
            if (radioactivity != 0)
                res += Localizer.Format("#KH_Module_info10", radioactivity.ToString("N0"));//"\nRadioactive emission: " +  + "/day"
            if (complexity != 0)
                res += Localizer.Format("#KH_Module_info11", (complexity * 100).ToString("N0"));// "\nTraining complexity: " + (complexity * 100).ToString("N0") + "%"
            if (string.IsNullOrEmpty(res))
                return "";
            return  Localizer.Format("#KH_Module_typetitle", Title) + res;//"Module type: " + 
        }
    }
}
