using System;
using BepInEx;
using BepInEx.Logging;
using CJ.ModSdk.Utils;
using DebugPlus.ConsoleCommands;
using DebugPlus.Patches;
using EFT.UI;

namespace DebugPlus;

[BepInPlugin("com.cj.debugplus", "DebugPlus", "1.4.0")]
[BepInDependency("com.cj.modsdk", "1.0.0")]
internal class DebugPlus : BaseUnityPlugin
{
    public static DebugPlus Instance { get; private set; }
    public static ManualLogSource Log { get; private set; }

    internal void Awake()
    {
        if (!VersionChecker.CheckEftVersion(Logger, Config, Info.Metadata.Name))
        {
            throw new Exception("Invalid EFT Version");
        }

        Instance = this;
        DontDestroyOnLoad(this);
        Log = Logger;

        DebugPlusConfig.InitConfig(Config);

        #region LOGGIN_PATCHES

        new LogPatch().Enable();
        new LogObjPatch().Enable();

        new LogFormatPatch().Enable();
        new LogFormatObjPatch().Enable();

        new LogWarningPatch().Enable();
        new LogWarningContextPatch().Enable();
        new LogWarningFormatPatch().Enable();
        new LogWarningFormatContextPatch().Enable();

        new LogErrorPatch().Enable();
        new LogErrorObjPatch().Enable();
        new LogErrorFormatPatch().Enable();
        new LogErrorFormatObjPatch().Enable();

        new LogExceptionPatch().Enable();
        new LogExceptionContextPatch().Enable();

        #endregion

        #region PLAYER_PATCHES

        new GodModePatch().Enable();
        new OnGameStartedPatch().Enable();

        #endregion

        #region OTHER

        new AfterApplicationLoadedPatch().Enable();

        #endregion

        RegisterCommands();
    }

    private static void RegisterCommands()
    {
        ConsoleScreen.Processor.RegisterCommand<SpawnBotsAsync>();
        ConsoleScreen.Processor.RegisterCommand<ReloadFromServerAsync>();
        ConsoleScreen.Processor.RegisterCommand<StartRaidAsync>();
    }
}
