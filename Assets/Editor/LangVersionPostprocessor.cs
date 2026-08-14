using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps Unity's generated IDE projects aligned with the language version passed to Roslyn.
/// Unity 2022 emits C# 9 into csproj files even when /langversion:latest is configured.
/// </summary>
internal sealed class LangVersionPostprocessor : AssetPostprocessor
{
    private const string SyncSessionKey = "TarkovSdk.LangVersionProjectSync.v2";

    private static string OnGeneratedCSProject(string path, string content)
    {
        const string pattern = @"<LangVersion>.*?</LangVersion>";
        const string replacement = "<LangVersion>latest</LangVersion>";
        return Regex.Replace(content, pattern, replacement);
    }

    [InitializeOnLoadMethod]
    private static void ScheduleRiderProjectSync()
    {
        EditorApplication.delayCall += SyncRiderProjectsOnce;
    }

    [MenuItem("SDK/Mod Tools/Regenerate Project Files")]
    private static void RegenerateRiderProjectFiles()
    {
        SyncRiderProjects();
    }

    private static void SyncRiderProjectsOnce()
    {
        if (SessionState.GetBool(SyncSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(SyncSessionKey, true);
        SyncRiderProjects();
    }

    private static void SyncRiderProjects()
    {
        Type riderEditorType = Type.GetType(
            "Packages.Rider.Editor.RiderScriptEditor, Unity.Rider.Editor"
        );
        MethodInfo syncMethod = riderEditorType?.GetMethod(
            "SyncSolution",
            BindingFlags.Public | BindingFlags.Static
        );

        if (syncMethod == null)
        {
            Debug.LogWarning(
                "Rider project generator was not available. Use Preferences > External Tools > Regenerate project files."
            );
            return;
        }

        syncMethod.Invoke(null, null);
        Debug.Log("Rider project files regenerated with LangVersion=latest.");
    }
}
