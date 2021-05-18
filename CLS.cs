using ConnectedLivingSpace;
using System;
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
                Type clsAddonType = AssemblyLoader.loadedAssemblies.SelectMany(a => a.assembly.GetExportedTypes()).SingleOrDefault(t => t.FullName == "ConnectedLivingSpace.CLSAddon");
                if (clsAddonType != null)
                    clsAddon = (ICLSAddon)clsAddonType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                return clsAddon;
            }
        }

        public static ICLSSpace GetCLSSpace(this ProtoCrewMember pcm) => CLSAddon.Vessel.Spaces.Find(space => space.Crew.Any(kerbal => kerbal.Kerbal == pcm));
    }
}
