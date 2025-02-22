using System;
using HarmonyLib;
using Timberborn.Beavers; 
using Timberborn.Characters;
using Timberborn.Population;
using Timberborn.Reproduction;

namespace CustomNameList
{
    class RandomNamePatch
    {
        [HarmonyPatch(typeof(BeaverNameService), "RandomName")]
        public static class PatchRandomNameGeneration
        {
            private static void Postfix(ref string __result)
            {
                Plugin.Log.LogInfo($"RandomName Called.");
                
                if (!Plugin.NameService.IsInitialized)
                    return;

                var nextRandomName = Plugin.NameService.NextName();
                if (nextRandomName != null)
                {
                    Plugin.Log.LogInfo($"Using random name: {nextRandomName}");
                    
                    __result = nextRandomName;
                }
                else
                {
                    Plugin.Log.LogInfo($"No names in list");
                }
            }
        }
    }

    class BeaverDiedPatch
    {
        [HarmonyPatch(typeof(CharacterPopulation), "OnCharacterKilled")]
        public static class PatchCharacterKilled
        {
            private static void Prefix(CharacterKilledEvent characterKilledEvent)
            {
                var characterName = characterKilledEvent.Character.FirstName;
                Plugin.Log.LogInfo($"Character {characterName} Killed.");
                
                if (!Plugin.NameService.IsInitialized)
                    return;

                if (Plugin.NameService.IsActive(characterName))
                {
                    Plugin.NameService.AddToQueue(characterName);
                }
            }
        }
    }
}