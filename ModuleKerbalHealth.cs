using System;
using System.Collections.Generic;
using KSP.Localization;

namespace KerbalHealth
{
    public class ModuleKerbalHealth : PartModule, IResourceConsumer
    {
        [KSPField]
        public string title = "";  // Module title displayed in right-click menu (empty string for auto)

        [KSPField(isPersistant = true)]
        public uint id = 0;

        [KSPField]
        public float hpChangePerDay = 0;  // How many raw HP per day every affected kerbal gains

        [KSPField]
        public float recuperation = 0;  // Will increase HP by this % of (MaxHP - HP) per day

        [KSPField]
        public float decay = 0;  // Will decrease by this % of (HP - MinHP) per day

        [KSPField]
        public bool partCrewOnly = false;  // Does the module affect health of only crew in this part or the entire vessel?

        [KSPField]
        public string multiplyFactor = "All";  // Name of factor whose effect is multiplied

        [KSPField]
        public float multiplier = 1;  // How the factor is changed (e.g., 0.5 means factor's effect is halved)

        [KSPField]
        public int crewCap = 0;  // Max crew this module's multiplier applies to without penalty, 0 for unlimited (a.k.a. free multiplier)

        [KSPField]
        public double space = 0;  // Points of living space provided by the part (used to calculate Confinement factor)

        [KSPField]
        public float shielding = 0;  // Number of halving-thicknesses

        [KSPField]
        public float radioactivity = 0;  // Radioactive emission, bananas/day

        [KSPField]
        public string resource = "ElectricCharge";  // Determines, which resource is consumed by the module

        [KSPField]
        public float resourceConsumption = 0;  // Flat EC consumption (units per second)

        [KSPField]
        public float resourceConsumptionPerKerbal = 0;  // EC consumption per affected kerbal (units per second)

        [KSPField]
        public float complexity = 0;  // 0 if no training needed for this part, 1 for standard training complexity

        [KSPField(isPersistant = true)]
        public bool isActive = true;  // If not alwaysActive, this determines if the module is active

        [KSPField(isPersistant = true)]
        public bool starving = false;  // Determines if the module is disabled due to the lack of the resource

