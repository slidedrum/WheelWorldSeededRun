using HarmonyLib;

namespace SeededRun
{
    [HarmonyPatch(typeof(StartMenuInGame), nameof(StartMenuInGame.DoNewGame))]
    internal static class StartNewGamePatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            SaveDataManager manager = MesshofBehaviourSingleton<SaveDataManager>.Instance;
            if (manager == null)
                return;

            if (manager.SaveExists())
            {
                NewGameSeedContext.MarkNextArchiveAsNewGame();
                return;
            }

            if (SeededSaveData.AddSeedToNewSave(manager.Data))
                manager.RequestSave_BecauseSomeCriticalProgressDataWasUpdated(manager);
        }
    }

    [HarmonyPatch(typeof(SaveDataManager), nameof(SaveDataManager.ArchiveThenDeleteCurrentSave))]
    internal static class NewGameArchivePatch
    {
        [HarmonyPrefix]
        private static void Prefix(out bool __state)
        {
            __state = NewGameSeedContext.ConsumeNewGameArchiveMarker();
        }

        [HarmonyPostfix]
        private static void Postfix(SaveDataManager __instance, bool __state)
        {
            if (!__state)
                return;

            if (SeededSaveData.AddSeedToNewSave(__instance.Data))
                __instance.RequestSave_BecauseSomeCriticalProgressDataWasUpdated(__instance);
        }
    }
}
