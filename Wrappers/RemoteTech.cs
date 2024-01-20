using System;
using System.Reflection;
using System.Linq;

namespace KerbalHealth
{
    static class RemoteTech
    {
        static bool? installed;
        static Type api;
        static MethodInfo hasConnectionToKSC;

        public static bool Installed
        {
            get
            {
                if (installed != null)
                    return (bool)installed;
                api = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.name == "RemoteTech")?.assembly?.GetType("RemoteTech.API.API", false);
                hasConnectionToKSC = api?.GetMethod("HasConnectionToKSC");
                return (bool)(installed = hasConnectionToKSC != null);
            }
        }

        /// <summary>
        /// Returns true if the vessel is connected to KSC. Must check Installed before calling this method
        /// </summary>
        public static bool IsConnectedWithRemoteTech(this Vessel v)
        {
            if (hasConnectionToKSC?.Invoke(null, new object[] { v?.id }) is bool result)
                return result;
            Core.Log($"Tried to get RemoteTech connection state for {v?.name}, but hasConnectionToKSC returned a strange value. RemoteTech API is {(Installed ? "present" : "absent")}. hasConnectionToKSC is {(hasConnectionToKSC != null ? "found" : "not found")}.", LogLevel.Important);
            return false;
        }
    }
}
