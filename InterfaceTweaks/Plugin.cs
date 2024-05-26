using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        public static void PrintAllComponents(GameObject gameObject)
        {
            // Debug util
            foreach (var c in gameObject.GetComponentsInChildren<object>())
            {
                Log.LogWarning(c.GetType() + ": " + c);
            }
        }

        private static readonly List<Harmony> _harmony = [];

        public static ConfigEntry<bool> showDamagePreview;
        public static ConfigEntry<bool> showUnlockableTraits;
        public static ConfigEntry<bool> highlightUnlockableSpecies;

        private void Awake()
        {
            Log = base.Logger;

            showDamagePreview = Config.Bind("General", "ShowDamagePreview", true,
                    "Shows a preview of damage dealt when attacking (only for basic attacks). White number = shield, Black number = shadow tiles, Blue number = mana, Red number = health");
            showUnlockableTraits = Config.Bind("General.Unlockables", "ShowUnlockableTraits", false,
                    "Whether to show unlockable traits in a tooltip (Top-left of gallery and new game \"GeneticTraits\" on starter selection screen)");
            highlightUnlockableSpecies = Config.Bind("General.Unlockables", "HighlightUnlockableSpecies", false,
                    "Whether to highlight unlockable species (in portals, combats, tavern hires, and character overview)");


            _harmony.Add(Harmony.CreateAndPatchAll(typeof(Plugin)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(DamagePreview)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(ScrollingFixes)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(SelectionMenuSorter)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(SexTrainingFilter)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(UnlockIndicator)));
        }

        private void OnDestroy()
        {
            foreach (var item in _harmony)
            {
                item.UnpatchSelf();
            }
            DamagePreview.OnDestroy();
            SelectionMenuSorter.OnDestroy();
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

        [HarmonyPatch(typeof(InterfaceController), nameof(InterfaceController.showCharacterInfo))]
        [HarmonyPostfix]
        public static void ShowExtraAp(Character c, InterfaceController __instance)
        {
            if (c != null)
            {
                var images = __instance.actionPointRoster.transform.GetComponentsInChildren<Image>();
                SetImageApColors(c.stats.CurrAp, images);
            }
        }

        [HarmonyPatch(typeof(InterfaceController), nameof(InterfaceController.updatePartyCharacter))]
        [HarmonyPostfix]
        public static void ShowExtraApInRoster(int id, InterfaceController __instance)
        {
            for (int j = 0; j < __instance.partyCharacters.Count; j++)
            {
                if (__instance.partyCharacters[j].stats.genetics.id == id)
                {
                    Stats stats = __instance.partyCharacters[j].stats;
                    GameObject partyPanel = __instance.partyCharacters[j].partyPanel;
                    Transform transform = partyPanel.GetComponentsInChildren<GridLayoutGroup>()[0].transform;
                    var images = transform.GetComponentsInChildren<Image>();
                    SetImageApColors(stats.CurrAp, images);
                }
            }
        }

        private static void SetImageApColors(int ap, Image[] images)
        {
            Image ap1Image = images[images.Length - 2];
            Image ap2Image = images[images.Length - 1];
            if (ap >= 5)
            {
                ap1Image.color = new Color(1, 0, 0);
            }
            else if (ap >= 3)
            {
                ap1Image.color = new Color(0, 1, 0);
            }
            if (ap >= 6)
            {
                ap2Image.color = new Color(1, 0, 0);
            }
            else if (ap >= 4)
            {
                ap2Image.color = new Color(0, 1, 0);
            }
        }
    }
}
