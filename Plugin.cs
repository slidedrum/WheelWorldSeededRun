using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System;

namespace SeededRun
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        internal static ConfigEntry<string> PartDropSeed;

        public override void Load()
        {
            Log = base.Log;

            PartDropSeed = Config.Bind("Seed", "PartDropSeed", "", "Leave empty to disable seeded drops.");

            Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            if (string.IsNullOrEmpty(PartDropSeed.Value))
            {
                Log.LogInfo("Part-drop seeding is disabled.");
            }
            else
            {
                TryGetPartDropSeed(out int seed);
                Log.LogInfo($"Part-drop seed: \"{PartDropSeed.Value}\" (internal seed: {seed})");
            }
        }

        internal static bool TryGetPartDropSeed(out int seed)
        {
            string value = PartDropSeed?.Value;

            if (string.IsNullOrEmpty(value))
            {
                seed = 0;
                return false;
            }

            // FNV-1a over the UTF-16 code units provides a stable mapping from
            // an arbitrary text seed to the integer required by Random.InitState.
            // Do not use string.GetHashCode(), because its output is not a
            // suitable persistent value across runtimes.
            unchecked
            {
                uint hash = 2166136261u;

                foreach (char character in value)
                {
                    hash ^= (byte)character;
                    hash *= 16777619u;
                    hash ^= (byte)(character >> 8);
                    hash *= 16777619u;
                }

                seed = (int)hash;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BikePart), nameof(BikePart.GetRandomPartDrop))]
    internal static class GetRandomPartDropPatch
    {
        private struct PatchState
        {
            public bool MustRestore;
            public UnityEngine.Random.State PreviousRandomState;
        }

        [HarmonyPrefix]
        private static void Prefix(out PatchState __state)
        {
            __state = default;

            // An empty seed leaves Unity's RNG completely untouched.
            if (!Plugin.TryGetPartDropSeed(out int seed))
                return;

            __state.PreviousRandomState = UnityEngine.Random.state;
            __state.MustRestore = true;

            UnityEngine.Random.InitState(seed);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, PatchState __state)
        {
            if (__state.MustRestore)
                UnityEngine.Random.state = __state.PreviousRandomState;

            return __exception;
        }
    }
}
