using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalHealth
{
    public class ModuleKerbalHealth : PartModule, IResourceConsumer
    {
        [KSPField]
        public float hpChangePerDay = 0;  // How many raw HP per day every affected kerbal gains

        [KSPField]
        public float hpMarginalChangePerDay = 0;  // If >0, will increase HP by this % of (MaxHP - HP). If <0, will decrease by this % of (HP - MinHP)

        [KSPField]
        public bool partCrewOnly = false;  // Does the module affect health of only crew in this part or the entire vessel?

        [KSPField]
        public float ecConsumption = 0;  // Flat EC consumption (units per second)

        [KSPField]
        public float ecConsumptionPerKerbal = 0;  // EC consumption per affected kerbal (units per second)

        [KSPField]
        public bool alwaysActive = false;  // Is the module's effect (and consumption) always active or togglable in-flight

        [KSPField(isPersistant = true)]
        public bool isActive = true;  // If not alwaysActive, this determines if the module is active

        [KSPField]
        public string multiplyFactor = "All";  // Name of factor whose effect is multiplied

        [KSPField]
        public float multiplier = 1;  // How the factor is changed (e.g., 0.5 means factor's effect is halved)

        [KSPField]
        public int crewCap = 0;  // Max crew this module's multiplier applies to without penalty

        double lastUpdated;

        public HealthFactor MultiplyFactor
        {
            get { return Core.FindFactor(multiplyFactor); }
            set { multiplyFactor = value.Id; }
        }

        public bool IsModuleActive
        { get { return alwaysActive || isActive; } }

        // Returns # of kerbals affected by this module, capped by crewCap
        public int AffectedCrewCount
        {
            get
            {
                int r = 0;
                if (Core.IsInEditor)
                    if (partCrewOnly)
                    {
                        foreach (PartCrewManifest pcm in ShipConstruction.ShipManifest.PartManifests)
                            foreach (ModuleKerbalHealth mkh in pcm.PartInfo.partPrefab.FindModulesImplementing<ModuleKerbalHealth>())
                                if (mkh == this) r = pcm.GetPartCrew().Length;
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
            if (ecConsumption != 0) return new List<PartResourceDefinition>() { PartResourceLibrary.Instance.GetDefinition("ElectricCharge") };
            else return new List<PartResourceDefinition>();
        }

        public override void OnStart(StartState state)
        {
            Core.Log("ModuleKerbalHealth.OnStart (" + state + ")");
            base.OnStart(state);
            lastUpdated = Planetarium.GetUniversalTime();
        }

        public void FixedUpdate()
        {
            if (Core.IsInEditor) return;
            double time = Planetarium.GetUniversalTime();
            if (IsModuleActive && ((ecConsumption != 0) || (ecConsumptionPerKerbal != 0)) && (TimeWarp.CurrentRate == 1))
            {
                Core.Log(AffectedCrewCount + " crew affected by this part.");
                double ec = (ecConsumption + ecConsumptionPerKerbal * AffectedCrewCount) * (time - lastUpdated), ec2;
                if ((ec2 = vessel.RequestResource(part, PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id, ec, false)) * 2 < ec)
                {
                    Core.Log("Module shut down due to lack of EC (" + ec + " needed, " + ec2 + " provided).");
                    ScreenMessages.PostScreenMessage("Kerbal Health Module shut down due to lack of EC.");
                    isActive = false;
                }
            }
            lastUpdated = time;
        }

        [KSPEvent(active = true, guiActive = true, name = "OnToggleActive", guiName = "Health Module")]
        public void OnToggleActive()
        {
            isActive = !isActive;
        }

        public override string GetInfo()
        {
            string res = "KerbalHealth Module";
            if (partCrewOnly) res += "\nAffects only part crew"; else res += "\nAffects entire vessel";
            if (hpChangePerDay != 0) res += "\nHP/day: " + hpChangePerDay.ToString("F1");
            if (hpMarginalChangePerDay != 0) res += "\nMarginal HP/day: " + hpMarginalChangePerDay.ToString("F1") + "%";
            if (multiplier != 1) res += "\n" + (multiplier > 0 ? "" : "+") + ((multiplier - 1) * 100).ToString("F0") + "% to " + multiplyFactor;
            if (crewCap > 0) res += " for up to " + crewCap + " kerbal" + (crewCap != 1 ? "s" : "");
            if (ecConsumption != 0) res += "\nElectric Charge: " + ecConsumption.ToString("F1") + "/sec.";
            if (ecConsumptionPerKerbal != 0) res += "\nEC per Kerbal: " + ecConsumptionPerKerbal.ToString("F1") + "/sec.";
            return res;
        }
    }
}
