using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalHealth
{
    public class ModuleKerbalHealth : PartModule, IResourceConsumer
    {
        [KSPField]
        string title = "";  // Module title displayed in right-click menu (empty string for auto)

        [KSPField]
        public float hpChangePerDay = 0;  // How many raw HP per day every affected kerbal gains

        [KSPField]
        public float recuperation = 0;  // Will increase HP by this % of (MaxHP - HP) per day

        [KSPField]
        public float decay = 0;  // Will decrease by this % of (HP - MinHP) per day

        [KSPField]
        public bool partCrewOnly = false;  // Does the module affect health of only crew in this part or the entire vessel?

        [KSPField]
        public string resource = "ElectricCharge";  // Determines, which resource is consumed by the module

        [KSPField]
        public float resourceConsumption = 0;  // Flat EC consumption (units per second)

        [KSPField]
        public float resourceConsumptionPerKerbal = 0;  // EC consumption per affected kerbal (units per second)

        [KSPField]
        public bool alwaysActive = false;  // Is the module's effect (and consumption) always active or togglable in-flight

        [KSPField(isPersistant = true)]
        public bool isActive = true;  // If not alwaysActive, this determines if the module is active

        [KSPField]
        public string multiplyFactor = "All";  // Name of factor whose effect is multiplied

        [KSPField]
        public float multiplier = 1;  // How the factor is changed (e.g., 0.5 means factor's effect is halved)

        [KSPField]
        public int crewCap = 0;  // Max crew this module's multiplier applies to without penalty, 0 for unlimited (a.k.a. free multiplier)

        [KSPField]
        public float shielding = 0;  // Number of halving-thicknesses

        [KSPField]
        public float radioactivity = 0;  // Radioactive emission, bananas/day

        double lastUpdated;

        public HealthFactor MultiplyFactor
        {
            get => Core.FindFactor(multiplyFactor);
            set => multiplyFactor = value.Name;
        }

        public bool IsModuleActive => alwaysActive || isActive;

        /// <summary>
        /// Returns # of kerbals affected by this module, capped by crewCap
        /// </summary>
        public int AffectedCrewCount
        {
            get
            {
                int r = 0;
                if (Core.IsInEditor)
                    if (partCrewOnly)
                    {
                        foreach (ProtoCrewMember pcm in ShipConstruction.ShipManifest.GetPartCrewManifest(part.craftID).GetPartCrew())
                            if (pcm != null) r++;
                        Core.Log(r + " kerbals found in " + part?.name + ".");
                    }
                    else r = ShipConstruction.ShipManifest.CrewCount;
                else if (partCrewOnly) r = part.protoModuleCrew.Count;
                else r = vessel.GetCrewCount();
                if (crewCap > 0) return Math.Min(r, crewCap);
                else return r;
            }
        }

        public List<PartResourceDefinition> GetConsumedResources()
        {
            if (resourceConsumption != 0) return new List<PartResourceDefinition>() { ResourceDefinition };
            else return new List<PartResourceDefinition>();
        }

        PartResourceDefinition ResourceDefinition
        {
            get => PartResourceLibrary.Instance.GetDefinition(resource);
            set => resource = value?.name;
        }

        public override void OnStart(StartState state)
        {
            Core.Log("ModuleKerbalHealth.OnStart(" + state + ")");
            base.OnStart(state);
            if (alwaysActive)
            {
                isActive = true;
                Events["OnToggleActive"].guiActive = false;
            }
            UpdateGUIName();
            lastUpdated = Planetarium.GetUniversalTime();
        }

        public void FixedUpdate()
        {
            if (Core.IsInEditor || !Core.ModEnabled) return;
            double time = Planetarium.GetUniversalTime();
            if (IsModuleActive && ((resourceConsumption != 0) || (resourceConsumptionPerKerbal != 0)))
            {
                Core.Log(AffectedCrewCount + " crew affected by this part + " + part.name + ".");
                double res = (resourceConsumption + resourceConsumptionPerKerbal * AffectedCrewCount) * (time - lastUpdated), res2;
                if ((res2 = vessel.RequestResource(part, ResourceDefinition.id, res, false)) * 2 < res)
                {
                    Core.Log(Title + " Module shut down due to lack of " + resource + " (" + res + " needed, " + res2 + " provided).");
                    ScreenMessages.PostScreenMessage(Title + " Module in " + part.name + " shut down due to lack of " + ResourceDefinition.name + ".");
                    isActive = false;
                }
            }
            lastUpdated = time;
        }

        public string Title
        {
            get
            {
                if (title != "") return title;
                if (recuperation > 0) return "R&R";
                if (decay > 0) return "Health Poisoning";
                switch (multiplyFactor)
                {
                    case "Crowded": return "Comforts";
                    case "Microgravity": return "Paragravity";
                    case "Sickness": return "Sick Bay";
                }
                if (shielding > 0) return "RadShield";
                if (radioactivity > 0) return "Radiation";
                return "Health Module";
            }
            set => title = value;
        }

        void UpdateGUIName() => Events["OnToggleActive"].guiName = (isActive ? "Disable " : "Enable ") + Title;
        
        [KSPEvent(name = "OnToggleActive", guiActive = true, guiName = "Toggle Health Module", guiActiveEditor = false)]
        public void OnToggleActive()
        {
            isActive = alwaysActive || !isActive;
            UpdateGUIName();
        }

        public override string GetInfo()
        {
            string res = "";
            res += Title;
            if (hpChangePerDay != 0) res += "\nHP/day: " + hpChangePerDay.ToString("F1");
            if (recuperation != 0) res += "\nRecuperation: " + recuperation.ToString("F1") + "%/day";
            if (decay != 0) res += "\nHealth decay: " + decay.ToString("F1") + "%/day";
            if (multiplier != 1) 
                res += "\n" + multiplier.ToString("F2") + "x " + multiplyFactor;
            if (crewCap > 0) res += " for up to " + crewCap + " kerbal" + (crewCap != 1 ? "s" : "");
            if (resourceConsumption != 0) res += "\n" + ResourceDefinition.abbreviation + ": " + resourceConsumption.ToString("F1") + "/sec.";
            if (resourceConsumptionPerKerbal != 0) res += "\n" + ResourceDefinition.abbreviation + " per Kerbal: " + resourceConsumptionPerKerbal.ToString("F1") + "/sec.";
            if (shielding != 0) res += "\nShielding rating: " + shielding.ToString("F1");
            if (radioactivity != 0) res += "\nRadioactive emission: " + radioactivity.ToString("N0") + "/day";
            return res.Trim('\n');
        }
    }
}
