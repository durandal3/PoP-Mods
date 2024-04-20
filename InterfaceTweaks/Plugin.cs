using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;

namespace InterfaceTweaks
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private static List<Harmony> _harmony = [];

        private void Awake()
        {
            Log = base.Logger;
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(Plugin)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(SexTrainingFilter)));
        }

        private void OnDestroy()
        {
            foreach (var item in _harmony)
            {
                item.UnpatchSelf();
            }
        }


        [HarmonyPatch(typeof(ToolTipManager), nameof(ToolTipManager.showTooltip))]
        [HarmonyPrefix]
        public static void MakeTavernCharTooltipsEasierToGetTo(ref bool __runOriginal)
        {
            // Don't show other tooltips while the character tooltip is open and on the tavern hire screen (and not on the character tooltip)
            if (ToolTipManager.instance.characterTooltipOpen &&
                    TownInterfaceController.instance != null &&
                    TownInterfaceController.instance.hireCharacterRoster.activeInHierarchy &&
                    ToolTipManager.instance.inactivityTimerStarted)
            {
                __runOriginal = false;
            }
        }

        [HarmonyPatch(typeof(QuickCharacterInfo), nameof(QuickCharacterInfo.setFilteredInfo))]
        [HarmonyPostfix]
        public static void AddInjuryToName(int v, QuickCharacterInfo __instance)
        {
            if (v != 0)
            {
                return;
            }

            if (__instance.character.isInjured)
            {
                __instance.GetComponentInChildren<TMP_Text>().text = "<color=red>(I)</color> " + __instance.GetComponentInChildren<TMP_Text>().text;
            }
        }
    }
}