        [KSPField(guiName = "", guiActive = true, guiActiveEditor = true, guiUnits = "#KH_Module_ecPersec")] // /sec
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
                            if (pcm != null) r++;
                        Core.Log(r + " kerbal(s) found in " + part?.name + ".");
                        return r;
                    }
                    else return ShipConstruction.ShipManifest.CrewCount;
                else return partCrewOnly ? part.protoModuleCrew.Count : vessel.GetCrewCount();
            }
        }

        /// <summary>
        /// Returns # of kerbals affected by this module, capped by crewCap
        /// </summary>
        public int CappedAffectedCrewCount => crewCap > 0 ? Math.Min(TotalAffectedCrewCount, crewCap) : TotalAffectedCrewCount;

        public List<PartResourceDefinition> GetConsumedResources() => (resourceConsumption != 0 || resourceConsumptionPerKerbal != 0) ? new List<PartResourceDefinition>() { ResourceDefinition } : new List<PartResourceDefinition>();

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
            if ((complexity != 0) && (id == 0)) id = part.persistentId;
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
            if (Core.IsInEditor || !Core.ModEnabled) return;
            double time = Planetarium.GetUniversalTime();
            if (isActive && ((resourceConsumption != 0) || (resourceConsumptionPerKerbal != 0)))
            {
                ecPerSec = TotalResourceConsumption;
                double res = ecPerSec * (time - lastUpdated), res2;
                if (resource != "ElectricCharge") ecPerSec = 0;
                starving = (res2 = vessel.RequestResource(part, ResourceDefinition.id, res, false)) * 2 < res;
                if (starving) Core.Log(Title + " Module is starving of " + resource + " (" + res + " needed, " + res2 + " provided).");
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
            if (!Core.ModEnabled) return null;
            ModuleKerbalHealth mkh = proto_part_module as ModuleKerbalHealth;
            if (mkh.isActive && ((mkh.resourceConsumption != 0) || (mkh.resourceConsumptionPerKerbal != 0)))
            {
                mkh.ecPerSec = mkh.TotalResourceConsumption;
                double res = mkh.ecPerSec * elapsed_s, res2;
                if (mkh.resource != "ElectricCharge") mkh.ecPerSec = 0;
                availableResources.TryGetValue("ElectricCharge", out res2);
                if (res2 < mkh.ecPerSec) mkh.starving = true;
                resourceChangeRequest.Add(new KeyValuePair<string, double>(mkh.resource, -res));
                if (mkh.starving) Core.Log(mkh.Title + " Module is starving of " + mkh.resource + " (" + res + " needed, " + res2 + " available.");
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
            if (!Core.ModEnabled || !isActive) return null;
            resources.Add(new KeyValuePair<string, double>(resource, -ecPerSec));
            return Title.ToLower();
        }

        public string Title
        {
            get
            {
                if (!string.IsNullOrEmpty(title)) return title;
                if (recuperation > 0) return Localizer.Format("#KH_Module_type1");//"R&R"
                if (decay > 0) return Localizer.Format("#KH_Module_type2");//"Health Poisoning"
                switch (multiplyFactor.ToLower())
                {
                    case "stress": return Localizer.Format("#KH_Module_type3");  //"Stress Relief"
                    case "confinement": return Localizer.Format("#KH_Module_type4");//"Comforts"
                    case "loneliness": return Localizer.Format("#KH_Module_type5");//"Meditation"
                    case "microgravity": return (multiplier <= 0.25) ? Localizer.Format("#KH_Module_type6") : Localizer.Format("#KH_Module_type7");//"Paragravity""Exercise Equipment"
                    case "connected": return Localizer.Format("#KH_Module_type8");//"TV Set"
                    case "conditions": return Localizer.Format("#KH_Module_type9");//"Sick Bay"
                }
                if (space > 0) return Localizer.Format("#KH_Module_type10");//"Living Space"
                if (shielding > 0) return Localizer.Format("#KH_Module_type11");//"RadShield"
                if (radioactivity > 0) return Localizer.Format("#KH_Module_type12");//"Radiation"
                return Localizer.Format("#KH_Module_title");//"Health Module"
            }
            set => title = value;
        }

        void UpdateGUIName()
        {
            Events["OnToggleActive"].guiName = Localizer.Format(isActive ? "#KH_Module_Disable" : "#KH_Module_Enable", Title);//"Disable ""Enable "
            Fields["ecPerSec"].guiActive = Fields["ecPerSec"].guiActiveEditor = Core.ModEnabled && isActive && ecPerSec != 0;
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
            if (hpChangePerDay != 0) res = Localizer.Format("#KH_Module_info1", hpChangePerDay.ToString("F1"));//"\nHealth points: " +  + "/day"
            if (recuperation != 0) res += Localizer.Format("#KH_Module_info2", recuperation.ToString("F1"));//"\nRecuperation: " +  + "%/day"
            if (decay != 0) res += Localizer.Format("#KH_Module_info3", decay.ToString("F1"));//"\nHealth decay: " +  + "%/day"
            if (multiplier != 1) res += Localizer.Format("#KH_Module_info4", multiplier.ToString("F2"),multiplyFactor);//"\n" +  + "x " + 
            if (crewCap > 0) res += Localizer.Format("#KH_Module_info5", crewCap,(crewCap != 1 ? Localizer.Format("#KH_Module_info5_s") : ""));//" for up to " +  + " kerbal" + "s"
            if (space != 0) res += Localizer.Format("#KH_Module_info6",space.ToString("F1"));//"\nSpace: " + 
            if (resourceConsumption != 0) res += Localizer.Format("#KH_Module_info7", ResourceDefinition.abbreviation,resourceConsumption.ToString("F2"));//"\n" +  + ": " +  + "/sec."
            if (resourceConsumptionPerKerbal != 0) res += Localizer.Format("#KH_Module_info8", ResourceDefinition.abbreviation,resourceConsumptionPerKerbal.ToString("F2"));//"\n" +  + " per Kerbal: " +  + "/sec."
            if (shielding != 0) res += Localizer.Format("#KH_Module_info9", shielding.ToString("F1"));//"\nShielding rating: " + 
            if (radioactivity != 0) res += Localizer.Format("#KH_Module_info10", radioactivity.ToString("N0"));//"\nRadioactive emission: " +  + "/day"
            if (complexity != 0) res += Localizer.Format("#KH_Module_info11", (complexity * 100).ToString("N0"));// "\nTraining complexity: " + (complexity * 100).ToString("N0") + "%"
            if (string.IsNullOrEmpty(res)) return "";
            return  Localizer.Format("#KH_Module_typetitle", Title)+ res;//"Module type: " + 
        }
    }
}
