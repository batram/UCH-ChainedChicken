using System.Xml;
using GameEvent;
using HarmonyLib;
using UnityEngine;

namespace ChainedChickenMod.Patches
{

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
                tablOverlay.SetOnOffButtonStyles(ChainedChickenMod.ChainedModifierEnabled);
                tablOverlay.dataModel.Set<bool>("ChainPlayers", ChainedChickenMod.ChainedModifierEnabled);
            }
            if (__instance.gameObject.name == "On Button")
            {
                var group = __instance.transform.parent.parent;
                if (group.Find("Subtitle").GetComponent<TabletTextLabel>().text == "Chain Players Together")
                {
                    Debug.Log("ChainPlayers on");
                    var tablOverlay = group.parent.GetComponent<TabletModalOverlay>();

                    ChainedChickenMod.ChainedModifierEnabled = true;
                    tablOverlay.dataModel.Set<bool>("ChainPlayers", ChainedChickenMod.ChainedModifierEnabled);
                    tablOverlay.rulesScreen.MarkRulesDirty();

                    TabletModalOverlay.BroadcastRuleChange(TabletRule.ModifierWallSlidesDisabled, 0, 0, ChainedChickenMod.ChainedModifierEnabled);
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

                    ChainedChickenMod.ChainedModifierEnabled = false;
                    tablOverlay.dataModel.Set<bool>("ChainPlayers", ChainedChickenMod.ChainedModifierEnabled);

                    TabletModalOverlay.BroadcastRuleChange(TabletRule.ModifierWallSlidesDisabled, 0, 0, ChainedChickenMod.ChainedModifierEnabled);
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
                if(ChainedChickenMod.ChainModifiersEntry != null)
                {
                    ChainedChickenMod.ChainModifiersEntry.transform.Find("ValueMod").GetComponent<TabletTextLabel>().text = Modifiers.GetOnOffValueString(ChainedChickenMod.ChainedModifierEnabled);
                    __instance.SetLineModified(ChainedChickenMod.ChainModifiersEntry.transform.Find("Text Label").GetComponent<TabletTextLabel>(), !ChainedChickenMod.ChainedModifierEnabled);
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

            if (ChainedChickenMod.isLocalOrModded())
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

                ChainedChickenMod.ChainModifiersEntry = clone;
                ChainedChickenMod.ChainModifiersEntry.transform.Find("ValueMod").GetComponent<TabletTextLabel>().text = Modifiers.GetOnOffValueString(ChainedChickenMod.ChainedModifierEnabled);
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
                    ChainedChickenMod.ChainedModifierEnabled = msgGameRuleSet.Valueb;
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
            if (ChainedChickenMod.ChainedModifierEnabled)
            {
                __result += "\n[modded] Chain Players Together ";
            }
        }
    }
    
    [HarmonyPatch(typeof(ModSource), nameof(ModSource.ReadFromXmlNode))]
    static class ModSourceFromXMLPatch
    {
        static void Postfix(ModSource __instance, XmlNode child)
        {
            Debug.Log("Futz ReadFromXmlNode");
            bool ogVal = ChainedChickenMod.ChainedModifierEnabled;
            ChainedChickenMod.ChainedModifierEnabled = QuickSaver.ParseAttrBool(child, "ChainPlayers", false);
            if (ChainedChickenMod.ChainedModifierEnabled && ogVal != ChainedChickenMod.ChainedModifierEnabled)
            {
                GameEventManager.SendEvent(new ModifiersChangedEvent(TabletRule.None));
            }
            if (ChainedChickenMod.ChainModifiersEntry != null)
            {
                ChainedChickenMod.ChainModifiersEntry.transform.Find("ValueMod").GetComponent<TabletTextLabel>().text = Modifiers.GetOnOffValueString(ChainedChickenMod.ChainedModifierEnabled);
            }
        }
    }

    [HarmonyPatch(typeof(ModSource), nameof(ModSource.WriteToXmlNode))]
    static class ModSourceWriteToXmlNodePatch
    {
        static void Postfix(ModSource __instance, XmlDocument doc, XmlElement modsNode)
        {
            Debug.Log("Futz WriteToXmlNode");
            QuickSaver.AddAttribute(doc, modsNode, "ChainPlayers", ChainedChickenMod.ChainedModifierEnabled.ToString());

        }
    }
}