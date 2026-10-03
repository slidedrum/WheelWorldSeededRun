using System;
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
            if (manager == null || manager.SaveExists())
                return;

            if (SeededSaveData.AddConfiguredSeed(manager.Data))
                manager.RequestSave_BecauseSomeCriticalProgressDataWasUpdated(manager);
        }
    }

    // This private method is the confirmation path for replacing an existing save.
    [HarmonyPatch(typeof(StartMenuInGame), "DoNewGame_Accept")]
    internal static class ConfirmNewGamePatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            NewGameSeedContext.MarkNextArchiveAsNewGame();
        }
    }

    [HarmonyPatch(typeof(SaveDataManager), nameof(SaveDataManager.ArchiveThenDeleteCurrentSave))]
    internal static class NewGameArchivePatch
    {
        [HarmonyPrefix]
        private static void Prefix(out bool __state)
        {
            __state = NewGameSeedContext.BeginArchive();
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, bool __state)
        {
            if (__state)
                NewGameSeedContext.EndArchive();

            return __exception;
        }
    }

    // ArchiveThenDeleteCurrentSave constructs the replacement before saving it.
    [HarmonyPatch(typeof(SaveData_v3_latest), MethodType.Constructor, new Type[] { })]
    internal static class NewSaveDataConstructorPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SaveData_v3_latest __instance)
        {
            if (NewGameSeedContext.IsCreatingNewGameSave)
                SeededSaveData.AddConfiguredSeed(__instance);
        }
    }
}
