using System;
using UnityEngine;

namespace KerbalHealth
{
    class Log
    {
        public enum LogLevel { None, Error, Warning, Debug };
        public static LogLevel Level { get; set; } = LogLevel.Debug;

        public static void Post(string message, LogLevel messageLevel = LogLevel.Debug)
        {
            if (messageLevel <= Level)
                Debug.Log("[KerbalHealth] " + Time.realtimeSinceStartup + ": " + message);
        }
    }
}
