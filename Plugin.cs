using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SeededRun
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public sealed class Plugin : BasePlugin
    {
        internal static ManualLogSource PluginLog { get; private set; }
        internal static ConfigEntry<string> PartDropSeedConfig { get; private set; }
        internal static ConfigEntry<bool> UseOnExistingSaveConfig { get; private set; }

        public override void Load()
        {
            PluginLog = Log;
            PartDropSeedConfig = Config.Bind("Seed", "PartDropSeed", "", "Leave empty to create an unseeded save.");
            UseOnExistingSaveConfig = Config.Bind("Seed", "UseOnExistingSave", false, "Force seed on current save.");

            new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();
            AddComponent<SeedWatermarkBehaviour>();

            PluginLog.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded.");

            if (SeedParser.TryConvertToInt32(PartDropSeedConfig.Value, out int seed))
            {
                PluginLog.LogInfo($"Part-drop seed for future new saves: \"{PartDropSeedConfig.Value}\" (internal seed: {seed}).");
            }
            else
            {
                PluginLog.LogInfo("No part-drop seed is configured for future new saves.");
            }
        }
    }
}
