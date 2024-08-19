using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Xml;
using GameEvent;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace ChainedChickenMod.Patches
{
    public class ModdedModifiers : Modifiers
    {
        static public bool modded = true;

        public Dictionary<String, ModModMod> moddedMods = new Dictionary<String, ModModMod>();

        public static new ModSource DefaultModSource
        {
            get
            {
                if (Modifiers.defaultModSource == null)
                {
                    ModdedModSource modsa = new ModdedModSource();
                    modsa.moddedMods.Add("ChainPlayers", new ModModMod(false, false, "Chain Players"));
                    Modifiers.defaultModSource = modsa;
                }
                return Modifiers.defaultModSource;
            }
        }

        [HarmonyPatch(typeof(Modifiers), nameof(Modifiers.GetInstance))]
        static class ModifiersGetInstancePatch
        {
            static public bool Prefix(out Modifiers __result)
            {
                if (Modifiers.instance == null)
                {
                    Modifiers bas = Resources.Load("ModifierSettings") as Modifiers;
                    ModdedModifiers modsa = ScriptableObject.CreateInstance<ModdedModifiers>();
                    modsa.moddedMods.Add("ChainPlayers", new ModModMod(false, false, "Chain Players"));
                    Modifiers.instance = modsa;
                }

                __result = Modifiers.instance;
                return false;
            }
        }

    }

    public class ModModMod
    {

        public System.Object value;
        public System.Object defaultValue;
        public string labelText;

        public ModModMod(System.Object value, System.Object defaultValue, string labelText)
        {
            this.value = value;
            this.defaultValue = defaultValue;
            this.labelText = labelText;
        }

    }

    public class ModdedModSource : ModSource
    {
        static public bool modded = true;

        public Dictionary<String, ModModMod> moddedMods = new Dictionary<String, ModModMod>();


        [HarmonyPatch(typeof(ModSource), nameof(ModSource.WriteToModSettings))]
        static class ModSourceWriteToModSettingsPatch
        {
            static public void Postfix(ModSource __instance, bool includeTreehouseSettings)
            {

                if (__instance.GetType() != typeof(ModdedModSource) || Modifiers.GetInstance().GetType() != typeof(ModdedModifiers))
                {
                    return;
                }
                Debug.Log("what the futz WriteToModSettings: __instance " + __instance.GetType() + " Modifiers : " + Modifiers.GetInstance().GetType());


                ModdedModSource mos = (ModdedModSource)__instance;
                ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();

                foreach (string k in mos.moddedMods.Keys)
                {
                    if (!modins.moddedMods.ContainsKey(k))
                    {
                        modins.moddedMods.Add(k, mos.moddedMods[k]);
                    }
                    else
                    {
                        modins.moddedMods[k] = mos.moddedMods[k];
                    }
                }
            }
        }


        [HarmonyPatch(typeof(ModSource), nameof(ModSource.IsCurrentlyApplied))]
        static class ModSourceIsCurrentlyAppliedPatch
        {
            static public void Postfix(ModSource __instance, ref bool __result)
            {

                if (!__result)
                {
                    __result = false;
                }

                if (__instance.GetType() != typeof(ModdedModSource) || Modifiers.GetInstance().GetType() != typeof(ModdedModifiers))
                {
                    return;
                }
                Debug.Log("what the futz IsCurrentlyApplied");
                Debug.Log("what the futz IsCurrentlyApplied: __instance " + __instance.GetType() + " Modifiers : " + Modifiers.GetInstance().GetType());


                ModdedModSource mos = (ModdedModSource)__instance;
                ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();
                foreach (string k in mos.moddedMods.Keys)
                {
                    if (!modins.moddedMods.ContainsKey(k) || mos.moddedMods[k].value != modins.moddedMods[k].value)
                    {
                        __result = false;
                    }
                }

                __result = true;
            }
        }

        [HarmonyPatch(typeof(ModSource), nameof(ModSource.CompareTo))]
        static class ModSourceCompareToPatch
        {
            static public void Postfix(ModSource __instance, ModSource other, ref bool __result)
            {
                Debug.Log("what the futz CompareTo");

                if (!__result)
                {
                    __result = false;
                }
                ModdedModSource mos = (ModdedModSource)__instance;

                foreach (string k in mos.moddedMods.Keys)
                {
                    if (!((ModdedModSource)other).moddedMods.ContainsKey(k) || (bool)mos.moddedMods[k].value != (bool)((ModdedModSource)other).moddedMods[k].value)
                    {
                        __result = false;
                    }
                }

                __result = true;
            }
        }

        [HarmonyPatch(typeof(ModSource), nameof(ModSource.WriteToXmlNode))]
        static class ModSourceWriteToXmlNodeNodePatch
        {
            static public void Prefix(ModSource __instance, XmlDocument doc, XmlElement modsNode)
            {
                Debug.Log("what the futz WriteToXmlNode");
                ModdedModSource mos = (ModdedModSource)__instance;
                foreach (string k in mos.moddedMods.Keys)
                {
                    QuickSaver.AddAttribute(doc, modsNode, k, mos.moddedMods[k].value.ToString());
                }
            }
        }


        [HarmonyPatch(typeof(ModSource), nameof(ModSource.ReadFromXmlNode))]
        static class ModSourceReadFromXmlNodePatch
        {
            static public void Prefix(ModSource __instance, XmlNode child)
            {
                Debug.Log("what the futz ReadFromXmlNode");
                ModdedModSource mos = (ModdedModSource)__instance;
                foreach (string k in mos.moddedMods.Keys)
                {
                    if (mos.moddedMods[k].value is bool)
                    {
                        mos.moddedMods[k].value = QuickSaver.ParseAttrBool(child, k, (bool)mos.moddedMods[k].defaultValue);
                    }
                }
            }
        }
    }


    [HarmonyPatch(typeof(GameRulePreset), nameof(GameRulePreset.LoadRulesetFromXML))]
    static class GameRulePresetLoadRulesetFromXMLPatch
    {
        static public void Prefix(GameRulePreset __instance)
        {
            Debug.Log("what the futz LoadRulesetFromXML");
            ModdedModSource modsa = new ModdedModSource();
            modsa.moddedMods.Add("ChainPlayers", new ModModMod(false, false, "Chain Players"));
            __instance.mods = modsa;
        }
    }

    class OverlayInfo : TabletButtonEventDispatcher
    {
        public string key;

        public void toggle(bool v)
        {
            Debug.Log("ovi toggle " + key + " => " + v);
            var tablOverlay = gameObject.GetComponent<TabletModalOverlay>();

            ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();
            if (modins.moddedMods.ContainsKey(key))
            {
                modins.moddedMods[key].value = v;
            }
            tablOverlay.dataModel.Set<bool>(key, v);

            TabletModalOverlay.BroadcastRuleChange(TabletRule.ModifierWallSlidesDisabled, 0, 0, v);
            tablOverlay.Close();
            GameObject.Destroy(this);
        }
    }

    [HarmonyPatch(typeof(TabletButton), nameof(TabletButton.OnAccept))]
    static class TabletButtonOnAcceptCtorPatch
    {
        static public void Prefix(TabletButton __instance)
        {
            var avd = __instance.gameObject.GetComponent<TabletButtonEventDispatcherExtended>();
            if (avd != null)
            {
                ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();
                if (modins.moddedMods.ContainsKey(avd.key))
                {
                    var md = modins.moddedMods[avd.key];
                    Debug.Log("Dynamics Stuff " + avd.key);
                    var tbScreen = __instance.GetComponent<TabletButtonEventDispatcher>().tabletScreen;
                    __instance.GetComponent<TabletButtonEventDispatcher>().tabletScreen.OpenModalOverlay(TabletRule.None);
                    var tablOverlay = tbScreen.tablet.modalOverlay;
                    tablOverlay.Initialize(TabletRule.None, new UnityAction(() =>
                    {
                        Debug.Log("close " + avd.key);
                        tbScreen.tablet.rulesScreen.UpdateButtonValue(TabletRule.None, 0, false);
                    }));
                    var ovInfo = tablOverlay.gameObject.GetComponent<OverlayInfo>();

                    if (ovInfo == null)
                    {
                        ovInfo = tablOverlay.gameObject.AddComponent<OverlayInfo>();
                    }

                    ovInfo.key = avd.key;

                    tablOverlay.titleText.text = md.labelText;
                    tablOverlay.onOffContainer.gameObject.SetActive(true);
                    tablOverlay.SetOnOffButtonStyles((bool)md.value);
                    tablOverlay.dataModel.Set<bool>(avd.key, (bool)md.value);
                }
            }

            var ovi = __instance.transform.parent.parent.parent.GetComponent<OverlayInfo>();
            if (ovi && __instance.gameObject.name == "On Button")
            {
                ovi.toggle(true);
            }
            if (ovi && __instance.gameObject.name == "Off Button")
            {
                ovi.toggle(false);
            }

        }
    }

    [HarmonyPatch(typeof(TabletRulesScreen), nameof(TabletRulesScreen.UpdateButtonValue))]
    static class TabletUpdateButtonValuePatch
    {
        static void Postfix(TabletRulesScreen __instance, TabletRule overlayType, int buttonIndex, bool textSizeModifier)
        {
            var con = __instance.tablet.modifiersContainer.gameObject.transform.Find("Border/Modifiers BG/ScrollHolder/ItemContainer");

            Debug.Log("UpdateButtonValue");

            ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();
            foreach (string k in modins.moddedMods.Keys)
            {
                var cl = con.transform.Find(k);
                if (cl != null)
                {
                    cl.transform.Find("ValueMod").GetComponent<TabletTextLabel>().text = Modifiers.GetOnOffValueString((bool)modins.moddedMods[k].value);
                    __instance.SetLineModified(cl.transform.Find("Text Label").GetComponent<TabletTextLabel>(), (bool)modins.moddedMods[k].value == (bool)modins.moddedMods[k].defaultValue);
                }
            }

            Chain.chainPlayersInLobby();
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

            if (ChainedChickenMod.isLocalOrModded())
            {
                clonel.GetComponent<TabletTextLabel>().text = "Modded";

                ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();
                foreach (string k in modins.moddedMods.Keys)
                {
                    TabletStuff.ModifierEntry(con, k, modins.moddedMods[k]);
                }

            }
            else
            {
                clonel.GetComponent<TabletTextLabel>().text = "Mods disabled (use MORE)";
            }
        }
    }
    class TabletButtonEventDispatcherExtended : TabletButtonEventDispatcher
    {
        public string key;
    }

    class TabletStuff
    {
        public static void ModifierEntry(Transform con, string k, ModModMod modModMod)
        {
            var but = con.Find("Dance Invincibility");

            GameObject clone = GameObject.Instantiate(but.gameObject);
            clone.name = k;
            clone.transform.SetParent(con, false);
            var evd = clone.GetComponent<TabletButtonEventDispatcher>();
            evd.overlayType = TabletRule.None;
            var avd = clone.AddComponent<TabletButtonEventDispatcherExtended>();
            avd.key = k;


            var tb = clone.GetComponent<TabletButton>();
            tb.OnClick = null;

            var tlabel = clone.transform.Find("Text Label");
            tlabel.GetComponent<TabletTextLabel>().text = modModMod.labelText;
            tlabel.GetComponent<I2.Loc.Localize>().enabled = false;
            tlabel.GetComponent<LocalizationFontSizeSwitcher>().enabled = false;

            clone.transform.Find("ValueMod").GetComponent<TabletTextLabel>().text = Modifiers.GetOnOffValueString((bool)modModMod.value);
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

                if (msgGameRuleSet.NewRule == TabletRule.ModifierWallSlidesDisabled)
                {
                    //TODO: generalize, send own msg
                    ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();
                    modins.moddedMods["ChainPlayers"].value = msgGameRuleSet.Valueb;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Modifiers), nameof(Modifiers.GetCurrentModifierListString))]
    static class ModifiersGetCurrentModifierListStringPatch
    {
        static void Postfix(Modifiers __instance, ref string __result)
        {
            Debug.Log("GetCurrentModifierListString");

            ModdedModifiers modins = (ModdedModifiers)Modifiers.GetInstance();
            foreach (string k in modins.moddedMods.Keys)
            {
                if ((bool)modins.moddedMods[k].value != (bool)modins.moddedMods[k].defaultValue)
                {
                    __result += "\n[modded] " + modins.moddedMods[k].labelText;
                }
            }
        }
    }
}