using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using BepInEx.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using GameEvent;
using ChainedChickenMod.Patches;

[assembly: AssemblyVersion("0.0.0.1")]
[assembly: AssemblyInformationalVersion("0.0.0.1")]

namespace ChainedChickenMod
{
    [BepInPlugin("ChainedChicken", "ChainedChicken", "0.0.0.1")]
    public class ChainedChickenMod : BaseUnityPlugin
    {
        public static GameObject ChainModifiersEntry;
        //public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<int> ChainLength;
        public static Texture2D Tex2D;
        public static List<GameObject> gl = new List<GameObject>();

        void Awake()
        {
            Debug.Log("Let there be chains!");
            new Harmony("ChainedChicken").PatchAll();

            //Enabled = Config.Bind("General", "Enabled", true);
            ChainLength = Config.Bind("General", "ChainLength", 10, "Length of the chain");
        }


        public static bool isLocalOrModded()
        {
            return GameSettings.GetInstance().StartLocal || GameSettings.GetInstance().versionNumber.Contains("EvenMorePlayers");
        }

        static Texture2D getChainTex()
        {
            if (Tex2D)
            {
                return Tex2D;
            }

            var assembly =  typeof(ChainedChickenMod).Assembly;
            var resourceStream = assembly.GetManifestResourceStream("ChainedChicken.link.png");

            if (resourceStream != null)
            {
                byte[] linkData = new byte[resourceStream.Length];
                resourceStream.Read(linkData, 0, linkData.Length);

                Tex2D = new Texture2D(2, 2);          
                if (Tex2D.LoadImage(linkData))          
                    return Tex2D;             
            }

            return null;
        }


        static void chainChars(int length, Character char1, Character char2)
        {
            var obj = char1.gameObject;
            for (var i = 0; i <= length; i++)
            {
                obj = addLink(obj, i);
                gl.Add(obj);
            }
            obj.name = "Last chain";
            var hinge = obj.AddComponent<HingeJoint2D>();
            hinge.autoConfigureConnectedAnchor = false;
            hinge.connectedBody = char2.GetComponent<Rigidbody2D>();
            hinge.connectedAnchor = new Vector2(0, 0);
            hinge.anchor = new Vector2(0, 0);
            hinge.enableCollision = false;
            hinge.useLimits = false;

            var dis = char1.gameObject.AddComponent<DistanceJoint2D>();
            dis.enableCollision = false;
            dis.maxDistanceOnly = true;
            dis.autoConfigureConnectedAnchor = false;
            dis.autoConfigureDistance = false;
            dis.connectedBody = char2.gameObject.GetComponent<Rigidbody2D>();
            dis.connectedAnchor = new Vector2(0, 0);
            dis.distance = length  / 2;
        }

        static GameObject addLink(GameObject go, int offset)
        {
            Texture2D tex = getChainTex();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 279, 102), new Vector2(0.5f, 0.5f));
            GameObject newLink = new GameObject();
            newLink.transform.localScale = new Vector3(0.2f, 0.2f, 0);
            var rig = newLink.AddComponent<Rigidbody2D>();
            rig.mass = 0.00f;
            rig.gravityScale = 0.52f;
            rig.drag = 0.0f;
            rig.inertia = 0.0f;
            rig.angularDrag = 0.0f;
            rig.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            var hinge = newLink.AddComponent<HingeJoint2D>();

            hinge.autoConfigureConnectedAnchor = false;
            hinge.connectedBody = go.GetComponent<Rigidbody2D>();
            if (offset == 0)
            {
                hinge.connectedAnchor = new Vector2(0, 0);
            }
            else
            {
                hinge.connectedAnchor = new Vector2(1.8f, 0);
            }
            hinge.anchor = new Vector2(-0.6f, 0);
            hinge.enableCollision = false;
            hinge.useLimits = false;

            SpriteRenderer SR = newLink.AddComponent<SpriteRenderer>();
            SR.sprite = sprite;
            SR.transform.position = go.transform.position + new Vector3(0.36f, 0, 0);
            return newLink;
        }

        public static void chainPlayers(List<Character> clist)
        {

            foreach(GameObject go in gl)
            {
                Destroy(go);
            }
            gl = new List<GameObject>();

            foreach (Character c in clist)
            {
                if (c != null)
                {
                    Destroy(c.gameObject.GetComponent<DistanceJoint2D>());
                }
            }

            Debug.Log("clist.Count " + clist.Count);

            for(var i = 0; i < clist.Count - 1; i++)
            {
                chainChars(ChainLength.Value, clist[i], clist[i + 1]);
            }
        }

        [HarmonyPatch(typeof(GameControl), nameof(GameControl.ToPlayMode))]
        static class GameControlToPlayModePatch
        {
            static void Postfix(GameControl __instance)
            {
                if(isLocalOrModded())
                {
                    ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();
                    
                    if(modins.moddedMods.ContainsKey("ChainPlayers") && (bool)modins.moddedMods["ChainPlayers"].value)
                    {
                        List<Character> clist = new List<Character>();
                        foreach (GamePlayer gamePlayer in __instance.PlayerQueue)
                        {
                            if (gamePlayer != null && gamePlayer.CharacterInstance != null)
                            {
                                clist.Add(gamePlayer.CharacterInstance);
                            }
                        }


                        chainPlayers(clist);
                    }
                }
            }
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