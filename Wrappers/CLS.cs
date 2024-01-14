using ConnectedLivingSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KerbalHealth
{
    static class CLS
    {
        static ICLSAddon clsAddon;
        static bool? clsInstalled;

        public static bool Installed => (bool)(clsInstalled ?? (clsInstalled = CLSAddon != null));

        public static bool Enabled => KerbalHealthGeneralSettings.Instance.CLSIntegration && Installed;

        public static ICLSAddon CLSAddon
        {
            get
            {
                if (clsAddon == null)
                {
                    Type clsAddonType = AssemblyLoader.loadedAssemblies.SelectMany(a => a.assembly.GetExportedTypes()).SingleOrDefault(t => t.FullName == "ConnectedLivingSpace.CLSAddon");
                    if (clsAddonType != null)
                        clsAddon = (ICLSAddon)clsAddonType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                }
                return clsAddon;
            }
        }

        public static ICLSSpace GetCLSSpace(this ProtoCrewMember pcm, Vessel vessel = null)
        {
            if (!Installed || pcm == null)
                return null;
            if (Core.IsInEditor)
            {
                Part p = pcm.GetCrewPart();
                return CLSAddon.Vessel?.Spaces?.Find(space => space.Parts.Any(part => part.Part == p));
            }
            ICLSVessel clsVessel = vessel == null ? CLSAddon.Vessel : CLSAddon.getCLSVessel(vessel);
            return clsVessel?.Spaces.Find(space => space.Crew.Any(kerbal => kerbal.Kerbal.name == pcm.name));
        }

        public static IEnumerable<ProtoCrewMember> GetCrew(this ICLSSpace clsSpace) =>
            Core.IsInEditor ? ShipConstruction.ShipManifest.GetAllCrew(false).Where(pcm => pcm.GetCLSSpace() == clsSpace) : clsSpace.Crew.Select(kerbal => kerbal.Kerbal);

        public static int GetCrewCount(this ICLSSpace clsSpace) =>
            clsSpace == null
            ? 1
            : (Core.IsInEditor ? ShipConstruction.ShipManifest.GetAllCrew(false).Count(pcm => pcm.GetCLSSpace() == clsSpace) : clsSpace.Crew.Count);
    }
}
