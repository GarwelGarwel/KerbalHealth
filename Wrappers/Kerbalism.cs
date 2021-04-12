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
                    featureLivingSpace = t.GetField("LivingSpace");
                    featureComfort = t.GetField("Comfort");
                    return true;
                }
                return (bool)kerbalismFound;
            }
        }

        public static bool IsSetup { get; set; }

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

        static FieldInfo GetField(string name) => kerbalismAssembly.GetType("KERBALISM.Rule").GetField(name);

        static object GetRule(string ruleName, string propertyName)
        {
            IEnumerable<object> rules;
            try { rules = (IEnumerable<object>)kerbalismAssembly.GetType("KERBALISM.Profile").GetField("rules").GetValue(null); }
            catch (ArgumentException e)
            {
                Core.Log($"KERBALISM.Profile.rules field not found. Exception: {e}", LogLevel.Error);
                return null;
            }
            return rules.FirstOrDefault(rule => (string)GetField("name").GetValue(rule) == ruleName);
        }

        public static object GetRuleProperty(string ruleName, string propertyName)
        {
            object rule = GetRule(ruleName, propertyName);
            if (rule != null)
                return GetField(propertyName).GetValue(rule);
            Core.Log($"Rule {ruleName} not found.", LogLevel.Error);
            return null;
        }

        public static void SetRuleProperty(string ruleName, string propertyName, object value)
        {
            object rule = GetRule(ruleName, propertyName);
            if (rule != null)
                GetField(propertyName).SetValue(rule, value);
            else Core.Log($"Rule {ruleName} not found.", LogLevel.Error);
        }

        public static double RadPerSecToBEDPerDay(double radPerSec) => radPerSec * KSPUtil.dateTimeFormatter.Day * 1e5;
    }
}
