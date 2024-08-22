using System;
using System.Collections;
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
        public static float chPullForce = 80f;
        public static float chFloorMul = 2.7f;
        public static float linkPullForce = 0.2f;

        public class ChainInfo : MonoBehaviour
        {
            public GameObject chain;
            public Character ch1;
            public Character ch2;
            public DistanceJoint2D dj;
            public List<GameObject> links = new List<GameObject>();
            public int length;

            void applyForce(GameObject target, GameObject towards, float f, bool close = false)
            {
                var rb = target.GetComponent<Rigidbody2D>();
                var dir = towards.transform.position - target.transform.position;
                dir = dir.normalized;
                if (dir.magnitude > 0.2f)
                {
                    if (!close || dir.magnitude < 2.4f)
                    {
                        var ch = target.GetComponent<Character>();
                        if (ch != null && ch.OnGround)
                        {
                            f = (f * chFloorMul) + 10;
                        }
                        rb.AddForce(dir * new Vector2(f * Time.deltaTime, f * Time.deltaTime), close ? ForceMode2D.Force : ForceMode2D.Impulse);
                    }
                }
            }

            void Update()
            {
                if (ch1 != null && ch2 != null && dj != null)
                {
                    if ((ch1.NetworkcurrentAnim == Character.AnimState.WIN && !ch1.Networksuccess)
                         || (ch2.NetworkcurrentAnim == Character.AnimState.WIN && !ch2.Networksuccess))
                    {
                        dj.distance -= 2f * Time.deltaTime;

                        if (ch1.NetworkcurrentAnim == Character.AnimState.WIN && !ch1.Networksuccess)
                        {
                            applyForce(ch2.gameObject, ch1.gameObject, chPullForce);

                            foreach (GameObject o in links)
                            {
                                applyForce(o, ch1.gameObject, linkPullForce, true);
                            }
                        }

                        if (ch2.NetworkcurrentAnim == Character.AnimState.WIN && !ch2.Networksuccess)
                        {
                            applyForce(ch1.gameObject, ch2.gameObject, chPullForce);
                            foreach (GameObject o in links)
                            {
                                applyForce(o, ch2.gameObject, linkPullForce, true);
                            }
                        }
                    }
                    else
                    {
                        dj.distance = length / 2;
                    }
                }
            }
        }

        public static void chainPlayersInTreehouse()
        {
            if (ChainedChickenMod.isLocalOrModded() && Matchmaker.InTreehouse)
            {
                ModdedModifiers modins = ModdedModifiers.GetWinstance();

                List<Character> clist = new List<Character>();
                foreach (LobbyPlayer gamePlayer in LobbyManager.instance.GetLobbyPlayers())
                {
                    if (gamePlayer != null && gamePlayer.CharacterInstance != null)
                    {
                        clist.Add(gamePlayer.CharacterInstance);
                    }
                }

                if (Modifiers.GetInstance().modsPreview
                    && modins.moddedMods.TryGetValue(ChainedChickenMod.ChainPlayersKey, out ModModMod bmod)
                    && (bool)bmod.value)
                {
                    int length = ChainedChickenMod.DefaultChainLength.Value;
                    if (modins.moddedMods.TryGetValue(ChainedChickenMod.ChainLengthKey, out ModModMod lmod) && lmod.value is int)
                    {
                        length = (int)lmod.value;
                    }

                    Chain.chainPlayers(clist, length);
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
            var chain = new GameObject
            {
                name = "Chain"
            };
            ChainInfo chainInfo = chain.AddComponent<ChainInfo>();

            chainInfo.chain = chain;
            chainInfo.ch1 = char1;
            chainInfo.ch2 = char2;
            chainInfo.length = length;

            gl.Add(chain);

            var obj = char1.gameObject;
            for (var i = 0; i <= length; i++)
            {
                obj = addLink(obj, i);
                obj.transform.SetParent(chain.transform);
                chainInfo.links.Add(obj);
                gl.Add(obj);
            }
            obj.name = "Last link";
            var hinge = obj.AddComponent<HingeJoint2D>();
            hinge.autoConfigureConnectedAnchor = false;
            hinge.connectedBody = char2.GetComponent<Rigidbody2D>();
            hinge.connectedAnchor = new Vector2(0, 0);
            hinge.anchor = new Vector2(0, 0);
            hinge.enableCollision = false;
            hinge.useLimits = false;

            var dis = char1.gameObject.AddComponent<DistanceJoint2D>();
            chainInfo.dj = dis;
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
            newLink.name = "Chain link";
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
        public static IEnumerator chainLater(float sec, List<Character> clist, int length)
        {
            yield return new WaitForSeconds(sec);
            chainPlayers(clist, length);
        }

        public static void chainPlayers(List<Character> clist, int length)
        {

            clearChain(clist);

            Debug.Log("clist.Count " + clist.Count);

            for (var i = 0; i < clist.Count - 1; i++)
            {
                chainChars(length, clist[i], clist[i + 1]);
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

        [HarmonyPatch(typeof(Character), nameof(Character.PositionCharacter))]
        static class CharacterPatch
        {
            static void Prefix(Character __instance)
            {
                //Disable chain
                var disj = __instance.gameObject.GetComponent<DistanceJoint2D>();
                if (disj != null)
                {
                    disj.enabled = false;
                }
            }
            public static IEnumerator enableLater(float sec, Character c)
            {
                yield return new WaitForSeconds(sec);
                var disj = c.gameObject.GetComponent<DistanceJoint2D>();
                if (disj != null)
                {

                    disj.enabled = true;
                }

            }

            static void Postfix(Character __instance)
            {
                //Enable chain
                var disj = __instance.gameObject.GetComponent<DistanceJoint2D>();
                if (disj != null)
                {
                    __instance.StartCoroutine(enableLater(0.3f, __instance));
                }
            }
        }

        [HarmonyPatch(typeof(GameControl), nameof(GameControl.ToPlayMode))]
        static class GameControlToPlayModePatch
        {
            static void Prefix(GameControl __instance)
            {
                List<Character> clist = new List<Character>();
                foreach (GamePlayer gamePlayer in __instance.PlayerQueue)
                {
                    if (gamePlayer != null && gamePlayer.CharacterInstance != null)
                    {
                        clist.Add(gamePlayer.CharacterInstance);
                    }
                }
                clearChain(clist);
            }

            static void Postfix(GameControl __instance)
            {
                if (ChainedChickenMod.isLocalOrModded())
                {
                    ModdedModifiers modins = ModdedModifiers.GetWinstance(); ;

                    if (modins.moddedMods.TryGetValue(ChainedChickenMod.ChainLengthKey, out ModModMod m) 
                    && (bool)m.value)
                    {
                        List<Character> clist = new List<Character>();
                        foreach (GamePlayer gamePlayer in __instance.PlayerQueue)
                        {
                            if (gamePlayer != null && gamePlayer.CharacterInstance != null)
                            {
                                clist.Add(gamePlayer.CharacterInstance);
                            }
                        }
                        clearChain(clist);
                        int length = ChainedChickenMod.DefaultChainLength.Value;
                        if (modins.moddedMods.TryGetValue(ChainedChickenMod.ChainLengthKey, out ModModMod mod) && mod.value is int)
                        {
                            length = (int)mod.value;
                        }
                        __instance.StartCoroutine(chainLater(0.3f, clist, length));
                    }
                }
            }
        }
    }
}