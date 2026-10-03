using HarmonyLib;

namespace SeededRun
{
    // If vanilla saves without this mod, playtime advances but the checkpoint does not.
    [HarmonyPatch(typeof(SaveDataManager), "ReadSaveIntoData")]
    internal static class ValidateLoadedSavePatch
    {
        [HarmonyPostfix]
        private static void Postfix(SaveDataManager __instance)
        {
            SeededSaveData.ApplySeedPolicyToExistingSave(__instance);
            SeededSaveData.ValidateAfterLoad(__instance);
        }
    }

    // The mod keeps this synchronized in memory so every later save captures it.
    [HarmonyPatch(typeof(SaveDataManager), nameof(SaveDataManager.AfterAnimatorTick))]
    internal static class RefreshPlaytimeCheckpointPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SaveDataManager __instance)
        {
            SeededSaveData.RefreshPlaytimeCheckpoint(__instance);
        }
    }
}
