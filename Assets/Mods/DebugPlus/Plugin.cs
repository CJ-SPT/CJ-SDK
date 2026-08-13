using System;
using BepInEx;
using BepInEx.Logging;
using CJ.ModSdk.Utils;

namespace DebugPlus;

[BepInPlugin("com.cj.debugplus", "DebugPlus", "1.4.0")]
internal class DebugPlus : BaseUnityPlugin
{
    public const int TarkovVersion = 40743;

    public static DebugPlus Instance { get; private set; }
    public ManualLogSource Log { get; private set; }

    internal void Awake()
    {
        if (!VersionChecker.CheckEftVersion(Logger, Config, TarkovVersion, Info.Metadata.Name))
        {
            throw new Exception("Invalid EFT Version");
        }
    }
}
