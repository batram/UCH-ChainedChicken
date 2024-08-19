using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using BepInEx.Configuration;
using ChainedChickenMod.Patches;

[assembly: AssemblyVersion("0.0.0.1")]
[assembly: AssemblyInformationalVersion("0.0.0.1")]

namespace ChainedChickenMod
{
    [BepInPlugin("ChainedChicken", "ChainedChicken", "0.0.0.1")]
    public class ChainedChickenMod : BaseUnityPlugin
    {
        public static ConfigEntry<int> ChainLength;

        void Awake()
        {
            Debug.Log("Let there be chains!");
            new Harmony("ChainedChicken").PatchAll();

            //Enabled = Config.Bind("General", "Enabled", true);
            ChainLength = Config.Bind("General", "ChainLength", 10, "Length of the chain");

            CustomModdedModifiers.moddedMods.Add("ChainPlayers", new ModModMod(false, false, "Chain Players"));
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