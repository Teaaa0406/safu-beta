using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tea.Safu.SusDebug
{
    public static class SusDebugger
    {
        public static void Log(string msg)
        {
            Debug.Log(CreateLogMessage(msg));
        }

        public static void LogWarning(string msg)
        {
            Debug.LogWarning(CreateLogMessage(msg));
        }

        public static void LogError(string msg)
        {
            Debug.LogError(CreateLogMessage(msg));
        }

        private static string CreateLogMessage(string msg)
        {
            return $"<color=aqua>[SusAnalyzer]</color> {msg}";
        }
    }
}
