using System.Reflection;
using DebugPlus.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace DebugPlus.Patches
{
    /// <summary>
    /// UnityEngine.Debug.Log(string message)
    /// </summary>
    internal class LogPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools
                .GetDeclaredMethods(typeof(Debug))
                .SingleDebug(m =>
                    m.Name == nameof(Debug.Log)
                    && m.GetParameters()[0].Name == "message"
                    && m.GetParameters().Length == 1
                );
        }

        [PatchPostfix]
        public static void PatchPostfix(object message)
        {
            if (!DebugPlusConfig.UnityInfoLogging.Value)
            {
                return;
            }

            if (message is string s)
            {
                DebugPlus.Log.LogInfo(Format.FormatString(s));
            }
        }
    }

    /// <summary>
    /// UnityEngine.Debug.Log(string message, Object object)
    /// </summary>
    internal class LogObjPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools
                .GetDeclaredMethods(typeof(Debug))
                .SingleDebug(m =>
                    m.Name == nameof(Debug.Log)
                    && m.GetParameters()[0].Name == "message"
                    && m.GetParameters().Length == 2
                );
        }

        [PatchPostfix]
        public static void PatchPostfix(object message, Object context)
        {
            if (!DebugPlusConfig.UnityInfoLogging.Value)
            {
                return;
            }

            if (message is string s && !context)
            {
                DebugPlus.Log.LogInfo(Format.FormatString(s));
                return;
            }

            DebugPlus.Log.LogInfo($"OBJECT: {message} : {context}");
        }
    }

    /// <summary>
    /// UnityEngine.Debug.LogFormat(string message, param object[] args)
    /// </summary>
    internal class LogFormatPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools
                .GetDeclaredMethods(typeof(Debug))
                .SingleDebug(m =>
                    m.Name == nameof(Debug.LogFormat)
                    && m.GetParameters()[0].Name == "format"
                    && m.GetParameters().Length == 2
                );
        }

        [PatchPostfix]
        public static void PatchPostfix(string format, object[] args)
        {
            if (!DebugPlusConfig.UnityInfoLogging.Value)
            {
                return;
            }

            DebugPlus.Log.LogInfo(Format.FormatString(format, args));
        }
    }

    /// <summary>
    /// UnityEngine.Debug.LogFormat(Object context, string message, param object[] args)
    /// </summary>
    internal class LogFormatObjPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools
                .GetDeclaredMethods(typeof(Debug))
                .SingleDebug(m =>
                    m.Name == nameof(Debug.LogFormat)
                    && m.GetParameters()[0].Name == "context"
                    && m.GetParameters().Length == 3
                );
        }

        [PatchPostfix]
        public static void PatchPostfix(Object context, string format, object[] args)
        {
            if (!DebugPlusConfig.UnityInfoLogging.Value)
            {
                return;
            }

            DebugPlus.Log.LogInfo(
                $"GameObject: {context} \nMessage : {Format.FormatString(format, args)}"
            );
        }
    }
}
