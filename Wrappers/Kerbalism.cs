using System;
using System.Linq;
using System.Reflection;

namespace KerbalHealth.Wrappers
{
    /// <summary>
    /// This class imports some of Kerbalism's methods. Its author is not affiliated with devs of Kerbalism in any way
    /// </summary>
    public static class Kerbalism
    {
        static bool? kerbalismFound;

        public static bool Found
        {
            get
            {
                if (!kerbalismFound.HasValue)
                    kerbalismFound = AssemblyLoader.loadedAssemblies.Select(a => new AssemblyName(a.assembly.FullName).Name).Contains("Kerbalism");
                return (bool)kerbalismFound;
            }
        }

        public static Func<bool> IsRadiationEnabled;

        public static void Init()
        {
            Type apiType = Type.GetType("KERBALISM.API");
            IsRadiationEnabled = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), apiType.GetMethod("RadiationEnabled"));
        }
    }
}
