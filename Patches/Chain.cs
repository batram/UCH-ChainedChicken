using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Xml;
using GameEvent;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace ChainedChickenMod.Patches
{
    public class Chain
    {
        public static Texture2D Tex2D;
        public static List<GameObject> gl = new List<GameObject>();

        public static void chainPlayersInTreehouse()
        {
            if (ChainedChickenMod.isLocalOrModded() && Matchmaker.InTreehouse)
            {
                ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();

                List<Character> clist = new List<Character>();
                foreach (LobbyPlayer gamePlayer in LobbyManager.instance.GetLobbyPlayers())
                {
                    if (gamePlayer != null && gamePlayer.CharacterInstance != null)
                    {
                        clist.Add(gamePlayer.CharacterInstance);
                    }
                }

                if (Modifiers.GetInstance().modsPreview && modins.moddedMods.ContainsKey("ChainPlayers") && (bool)modins.moddedMods["ChainPlayers"].value)
                {
                    Chain.chainPlayers(clist);
                }
                else
                {
                    Chain.clearChain(clist);
                }
            }
        }

        static Texture2D getChainTex()
        {
            if (Tex2D)
            {
                return Tex2D;
            }

            var assembly = typeof(ChainedChickenMod).Assembly;
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
            dis.distance = length / 2;
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

        public static void clearChain(List<Character> clist)
        {
            foreach (GameObject go in gl)
            {
                GameObject.Destroy(go);
            }
            gl = new List<GameObject>();

            foreach (Character c in clist)
            {
                if (c != null)
                {
                    GameObject.Destroy(c.gameObject.GetComponent<DistanceJoint2D>());
                }
            }
        }

        public static void chainPlayers(List<Character> clist)
        {

            clearChain(clist);

            Debug.Log("clist.Count " + clist.Count);

            for (var i = 0; i < clist.Count - 1; i++)
            {
                chainChars(ChainedChickenMod.ChainLength.Value, clist[i], clist[i + 1]);
            }
        }

        [HarmonyPatch]
        static class ChainUpInLobby
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(Modifiers), nameof(Modifiers.GetCurrentModifierListString));
                yield return AccessTools.Method(typeof(Modifiers), nameof(Modifiers.OnModifiersDynamicChange));
                yield return AccessTools.Method(typeof(LobbyPlayer), nameof(LobbyPlayer.RpcRequestPickResponse));
            }

            static public void Prefix()
            {
                Chain.chainPlayersInTreehouse();
            }
        }

        [HarmonyPatch(typeof(GameControl), nameof(GameControl.ToPlayMode))]
        static class GameControlToPlayModePatch
        {
            static void Postfix(GameControl __instance)
            {
                if (ChainedChickenMod.isLocalOrModded())
                {
                    ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();

                    if (modins.moddedMods.ContainsKey("ChainPlayers") && (bool)modins.moddedMods["ChainPlayers"].value)
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
    }
}