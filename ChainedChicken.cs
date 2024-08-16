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

[assembly: AssemblyVersion("0.0.0.1")]
[assembly: AssemblyInformationalVersion("0.0.0.1")]

namespace ChainedChickenMod
{
    [BepInPlugin("ChainedChicken", "ChainedChicken", "0.0.0.1")]
    public class ChainedChickenMod : BaseUnityPlugin
    {
        public static bool ChainedModifierEnabled;
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


        static bool isLocalOrModded()
        {
            return GameSettings.GetInstance().StartLocal || GameSettings.GetInstance().versionNumber.Contains("EvenMorePlayers");
        }

        static Texture2D getChainTex()
        {
            if (Tex2D)
            {
                return Tex2D;
            }

            string path = Path.Combine(Paths.PluginPath, "ChainedChickenMod", "link.png");

            if (File.Exists(path))
            {
                byte[] FileData = File.ReadAllBytes(path);
                Tex2D = new Texture2D(2, 2);          
                if (Tex2D.LoadImage(FileData))          
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

        [HarmonyPatch(typeof(TabletButton), nameof(TabletButton.OnAccept))]
        static class TabletButtonOnAcceptCtorPatch
        {
            static public void Prefix(TabletButton __instance)
            {
                if (__instance.gameObject.name == "Chain Players")
                {
                    var tbScreen = __instance.GetComponent<TabletButtonEventDispatcher>().tabletScreen;
                    __instance.GetComponent<TabletButtonEventDispatcher>().tabletScreen.OpenModalOverlay(TabletRule.None);
                    var tablOverlay = tbScreen.tablet.modalOverlay;

                    tablOverlay.titleText.text = "Chain Players Together";
                    tablOverlay.onOffContainer.gameObject.SetActive(true);
                    tablOverlay.SetOnOffButtonStyles(ChainedModifierEnabled);
                    tablOverlay.dataModel.Set<bool>("ChainPlayers", ChainedModifierEnabled);
                }
                if (__instance.gameObject.name == "On Button")
                {
                    var group = __instance.transform.parent.parent;
                    if (group.Find("Subtitle").GetComponent<TabletTextLabel>().text == "Chain Players Together")
                    {
                        Debug.Log("ChainPlayers on");
                        var tablOverlay = group.parent.GetComponent<TabletModalOverlay>();

                        ChainedModifierEnabled = true;
                        tablOverlay.dataModel.Set<bool>("ChainPlayers", ChainedModifierEnabled);
                        tablOverlay.rulesScreen.MarkRulesDirty();

                        TabletModalOverlay.BroadcastRuleChange(TabletRule.ModifierWallSlidesDisabled, 0, 0, ChainedModifierEnabled);
                        tablOverlay.Close();
                    }
                }
                if (__instance.gameObject.name == "Off Button")
                {
                    var group = __instance.transform.parent.parent;
                    if(group.Find("Subtitle").GetComponent< TabletTextLabel>().text == "Chain Players Together")
                    {
                        Debug.Log("ChainPlayers off");
                        var tablOverlay = group.parent.GetComponent<TabletModalOverlay>();

                        ChainedModifierEnabled = false;
                        tablOverlay.dataModel.Set<bool>("ChainPlayers", ChainedModifierEnabled);

                        TabletModalOverlay.BroadcastRuleChange(TabletRule.ModifierWallSlidesDisabled, 0, 0, ChainedModifierEnabled);
                        tablOverlay.Close();
                    }
                }
            }
        }

        
        [HarmonyPatch(typeof(TabletRulesScreen), nameof(TabletRulesScreen.UpdateButtonValue))]
        static class TabletUpdateButtonValuePatch
        {
            static void Postfix(TabletRulesScreen __instance, TabletRule overlayType, int buttonIndex, bool textSizeModifier)
            {
                if(overlayType == TabletRule.None)
                {
                    if(ChainModifiersEntry != null)
                    {
                        ChainModifiersEntry.transform.Find("ValueMod").GetComponent<TabletTextLabel>().text = Modifiers.GetOnOffValueString(ChainedModifierEnabled);
                        __instance.SetLineModified(ChainModifiersEntry.transform.Find("Text Label").GetComponent<TabletTextLabel>(), !ChainedModifierEnabled);
                    }
                }
            }
        }
        
        
        [HarmonyPatch(typeof(Tablet), nameof(Tablet.Start))]
        static class TabletStartPatch
        {
            static void Postfix(Tablet __instance)
            {
                var con = __instance.modifiersContainer.gameObject.transform.Find("Border/Modifiers BG/ScrollHolder/ItemContainer");
                Debug.Log("con name: " + con);

                var label = con.Find("Level Effects");
                Debug.Log("label: " + label);
                GameObject clonel = GameObject.Instantiate(label.gameObject);
                clonel.transform.SetParent(con, false);

                if (isLocalOrModded())
                {
                    clonel.GetComponent<TabletTextLabel>().text = "Modded";

                    var but = con.Find("Dance Invincibility");

                    GameObject clone = GameObject.Instantiate(but.gameObject);
                    clone.name = "Chain Players";
                    clone.transform.SetParent(con, false);
                    var evd = clone.GetComponent<TabletButtonEventDispatcher>();
                    evd.overlayType = TabletRule.None;

                    var tb = clone.GetComponent<TabletButton>();
                    tb.OnClick = null;

                    var tlabel = clone.transform.Find("Text Label");
                    tlabel.GetComponent<TabletTextLabel>().text = "Chain Players";
                    tlabel.GetComponent<I2.Loc.Localize>().enabled = false;
                    tlabel.GetComponent<LocalizationFontSizeSwitcher>().enabled = false;

                    ChainModifiersEntry = clone;
                    ChainModifiersEntry.transform.Find("ValueMod").GetComponent<TabletTextLabel>().text = Modifiers.GetOnOffValueString(ChainedModifierEnabled);
                } 
                else
                {
                    clonel.GetComponent<TabletTextLabel>().text = "Mods disabled (use MORE)";
                }

                /*
            TabletTextLabel[] componentsInChildren = valueLabel.transform.parent.GetComponentsInChildren<TabletTextLabel>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].UpdateDynamicText();
            } */
            }
        }


        [HarmonyPatch(typeof(TabletRulesScreen), nameof(TabletRulesScreen.handleEvent))]
        static class TabletRulesScreenHandleEventPatch
        {
            static void Prefix(TabletRulesScreen __instance, GameEvent.GameEvent e)
            {
                GameEvent.NetworkMessageReceivedEvent netw = e as GameEvent.NetworkMessageReceivedEvent;

                if (netw.Message.msgType == NetMsgTypes.GameRuleSet)
                {
                    MsgGameRuleSet msgGameRuleSet = (MsgGameRuleSet)netw.ReadMessage;

                    if(msgGameRuleSet.NewRule == TabletRule.ModifierWallSlidesDisabled) {
                        ChainedModifierEnabled = msgGameRuleSet.Valueb;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ModSource), nameof(ModSource.ReadFromXmlNode))]
        static class ModSourceFromXMLPatch
        {
            static void Postfix(ModSource __instance, XmlNode child)
            {
                Debug.Log("Futz ReadFromXmlNode");
                bool ogVal = ChainedModifierEnabled;
                ChainedModifierEnabled = QuickSaver.ParseAttrBool(child, "ChainPlayers", false);
                if (ChainedModifierEnabled && ogVal != ChainedModifierEnabled)
                {
                    GameEventManager.SendEvent(new ModifiersChangedEvent(TabletRule.None));
                }
                if (ChainModifiersEntry != null)
                {
                    ChainModifiersEntry.transform.Find("ValueMod").GetComponent<TabletTextLabel>().text = Modifiers.GetOnOffValueString(ChainedModifierEnabled);
                }
            }
        }

        [HarmonyPatch(typeof(ModSource), nameof(ModSource.WriteToXmlNode))]
        static class ModSourceWriteToXmlNodePatch
        {
            static void Postfix(ModSource __instance, XmlDocument doc, XmlElement modsNode)
            {
                Debug.Log("Futz WriteToXmlNode");
                QuickSaver.AddAttribute(doc, modsNode, "ChainPlayers", ChainedModifierEnabled.ToString());

            }
        }

        [HarmonyPatch(typeof(Modifiers), nameof(Modifiers.GetCurrentModifierListString))]
        static class ModifiersGetCurrentModifierListStringPatch
        {
            static void Postfix(Modifiers __instance, ref string __result)
            {
                Debug.Log("GetCurrentModifierListString");
                if (ChainedModifierEnabled)
                {
                    __result += "\n[modded] Chain Players Together ";
                }
            }
        }

        [HarmonyPatch(typeof(VersusControl), nameof(VersusControl.ToPlayMode))]
        static class VersusControlToPlayModePatch
        {
            static void Postfix(VersusControl __instance)
            {
                if(isLocalOrModded() && ChainedModifierEnabled)
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