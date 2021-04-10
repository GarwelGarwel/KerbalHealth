using System;
using System.Collections;
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
        public static Func<Vessel, double> GetRadiation;
        public static Func<Vessel, double> GetHabitatRadiation;

        static bool? kerbalismFound;
        static Assembly kerbalismAssembly;

        static FieldInfo featureRadiation;

        static FieldInfo featureLivingSpace;

        static FieldInfo featureComfort;

        public static bool Found
        {
            get
            {
                if (kerbalismFound == null)
                {
                    kerbalismAssembly = AssemblyLoader.loadedAssemblies
                        .FirstOrDefault(la => new AssemblyName(la.assembly.FullName)?.Name == "Kerbalism")?.assembly;
                    kerbalismFound = kerbalismAssembly != null;

                    if (kerbalismAssembly == null)
                        return false;

                    Core.Log("Kerbalism found. Initializing API.", LogLevel.Important);
                    Type t = kerbalismAssembly.GetType("KERBALISM.API");

                    GetRadiation = (Func<Vessel, double>)Delegate.CreateDelegate(typeof(Func<Vessel, double>), t.GetMethod("Radiation"));
                    GetHabitatRadiation = (Func<Vessel, double>)Delegate.CreateDelegate(typeof(Func<Vessel, double>), t.GetMethod("HabitatRadiation"));

                    t = kerbalismAssembly.GetType("KERBALISM.Features");
                    featureRadiation = t.GetField("Radiation");
                    featureLivingSpace = t.GetField("LivingSpace");
                    featureComfort = t.GetField("Comfort");
                    return true;
                }
                return (bool)kerbalismFound;
            }
        }

        public static bool IsSetup { get; set; }

        public static bool FeatureRadiation
        {
            get => (bool)featureRadiation.GetValue(null);
            set => featureRadiation.SetValue(null, value);
        }

        public static bool FeatureLivingSpace
        {
            get => (bool)featureLivingSpace.GetValue(null);
            set => featureLivingSpace.SetValue(null, value);
        }

        public static bool FeatureComfort
        {
            get => (bool)featureComfort.GetValue(null);
            set => featureComfort.SetValue(null, value);
        }

        public static object GetRuleProperty(string ruleName, string propertyName)
        {
            Type profileType = kerbalismAssembly.GetType("KERBALISM.Profile");
            Type ruleType = kerbalismAssembly.GetType("KERBALISM.Rule");
            IEnumerable rules;
            try { rules = (IEnumerable)profileType.GetField("rules").GetValue(null); }
            catch (ArgumentException e)
            {
                Core.Log($"KERBALISM.Profile.rules field not found. Exception: {e}", LogLevel.Error);
                return null;
            }
            foreach (object rule in rules)
                if ((string)ruleType.GetField("name").GetValue(rule) == ruleName)
                    return ruleType.GetField(propertyName).GetValue(rule);
            Core.Log($"Rule {ruleName} not found.", LogLevel.Error);
            return null;
        }

        public static void SetRuleProperty(string ruleName, string propertyName, object value)
        {
            Type profileType = kerbalismAssembly.GetType("KERBALISM.Profile");
            Type ruleType = kerbalismAssembly.GetType("KERBALISM.Rule");
            IEnumerable rules;
            try { rules = (IEnumerable)profileType.GetField("rules").GetValue(null); }
            catch (ArgumentException e)
            {
                Core.Log($"KERBALISM.Profile.rules field not found. Exception: {e}", LogLevel.Error);
                return;
            }
            foreach (object rule in rules)
                if ((string)ruleType.GetField("name").GetValue(rule) == ruleName)
                {
                    ruleType.GetField(propertyName).SetValue(rule, value);
                    return;
                }
            Core.Log($"Rule {ruleName} not found.", LogLevel.Error);
            return;
        }

        public static double RadToBED(double rad) => rad * 1e5;

        public static double BEDToRad(double bed) => bed / 1e5;

        public static double RadPerSecToBEDPerDay(double radPerSec) => radPerSec * KSPUtil.dateTimeFormatter.Day * 1e5;

        #region TESTING

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

        #endregion TESTING

        //public static void Init()
        //{
        //    if (kerbalismAssembly == null)
        //    {
        //        Core.Log("Kerbalism assembly not found and API could not be initialized.", LogLevel.Important);
        //        return;
        //    }
        //    Type t = kerbalismAssembly.GetType("KERBALISM.API");

        //    Radiation = (Func<Vessel, double>)Delegate.CreateDelegate(typeof(Func<Vessel, double>), t.GetMethod("Radiation"));
        //    HabitatRadiation = (Func<Vessel, double>)Delegate.CreateDelegate(typeof(Func<Vessel, double>), t.GetMethod("HabitatRadiation"));

        //    t = kerbalismAssembly.GetType("KERBALISM.Features");
        //    featureRadiation = t.GetField("Radiation");
        //    featureLivingSpace = t.GetField("LivingSpace");
        //    featureComfort = t.GetField("Comfort");

        //    IsInitialized = true;
        //}
    }
}
