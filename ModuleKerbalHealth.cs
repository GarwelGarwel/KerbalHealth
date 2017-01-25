using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalHealth
{
    public class ModuleKerbalHealth : PartModule
    {
        [KSPField]
        public bool partCrewOnly = false;  // Does the module affect health of only crew in this part or the entire vessel?

        [KSPField]
        public float hpChangePerDay = 0;  // How many raw HP per day every affected kerbal gains

        [KSPField]
        public float hpMarginalChangePerDay = 0;  // If >0, will increase HP by this fraction of (MaxHP - HP). If <0, will decrease by this fraction of (HP - MinHP)

        [KSPField]
        public float ecConsumptionFlat = 0;  // EC consumption (units per second)

        [KSPField]
        public float ecConsumptionPerKerbal = 0;  // EC consumption per affected kerbal (units per second)

        [KSPField]
        public bool alwaysActive = true;  // Is the module's effect (and consumption) always active or togglable in-flight

        [KSPField(isPersistant = true, guiActive = false, guiName = "Health Module Active")]
        public bool isActive = true;  // If not alwaysActive, this determines if the module is active

        double lastUpdated;

        public static bool IsModuleActive(ModuleKerbalHealth mkh)
        {
            return (mkh != null) && (mkh.alwaysActive || mkh.isActive);
        }

        public static bool IsModuleApplicable(PartCrewManifest part, ProtoCrewMember pcm)
        {
            ModuleKerbalHealth mkh = part?.PartInfo?.partPrefab?.FindModuleImplementing<ModuleKerbalHealth>();
            return IsModuleActive(mkh) && (!mkh.partCrewOnly || part.Contains(pcm));
        }

        public static bool IsModuleApplicable(ModuleKerbalHealth mkh, ProtoCrewMember pcm)
        {
            return IsModuleActive(mkh) && (!mkh.partCrewOnly || mkh.part.protoModuleCrew.Contains(pcm));
        }

        public override void OnAwake()
        {
            Log.Post("KerbalHealthModule.OnAwake");
            base.OnAwake();
        }

        public override void OnStart(StartState state)
        {
            Log.Post("KerbalHealthModule.OnStart (" + state + ")");
            base.OnStart(state);
            lastUpdated = Planetarium.GetUniversalTime();
        }

        public void FixedUpdate()
        {
            double time = Planetarium.GetUniversalTime();
            if (time - lastUpdated < Core.UpdateInterval) return;

        }

        public override string GetInfo()
        {
            return "KerbalHealth Module";
        }
    }
}
