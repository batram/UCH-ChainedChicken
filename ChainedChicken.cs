using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using BepInEx.Configuration;
using ChainedChicken.Patches;

[assembly: AssemblyVersion("0.0.0.3")]
[assembly: AssemblyInformationalVersion("0.0.0.3")]

namespace ChainedChicken
{
    [BepInPlugin("ChainedChicken", "ChainedChicken", "0.0.0.3")]
    public class ChainedChickenMod : BaseUnityPlugin
    {
        public static ConfigEntry<int> DefaultChainLength;
        private static Harmony ChainPatches;
        public const string ChainPlayersKey = "ChainPlayers";
        public const string ChainLengthKey = "ChainLength";

        void Awake()
        {
            Debug.Log("Let there be chains!");
            ChainPatches = new Harmony("ChainedChicken");
            ChainPatches.PatchAll();
            //Enabled = Config.Bind("General", "Enabled", true);
            DefaultChainLength = Config.Bind("General", "DefaultChainLength", 18, "defaull length of the chain (can be changed in game via modifiers)");

            CustomModdedModifiers.moddedMods.Add(ChainPlayersKey, new ModModMod(false, false, "Chain Players"));
            CustomModdedModifiers.moddedMods.Add(ChainLengthKey, new ModModMod(DefaultChainLength.Value, DefaultChainLength.Value, "Chain Length", new object[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }));
        }

        void OnDestroy()
        {
            Debug.Log("byebye");
            ChainPatches?.UnpatchSelf();
            UnloadResources();
        }

        public static void UnloadResources()
        {

        }

        public static bool isLocalOrModded()
        {
            return GameSettings.GetInstance().StartLocal || GameSettings.GetInstance().versionNumber.Contains("EvenMorePlayers");
        }
    }
}