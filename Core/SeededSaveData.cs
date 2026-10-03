using System;

namespace SeededRun
{
    internal static class SeededSaveData
    {
        private const string SeedIdText = "70b84eb8-df3b-47e1-91dd-ea0ecf49c538";
        private const string PlaytimeCheckpointIdText = "b9dc4682-1cc3-4ec3-b38f-d856ef732917";
        private const string InvalidSeedIdText = "1562971a-6c6f-44bd-8baf-62a52ec8ce7f";
        private const double PlaytimeToleranceSeconds = 1.0;

        private static readonly GlobalID SeedId = ParseId(SeedIdText);
        private static readonly GlobalID PlaytimeCheckpointId = ParseId(PlaytimeCheckpointIdText);
        private static readonly GlobalID InvalidSeedId = ParseId(InvalidSeedIdText);

        internal static bool TryGetActiveSeed(out int seed)
        {
            seed = 0;

            SaveData_v3_latest saveData =
                MesshofBehaviourSingleton<SaveDataManager>.Instance?.Data;

            if (saveData?.gameStateLongInts == null || IsInvalid(saveData))
                return false;

            if (!saveData.gameStateLongInts.TryGetValue(SeedId, out long savedSeed))
                return false;

            seed = unchecked((int)savedSeed);
            return true;
        }

        internal static bool AddConfiguredSeed(SaveData_v3_latest saveData)
        {
            if (saveData?.gameStateLongInts == null)
                return false;

            if (saveData.gameStateLongInts.ContainsKey(SeedId))
                return false;

            string configuredSeed = Plugin.PartDropSeedConfig.Value;
            if (!SeedParser.TryConvertToInt32(configuredSeed, out int seed))
                return false;

            saveData.gameStateLongInts.Add(SeedId, seed);
            RecordPlaytimeCheckpoint(saveData);

            Plugin.PluginLog.LogInfo($"Attached part-drop seed \"{configuredSeed}\" to the new save (internal seed: {seed}).");

            return true;
        }

        internal static void RecordPlaytimeCheckpointBeforeSave(SaveDataManager manager)
        {
            SaveData_v3_latest saveData = manager?.Data;
            if (!HasValidSeed(saveData))
                return;

            RecordPlaytimeCheckpoint(saveData);
        }

        internal static void ValidateAfterLoad(SaveDataManager manager)
        {
            SaveData_v3_latest saveData = manager?.Data;
            if (!HasValidSeed(saveData))
                return;

            if (!saveData.gameStateLongInts.TryGetValue(PlaytimeCheckpointId, out long checkpointBits))
            {
                MigrateMissingCheckpoint(manager, saveData);
                return;
            }

            double checkpoint = BitConverter.Int64BitsToDouble(checkpointBits);
            if (PlaytimeMatchesCheckpoint(saveData.allPlaytimeSecs, checkpoint))
                return;

            Invalidate(manager, saveData, checkpoint);
        }

        private static bool HasValidSeed(SaveData_v3_latest saveData)
        {
            return saveData?.gameStateLongInts != null
                && saveData.gameStateLongInts.ContainsKey(SeedId)
                && !IsInvalid(saveData);
        }

        private static bool IsInvalid(SaveData_v3_latest saveData)
        {
            return saveData.gameStateLongInts.TryGetValue(InvalidSeedId, out long invalid)
                && invalid != 0;
        }

        private static void RecordPlaytimeCheckpoint(SaveData_v3_latest saveData)
        {
            saveData.gameStateLongInts[PlaytimeCheckpointId] =
                BitConverter.DoubleToInt64Bits(saveData.allPlaytimeSecs);
        }

        private static bool PlaytimeMatchesCheckpoint(double playtime, double checkpoint)
        {
            if (double.IsNaN(playtime)
                || double.IsInfinity(playtime)
                || double.IsNaN(checkpoint)
                || double.IsInfinity(checkpoint))
            {
                return false;
            }

            return Math.Abs(playtime - checkpoint) <= PlaytimeToleranceSeconds;
        }

        private static void MigrateMissingCheckpoint(SaveDataManager manager, SaveData_v3_latest saveData)
        {
            RecordPlaytimeCheckpoint(saveData);
            RequestSave(manager);
            Plugin.PluginLog.LogWarning("Seeded save had no playtime checkpoint; created its initial checkpoint.");
        }

        private static void Invalidate(SaveDataManager manager, SaveData_v3_latest saveData, double checkpoint)
        {
            saveData.gameStateLongInts[InvalidSeedId] = 1;
            RequestSave(manager);

            Plugin.PluginLog.LogError($"Seeded save is invalid: its playtime ({saveData.allPlaytimeSecs:F3}s) does not match SeededRun's checkpoint ({checkpoint:F3}s). Seeded drops are permanently disabled for this save.");
        }

        private static void RequestSave(SaveDataManager manager)
        {
            manager.RequestSave_BecauseSomeCriticalProgressDataWasUpdated(manager);
        }

        private static GlobalID ParseId(string text)
        {
            if (!GlobalID.TryParse(text, out GlobalID id))
                throw new InvalidOperationException($"SeededRun save-data ID is invalid: {text}");

            return id;
        }
    }
}
