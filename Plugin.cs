using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace CustomNameList
{
    [BepInPlugin("com.aeskreis.beavernamelist", "Beaver Name Queue", "0.0.0.1")]
    [BepInProcess("Timberborn.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        internal static CustomNameService NameService = new();

        private void Awake()
        {
            Log = base.Logger;

            NameService.Init(Log);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            Logger.LogInfo($"Plugin com.aeskreis.beavernamelist is loaded!");
        }
    }
}
