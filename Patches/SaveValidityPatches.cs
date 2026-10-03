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
            SeededSaveData.ValidateAfterLoad(__instance);
        }
    }

    // This private method runs immediately before the game serializes save data.
    [HarmonyPatch(typeof(SaveDataFuncs), "TrackSavedToDisk")]
    internal static class RecordPlaytimeCheckpointPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SaveDataManager manager)
        {
            SeededSaveData.RecordPlaytimeCheckpointBeforeSave(manager);
        }
    }
}
