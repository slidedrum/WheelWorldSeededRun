using System;
using Il2CppSystem.Collections.Generic;

namespace SeededRun
{
    internal static class SeededSaveData
    {
        private const string SeedIdText = "70b84eb8-df3b-47e1-91dd-ea0ecf49c538";
        private const string SeedTextIdText = "d36eb23f-ac15-40fa-8022-cb7ca90656e7";
        private const string PlaytimeCheckpointIdText = "b9dc4682-1cc3-4ec3-b38f-d856ef732917";
        private const string InvalidSeedIdText = "1562971a-6c6f-44bd-8baf-62a52ec8ce7f";
        private const double PlaytimeToleranceSeconds = 1.0;

        private static readonly GlobalID SeedId = ParseId(SeedIdText);
        private static readonly GlobalID SeedTextId = ParseId(SeedTextIdText);
        private static readonly GlobalID PlaytimeCheckpointId = ParseId(PlaytimeCheckpointIdText);
        private static readonly GlobalID InvalidSeedId = ParseId(InvalidSeedIdText);

        internal static bool TryGetActiveSeed(out int seed)
        {
            seed = 0;

            SaveData_v3_latest saveData =
                MesshofBehaviourSingleton<SaveDataManager>.Instance?.Data;

            if (saveData?.gameStateLongInts == null)
                return false;

            if (!saveData.gameStateLongInts.TryGetValue(SeedId, out long savedSeed))
                return false;

            seed = unchecked((int)savedSeed);
            return true;
        }

        internal static string GetDisplaySeed()
        {
            SaveDataManager manager = MesshofBehaviourSingleton<SaveDataManager>.Instance;
            SaveData_v3_latest saveData = manager?.Data;

            if (saveData?.gameStateLongInts != null)
            {
                if (saveData.gameStateLongInts.TryGetValue(SeedId, out long savedSeed))
                {
                    int integerSeed = unchecked((int)savedSeed);
                    string seedText = GetSavedSeedText(saveData, integerSeed);
                    return IsInvalid(saveData) ? $"{seedText} (INVALID)" : seedText;
                }
            }

            if (manager == null || !manager.DataWasSavedToDisk)
            {
                string configuredSeed = Plugin.PartDropSeedConfig.Value;
                if (SeedParser.TryConvertToInt32(configuredSeed, out int _))
                    return configuredSeed;

                return "RANDOM";
            }

            return "UNSEEDED";
        }

        internal static bool AddSeedToNewSave(SaveData_v3_latest saveData)
        {
            if (saveData?.gameStateLongInts == null)
                return false;

            if (saveData.gameStateLongInts.ContainsKey(SeedId))
                return false;

            string seedText = GetConfiguredSeedOrCreateRandom();
            SeedParser.TryConvertToInt32(seedText, out int seed);

            saveData.gameStateLongInts.Add(SeedId, seed);
            SaveSeedText(saveData, seedText);
            RecordPlaytimeCheckpoint(saveData);

            Plugin.PluginLog.LogInfo($"Attached part-drop seed \"{seedText}\" to the new save (internal seed: {seed}).");

            return true;
        }

        internal static void ApplySeedPolicyToExistingSave(SaveDataManager manager)
        {
            SaveData_v3_latest saveData = manager?.Data;
            if (saveData?.gameStateLongInts == null)
                return;

            bool hasSavedSeed = saveData.gameStateLongInts.TryGetValue(SeedId, out long savedSeed);
            string configuredSeed = Plugin.PartDropSeedConfig.Value;

            if (!hasSavedSeed)
            {
                string seedText = Plugin.UseOnExistingSaveConfig.Value && !string.IsNullOrEmpty(configuredSeed) ? configuredSeed : CreateRandomSeed();
                ApplySeedToExistingSave(manager, saveData, seedText);
                return;
            }

            if (!Plugin.UseOnExistingSaveConfig.Value || !SeedParser.TryConvertToInt32(configuredSeed, out int integerSeed))
                return;

            if (unchecked((int)savedSeed) == integerSeed)
            {
                if (!TryGetSavedSeedText(saveData, out string savedText) || savedText != configuredSeed)
                {
                    SaveSeedText(saveData, configuredSeed);
                    RequestSave(manager);
                }

                return;
            }

            ApplySeedToExistingSave(manager, saveData, configuredSeed);
        }

        private static void ApplySeedToExistingSave(SaveDataManager manager, SaveData_v3_latest saveData, string seedText)
        {
            SeedParser.TryConvertToInt32(seedText, out int integerSeed);
            saveData.gameStateLongInts[SeedId] = integerSeed;
            saveData.gameStateLongInts[InvalidSeedId] = 1;
            SaveSeedText(saveData, seedText);
            RecordPlaytimeCheckpoint(saveData);
            RequestSave(manager);
            Plugin.PluginLog.LogWarning($"Applied seed \"{seedText}\" to an existing save. This save is permanently INVALID.");
        }

        internal static void RefreshPlaytimeCheckpoint(SaveDataManager manager)
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
                Invalidate(manager, saveData, "its playtime checkpoint is missing");
                return;
            }

            double checkpoint = BitConverter.Int64BitsToDouble(checkpointBits);
            if (PlaytimeMatchesCheckpoint(saveData.allPlaytimeSecs, checkpoint))
                return;

            Invalidate(manager, saveData, $"its playtime ({saveData.allPlaytimeSecs:F3}s) does not match SeededRun's checkpoint ({checkpoint:F3}s)");
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

        private static string GetSavedSeedText(SaveData_v3_latest saveData, int integerSeed)
        {
            if (TryGetSavedSeedText(saveData, out string savedText))
                return savedText;

            string configuredSeed = Plugin.PartDropSeedConfig.Value;
            if (SeedParser.TryConvertToInt32(configuredSeed, out int configuredIntegerSeed) && configuredIntegerSeed == integerSeed)
                return configuredSeed;

            return "UNKNOWN SEED";
        }

        private static string GetConfiguredSeedOrCreateRandom()
        {
            string configuredSeed = Plugin.PartDropSeedConfig.Value;
            return string.IsNullOrEmpty(configuredSeed) ? CreateRandomSeed() : configuredSeed;
        }

        private static string CreateRandomSeed()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static bool TryGetSavedSeedText(SaveData_v3_latest saveData, out string seedText)
        {
            seedText = null;

            if (saveData.completedChallengeSecondaryObjectives == null)
                return false;

            if (!saveData.completedChallengeSecondaryObjectives.TryGetValue(SeedTextId, out List<string> values) || values == null || values.Count == 0)
                return false;

            seedText = values[0];
            return !string.IsNullOrEmpty(seedText);
        }

        private static void SaveSeedText(SaveData_v3_latest saveData, string seedText)
        {
            if (!saveData.completedChallengeSecondaryObjectives.TryGetValue(SeedTextId, out List<string> values) || values == null)
            {
                values = new List<string>();
                saveData.completedChallengeSecondaryObjectives.Add(SeedTextId, values);
            }

            values.Clear();
            values.Add(seedText);
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

        private static void Invalidate(SaveDataManager manager, SaveData_v3_latest saveData, string reason)
        {
            saveData.gameStateLongInts[InvalidSeedId] = 1;
            RequestSave(manager);
            Plugin.PluginLog.LogError($"Seeded save is invalid because {reason}. The saved seed remains active, but this save will continue to display INVALID.");
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
