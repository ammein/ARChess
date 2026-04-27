using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ARChess.Scripts.Utility
{
    public static class Log
    {
        [Conditional("UNITY_EDITOR")]
        public static void LogThis(string message, Object context)
        {
            Debug.Log(message, context);
        }
    }
}
