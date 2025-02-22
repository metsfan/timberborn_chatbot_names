using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using System.IO;
using UnityEngine;

namespace CustomNameList
{
    [BepInPlugin("com.aeskreis.beavernamelist", "Beaver Name Queue", "0.0.0.1")]
    [BepInProcess("Timberborn.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private static string _namesFilePath = $"{Path.GetDirectoryName(Paths.ExecutablePath)}{Path.DirectorySeparatorChar}names.txt";

        internal static CustomNameService NameService = new CustomNameService(_namesFilePath);

        private void Awake()
        {
            Log = base.Logger;

            NameService.Init();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            Logger.LogInfo($"Plugin com.aeskreis.beavernamelist is loaded!");
        }
    }
}
