using System;
using System.Diagnostics;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace CJ.ModSdk.Utils;

[AttributeUsage(AttributeTargets.Assembly)]
public class VersionChecker : Attribute
{
    public const int TarkovVersion = 40743;

    public static bool CheckEftVersion(
        ManualLogSource logger,
        ConfigFile configFile,
        string modName
    )
    {
        int currentVersion = FileVersionInfo
            .GetVersionInfo(BepInEx.Paths.ExecutablePath)
            .FilePrivatePart;

        if (currentVersion == TarkovVersion)
        {
            return true;
        }

        string errorMessage =
            $"ERROR: {modName} was built for Tarkov {TarkovVersion}, but you are running {currentVersion}. Please download the correct plugin version.";
        logger.LogError(errorMessage);
        Chainloader.DependencyErrors.Add(errorMessage);

        configFile.Bind(
            "",
            "TarkovVersion",
            "",
            new ConfigDescription(
                errorMessage,
                null,
                new ConfigurationManagerAttributes
                {
                    CustomDrawer = ErrorLabelDrawer,
                    ReadOnly = true,
                    HideDefaultButton = true,
                    HideSettingName = true,
                    Category = null,
                }
            )
        );

        return false;
    }

    private static void ErrorLabelDrawer(ConfigEntryBase entry)
    {
        var styleNormal = new GUIStyle(GUI.skin.label) { wordWrap = true, stretchWidth = true };
        var styleError = new GUIStyle(GUI.skin.label)
        {
            stretchWidth = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.red },
            fontStyle = FontStyle.Bold,
        };

        GUILayout.BeginVertical();
        GUILayout.Label(entry.Description.Description, styleNormal, GUILayout.ExpandWidth(true));
        GUILayout.Label("Plugin has been disabled!", styleError, GUILayout.ExpandWidth(true));
        GUILayout.EndVertical();
    }
}
