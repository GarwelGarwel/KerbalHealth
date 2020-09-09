using System;
using System.Collections.Generic;
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
        static Assembly kerbalismAssembly;

        public static bool Found
        {
            get
            {
                if (kerbalismFound == null)
                {
                    kerbalismAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(la => la.name == "Kerbalism18").assembly;
                    kerbalismFound = kerbalismAssembly != null;
                }
                return (bool)kerbalismFound;
            }
        }

        public static Func<bool> IsRadiationEnabled;
        public static Func<Vessel, double> Radiation;
        public static Func<Vessel, double> HabitatRadiation;

        public static double RadToBED(double rad) => rad * 1e5;
        public static double BEDToRad(double bed) => bed / 1e5;
        public static double RadPerSecToBEDPerDay(double radPerSec) => radPerSec * KSPUtil.dateTimeFormatter.Day * 1e5;

        public static List<Tuple<string, double, double, double>> radiationComparison = new List<Tuple<string, double, double, double>>();

        public static void AddRadiationMeasurement(string body, double altitude, double kerbalHealth, double kerbalism) => radiationComparison.Add(new Tuple<string, double, double, double>(body, altitude, kerbalHealth, kerbalism));

        public static void PrintRadiationMeasurements()
        {
            if (radiationComparison.Count == 0)
                return;
            Core.Log($"Average KH/Kerbalism coefficient: {radiationComparison.Sum(t => t.Item3) / radiationComparison.Sum(t => t.Item4)}.");
            Core.Log($"Coefficients varied from {radiationComparison.Min(t => t.Item3 / t.Item4)} to {radiationComparison.Max(t => t.Item3 / t.Item4)}.");
            Core.Log($"KerbalHealth radiation varied from {radiationComparison.Min(t => t.Item3):N2} to {radiationComparison.Max(t => t.Item3):N2}.");
            Core.Log($"Kerbalism radiation varied from {radiationComparison.Min(t => t.Item4):N2} to {radiationComparison.Max(t => t.Item4):N2}.");
            string s = "";
            foreach (Tuple<string, double, double, double> t in radiationComparison)
                s += $"{t.Item1},{t.Item2},{t.Item3},{t.Item4}\n";
            Core.Log($"Radiation measurements:\n{s}");
        }

        public static void Init()
        {
            if (!Found)
            {
                Core.Log("Kerbalism not found and API could not be initialized.", LogLevel.Important);
                return;
            }
            Type apiType = kerbalismAssembly.GetType("KERBALISM.API");
            IsRadiationEnabled = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), apiType.GetMethod("RadiationEnabled"));
            Radiation = (Func<Vessel, double>)Delegate.CreateDelegate(typeof(Func<Vessel, double>), apiType.GetMethod("Radiation"));
            HabitatRadiation = (Func<Vessel, double>)Delegate.CreateDelegate(typeof(Func<Vessel, double>), apiType.GetMethod("HabitatRadiation"));
        }
    }
}
