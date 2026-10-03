using System;
using HarmonyLib;

namespace SeededRun
{
    [HarmonyPatch(typeof(BikePart), nameof(BikePart.GetRandomPartDrop))]
    internal static class PartDropPatch
    {
        private struct RandomState
        {
            internal bool MustRestore;
            internal UnityEngine.Random.State PreviousState;
        }

        [HarmonyPrefix]
        private static void Prefix(out RandomState __state)
        {
            __state = default;

            if (!SeededSaveData.TryGetActiveSeed(out int seed))
                return;

            __state.PreviousState = UnityEngine.Random.state;
            __state.MustRestore = true;
            UnityEngine.Random.InitState(seed);
        }

        [HarmonyFinalizer]
        private static Exception Finalizer(Exception __exception, RandomState __state)
        {
            if (__state.MustRestore)
                UnityEngine.Random.state = __state.PreviousState;

            return __exception;
        }
    }
}
