using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using GameEvent;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace ChainedChickenMod.Patches
{
    public class CustomModdedModifiers
    {
        public static short msgNum = (short)(NetMsgTypes.msgCount + 9124);
        public static Dictionary<String, ModModMod> moddedMods = new Dictionary<String, ModModMod>();

        public static Dictionary<String, ModModMod> ModdedMods
        {
            get
            {
                Dictionary<String, ModModMod> newModdedMods = new Dictionary<String, ModModMod>();
                foreach (KeyValuePair<string, ModModMod> kv in moddedMods)
                {
                    newModdedMods.Add(kv.Key, kv.Value.Clone());
                }

                return newModdedMods;
            }
        }

        public static void BroadcastRuleChange(string key, ModModMod mod)
        {
            ModdedMsgGameRuleSet msgGameRuleSet = new ModdedMsgGameRuleSet();
            msgGameRuleSet.key = key;
            msgGameRuleSet.mod = mod;
            LobbyManager.instance.client.Send(msgNum, msgGameRuleSet);
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.Connect))]
        static class LobbyManagerConnectPatch
        {
            static public void Postfix(LobbyManager __instance)
            {
                NetworkServer.RegisterHandler(msgNum, new NetworkMessageDelegate(__instance.distributeServerMessage));
                if (__instance.client != null)
                {
                    __instance.client.RegisterHandler(msgNum, new NetworkMessageDelegate(__instance.distributeMessage));
                }
            }
        }

        [HarmonyPatch(typeof(LobbyManager), nameof(LobbyManager.readMessage))]
        static class LobbyManagerReadMessagePatch
        {
            static public bool Prefix(LobbyManager __instance, NetworkMessage msg, ref MessageBase __result)
            {
                if (msg.msgType == msgNum)
                {
                    __result = msg.ReadMessage<ModdedMsgGameRuleSet>();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(TabletRulesScreen), nameof(TabletRulesScreen.handleEvent))]
        static class TabletRulesScreenHandleEventPatch
        {
            static void Prefix(TabletRulesScreen __instance, GameEvent.GameEvent e)
            {
                GameEvent.NetworkMessageReceivedEvent netw = e as GameEvent.NetworkMessageReceivedEvent;

                if (netw.Message.msgType == msgNum)
                {
                    ModdedMsgGameRuleSet msgGameRuleSet = (ModdedMsgGameRuleSet)netw.ReadMessage;

                    ModdedModifiers modins = ModdedModifiers.GetWinstance();
                    modins.moddedMods[msgGameRuleSet.key] = msgGameRuleSet.mod;

                    modins.OnModifiersDynamicChange();
                    __instance.UpdateButtonValue(msgGameRuleSet.NewRule, 0, false);
                }
            }
        }
    }

    public class ModdedMsgGameRuleSet : MsgGameRuleSet
    {
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(key);
            json = JsonConvert.SerializeObject(mod);
            writer.Write(json);
        }

        public override void Deserialize(NetworkReader reader)
        {
            key = reader.ReadString();
            json = reader.ReadString();
            mod = JsonConvert.DeserializeObject<ModModMod>(json);
            if (mod.value is Int64)
            {
                mod.value = Convert.ToInt32(mod.value);
                mod.defaultValue = Convert.ToInt32(mod.defaultValue);
                mod.possibleValues = mod.possibleValues.Select(item => (object)Convert.ToInt32(item)).ToArray();
            }
        }

        public string key;
        public string json;
        public ModModMod mod;
    }

    public class ModdedModifiers : Modifiers
    {
        static public bool modded = true;

        public Dictionary<String, ModModMod> moddedMods = new Dictionary<String, ModModMod>();
        private static ModdedModifiers winstance;


        public ModdedModifiers()
        {
            moddedMods = CustomModdedModifiers.ModdedMods;
        }

        [HarmonyPatch(typeof(Modifiers), "get_DefaultModSource")]
        static class ModifiersGetDefaultModSourcePatch
        {
            static public bool Prefix(out ModSource __result)
            {
                if (Modifiers.defaultModSource == null)
                {
                    Modifiers.defaultModSource = new ModdedModSource();
                }
                __result = Modifiers.defaultModSource;
                return false;
            }
        }


        static public ModdedModifiers GetWinstance()
        {
            if (ModdedModifiers.winstance == null)
            {

                ModdedModifiers.winstance = ScriptableObject.CreateInstance<ModdedModifiers>();

            }

            return ModdedModifiers.winstance;
        }
    }

    public class ModModMod
    {
        public object value;
        public object defaultValue;
        public object[] possibleValues;
        public string labelText;

        public ModModMod(object value, object defaultValue, string labelText, object[] possibleValues = null)
        {
            this.value = value;
            this.defaultValue = defaultValue;
            this.labelText = labelText;
            this.possibleValues = possibleValues;
        }

        internal ModModMod Clone()
        {
            return new ModModMod(value, defaultValue, labelText, possibleValues);
        }

        public string GetTextValue()
        {
            string tval = "???";

            switch (value)
            {
                case bool val:
                    tval = Modifiers.GetOnOffValueString(val);
                    break;
                default:
                    tval = value.ToString();
                    break;
            }

            return tval;
        }

        public bool IsDefault()
        {
            switch (value)
            {
                case bool val:
                    return val == (bool)defaultValue;
                case int val:
                    return val == (int)defaultValue;
            }
            throw new Exception("ModdedModifiers can't handle " + value.GetType());

        }
    }

    public class ModdedModSource : ModSource
    {
        static public bool modded = true;

        public Dictionary<String, ModModMod> moddedMods = new Dictionary<String, ModModMod>();


        public ModdedModSource()
        {
            moddedMods = CustomModdedModifiers.ModdedMods;
        }

        public ModdedModSource(ModSource modsIn)
        {
            var doc = new XmlDocument();
            XmlElement el = doc.CreateElement("mods");

            modsIn.WriteToXmlNode(doc, el);
            this.ReadFromXmlNode(el);

            moddedMods = CustomModdedModifiers.ModdedMods;
        }

        public new void ReadFromModSettings()
        {
            base.ReadFromModSettings();
            ModdedModifiers mos = ModdedModifiers.GetWinstance();

            foreach (string k in mos.moddedMods.Keys)
            {
                if (!moddedMods.ContainsKey(k))
                {
                    moddedMods.Add(k, mos.moddedMods[k].Clone());
                }
                else
                {
                    moddedMods[k] = mos.moddedMods[k].Clone();
                }
            }
        }

        [HarmonyPatch(typeof(ModSource), nameof(ModSource.ReadFromModSettings))]
        static class ModSourceReadFromModSettingsPatch
        {
            static public void Prefix(ModSource __instance)
            {
                if (__instance.GetType() == typeof(ModdedModSource))
                {
                    var ds = (ModdedModSource)__instance;
                    ModdedModifiers mos = ModdedModifiers.GetWinstance();

                    foreach (string k in mos.moddedMods.Keys)
                    {
                        if (!ds.moddedMods.ContainsKey(k))
                        {
                            ds.moddedMods.Add(k, mos.moddedMods[k].Clone());
                        }
                        else
                        {
                            ds.moddedMods[k] = mos.moddedMods[k].Clone();
                        }

                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.ReadAllSettings))]
        static class GameSettingsReadAllSettingsPatch
        {
            static public void Prefix(GameSettings __instance, GameRulePreset otherPreset)
            {
                if (otherPreset.mods.GetType() != typeof(ModdedModSource))
                {
                    otherPreset.mods = new ModdedModSource(otherPreset.mods);
                }
            }
        }

        [HarmonyPatch(typeof(ModSource), nameof(ModSource.WriteToModSettings))]
        static class ModSourceWriteToModSettingsPatch
        {
            static public void Postfix(ModSource __instance, bool includeTreehouseSettings)
            {
                var mods = __instance;
                ModdedModifiers modins = ModdedModifiers.GetWinstance();

                if (mods.GetType() != typeof(ModdedModSource))
                {
                    //Write defaults
                    modins.moddedMods = CustomModdedModifiers.ModdedMods;
                    return;
                }

                ModdedModSource mos = (ModdedModSource)__instance;

                foreach (string k in mos.moddedMods.Keys)
                {
                    if (!modins.moddedMods.ContainsKey(k))
                    {
                        modins.moddedMods.Add(k, mos.moddedMods[k].Clone());
                    }
                    else
                    {
                        modins.moddedMods[k] = mos.moddedMods[k].Clone();
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

                if (__instance.GetType() != typeof(ModdedModSource))
                {
                    return;
                }
                ModdedModSource mos = (ModdedModSource)__instance;
                ModdedModifiers modins = ModdedModifiers.GetWinstance();
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
                if (!__result)
                {
                    __result = false;
                }
                ModdedModSource mos = (ModdedModSource)__instance;

                foreach (string k in mos.moddedMods.Keys)
                {
                    if (!((ModdedModSource)other).moddedMods.ContainsKey(k) || mos.moddedMods[k].value != ((ModdedModSource)other).moddedMods[k].value)
                    {
                        __result = false;
                    }
                    break;
                }

                __result = true;
            }
        }


        [HarmonyPatch(typeof(GameRulePreset), nameof(GameRulePreset.LoadRulesFromSettings))]
        static class GameRulePresetLoadRulesFromSettingsPatch
        {
            static public void Prefix(GameRulePreset __instance)
            {
                if (__instance.mods.GetType() != typeof(ModdedModSource))
                {
                    __instance.mods = new ModdedModSource();
                }

                ModdedModSource mos = (ModdedModSource)__instance.mods;

                ModdedModifiers instanceModdedModifiers = ModdedModifiers.GetWinstance();
                foreach (string k in instanceModdedModifiers.moddedMods.Keys)
                {
                    if (!mos.moddedMods.ContainsKey(k))
                    {
                        mos.moddedMods.Add(k, instanceModdedModifiers.moddedMods[k].Clone());
                    }
                    else
                    {
                        mos.moddedMods[k] = instanceModdedModifiers.moddedMods[k].Clone();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ModSource), nameof(ModSource.WriteToXmlNode))]
        static class ModSourceWriteToXmlNodeNodePatch
        {
            static public void Prefix(ModSource __instance, XmlDocument doc, XmlElement modsNode)
            {
                if (__instance.GetType() == typeof(ModdedModSource))
                {
                    ModdedModSource mos = (ModdedModSource)__instance;
                    foreach (string k in mos.moddedMods.Keys)
                    {
                        QuickSaver.AddAttribute(doc, modsNode, k, mos.moddedMods[k].value.ToString());
                    }
                }
            }
        }


        [HarmonyPatch(typeof(ModSource), nameof(ModSource.ReadFromXmlNode))]
        static class ModSourceReadFromXmlNodePatch
        {
            static public void Prefix(ModSource __instance, XmlNode child)
            {
                ModdedModSource mos = (ModdedModSource)__instance;
                foreach (string k in mos.moddedMods.Keys)
                {
                    var defaultValue = mos.moddedMods[k].defaultValue;
                    switch (defaultValue)
                    {
                        case bool val:
                            mos.moddedMods[k].value = QuickSaver.ParseAttrBool(child, k, val);
                            break;
                        case int val:
                            mos.moddedMods[k].value = QuickSaver.ParseAttrInt(child, k, val);
                            break;
                    }
                }
            }
        }

        [HarmonyPatch]
        static class SwitchModSourcePatch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(QuickSaver), nameof(QuickSaver.GetCurrentXmlSnapshot));
                yield return AccessTools.Method(typeof(QuickSaver), nameof(QuickSaver.LoadSnapshotFromXmlDocument));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i < code.Count - 1; i++) // -1 since we will be checking i + 1
                {
                    var constructorInfo = typeof(ModSource).GetConstructor(new Type[0]);

                    if (code[i].opcode == OpCodes.Newobj && (ConstructorInfo)code[i].operand == constructorInfo)
                    {
                        Debug.Log(i + " code " + constructorInfo);
                        var constructorInfo2 = typeof(ModdedModSource).GetConstructor(new Type[0]);
                        code[i] = new CodeInstruction(OpCodes.Newobj, constructorInfo2);
                        break;
                    }
                }

                return code;
            }
        }

    }


    [HarmonyPatch(typeof(GameRulePreset), nameof(GameRulePreset.LoadRulesetFromXML))]
    static class GameRulePresetLoadRulesetFromXMLPatch
    {
        static public void Prefix(GameRulePreset __instance)
        {
            ModdedModSource modsa = new ModdedModSource();
            __instance.mods = modsa;
        }
    }

    class OverlayInfo : TabletButtonEventDispatcher
    {
        public string key;
        public ModModMod mod;
        public TabletModalOverlay tablOverlay;

        public void SetResult(object v)
        {
            Debug.Log("ovi set int " + key + " => " + v + " (" + v.GetType() + ") ");

            ModdedModifiers modins = ModdedModifiers.GetWinstance();
            if (modins.moddedMods.ContainsKey(key))
            {
                modins.moddedMods[key].value = v;
            }
            //tablOverlay.dataModel.Set(key, v);

            CustomModdedModifiers.BroadcastRuleChange(key, modins.moddedMods[key]);
            GameEventManager.SendEvent(new ModifiersChangedEvent(TabletRule.None));
            tablOverlay.rulesScreen.MarkRulesDirty(true);

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
                ModdedModifiers modins = ModdedModifiers.GetWinstance();
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
                    ovInfo.mod = avd.mod;
                    ovInfo.tablOverlay = tablOverlay;

                    tablOverlay.titleText.text = md.labelText;

                    if (md.value is bool)
                    {
                        tablOverlay.onOffContainer.gameObject.SetActive(true);
                        tablOverlay.SetOnOffButtonStyles(true);
                    }
                    else if (md.value is int)
                    {
                        tablOverlay.plusMinusContainer.gameObject.SetActive(true);
                        tablOverlay.okButtonContainer.gameObject.SetActive(true);
                        tablOverlay.plusMinusLabel.text = md.GetTextValue();
                    }
                    else
                    {
                        Debug.LogWarning("ModdedModifiers dialog not available for " + md.value.GetType());
                    }

                    //tablOverlay.dataModel.Set(avd.key, md.value);
                }
            }

            var ovi = __instance.transform.parent?.parent?.parent?.GetComponent<OverlayInfo>();
            if (ovi == null)
            {
                return;
            }
            if (__instance.gameObject.name == "On Button")
            {
                ovi.SetResult(true);
            }
            if (__instance.gameObject.name == "Off Button")
            {
                ovi.SetResult(false);
            }
            if (__instance.gameObject.name == "OK Button")
            {
                ovi.SetResult(ovi.mod.value);
            }
            if (ovi.mod.value is int && __instance.gameObject.name == "Minus Button")
            {
                int n = (int)ovi.mod.value - 1;
                if (ovi.mod.possibleValues.Contains(n))
                {
                    ovi.mod.value = n;
                }
                ovi.tablOverlay.plusMinusLabel.text = ovi.mod.GetTextValue();
            }
            if (ovi.mod.value is int && __instance.gameObject.name == "Plus Button")
            {
                int n = (int)ovi.mod.value + 1;
                if (ovi.mod.possibleValues.Contains(n))
                {
                    ovi.mod.value = n;
                }
                ovi.tablOverlay.plusMinusLabel.text = ovi.mod.GetTextValue();
            }
        }
    }

    [HarmonyPatch(typeof(TabletRulesScreen), nameof(TabletRulesScreen.UpdateButtonValue))]
    static class TabletUpdateButtonValuePatch
    {
        static void Postfix(TabletRulesScreen __instance, TabletRule overlayType, int buttonIndex, bool textSizeModifier)
        {
            var con = __instance.tablet.modifiersContainer.gameObject.transform.Find("Border/Modifiers BG/ScrollHolder/ItemContainer");

            ModdedModifiers modins = ModdedModifiers.GetWinstance();
            foreach (string k in modins.moddedMods.Keys)
            {
                var cl = con.transform.Find(k);
                ModModMod mod = modins.moddedMods[k];

                if (cl != null)
                {
                    __instance.SetLineModified(cl.transform.Find("Text Label").GetComponent<TabletTextLabel>(), mod.IsDefault());
                    cl.transform.Find("ValueMod").GetComponent<TabletTextLabel>().text = mod.GetTextValue();
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

                ModdedModifiers modins = ModdedModifiers.GetWinstance();
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
        public ModModMod mod;
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
            avd.mod = modModMod;


            var tb = clone.GetComponent<TabletButton>();
            tb.OnClick = null;

            var tlabel = clone.transform.Find("Text Label");
            tlabel.GetComponent<TabletTextLabel>().text = modModMod.labelText;
            tlabel.GetComponent<I2.Loc.Localize>().enabled = false;
            tlabel.GetComponent<LocalizationFontSizeSwitcher>().enabled = false;

            clone.transform.Find("ValueMod").GetComponent<TabletTextLabel>().text = modModMod.GetTextValue();
        }

    }

    [HarmonyPatch(typeof(Modifiers), nameof(Modifiers.GetCurrentModifierListString))]
    static class ModifiersGetCurrentModifierListStringPatch
    {
        static void Postfix(Modifiers __instance, bool forceModsApplied, ref string __result)
        {
            ModdedModifiers modins = ModdedModifiers.GetWinstance();
            foreach (string k in modins.moddedMods.Keys)
            {
                if (!modins.moddedMods[k].IsDefault())
                {
                    __result += "\n[modded] " + modins.moddedMods[k].labelText + ": " + modins.moddedMods[k].GetTextValue();
                }
            }
        }
    }
}