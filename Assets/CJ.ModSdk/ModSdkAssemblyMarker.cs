using System;
using BepInEx;
using CJ.ModSdk.Utils;

namespace CJ.ModSdk;

[BepInPlugin("com.cj.modsdk", "Cj Mod Sdk", "1.0.0")]
public class CjModSdk : BaseUnityPlugin
{
    public static string Guid = "com.cj.modsdk";

    internal void Awake()
    {
        if (!VersionChecker.CheckEftVersion(Logger, Config, Info.Metadata.Name))
        {
            throw new Exception("Invalid EFT Version");
        }
    }
}
