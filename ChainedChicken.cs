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

        [HarmonyPatch(typeof(Character), nameof(Character.Update))]
        static class CharacterUpdatePatch
        {
            static void Postfix(Character __instance)
            {
                //PULL on dance


                /*
                if(gl == null)
                {
                    return;
                }
                for (var i = 1; i < gl.Count; i++)
                {
                    var link = gl[i];
                    var oglink = gl[0];

                    if(link != null && oglink != null)
                    {
                        oglink.name = "OG link";

                        var rig = link.GetComponent<Rigidbody2D>();
                        var ogrig = oglink.GetComponent<Rigidbody2D>();
                        if (rig != null && ogrig != null)
                        {
                            rig.mass = ogrig.mass;
                            rig.gravityScale = ogrig.gravityScale;
                            rig.drag = ogrig.drag;
                            rig.inertia = ogrig.inertia;
                            rig.angularDrag = ogrig.angularDrag;
                            rig.collisionDetectionMode = ogrig.collisionDetectionMode;
                            rig.angularVelocity = ogrig.angularVelocity;
                        }
                        var hinge = link.GetComponent<HingeJoint2D>();
                        var oghinge = oglink.GetComponent<HingeJoint2D>();

                        if (hinge != null && oghinge != null)
                        {

                            hinge.autoConfigureConnectedAnchor = oghinge.autoConfigureConnectedAnchor;
                            //hinge.connectedAnchor = oghinge.connectedAnchor;
                            //hinge.anchor = oghinge.anchor;
                            hinge.enableCollision = oghinge.enableCollision;
                            hinge.useLimits = oghinge.useLimits;
                        }

                    }

                }
*/
            }
        }

    }
}