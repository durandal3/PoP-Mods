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

        public static ConfigEntry<bool> addSortButtons;
        public static ConfigEntry<bool> allowChangingWorldMod;
        public static ConfigEntry<bool> allowChangingWorldModToAnything;
        public static ConfigEntry<bool> showDamagePreview;
        public static ConfigEntry<bool> showUnlockableTraits;
        public static ConfigEntry<bool> highlightUnlockableSpecies;

        private void Awake()
        {
            Log = base.Logger;

            addSortButtons = Config.Bind("General", "AddSortButtons", true,
                    "Add sort buttons to various lists");
            allowChangingWorldMod = Config.Bind("General", "AllowChangingWorldMod", true,
                    "Allowing changing the World Modifier during new game setup");
            allowChangingWorldModToAnything = Config.Bind("General", "AllowChangingWorldModToAnything", false,
                    "Allows setting the World Modifier to anything, not just those normally available through the Creation ending");
            showDamagePreview = Config.Bind("General.Combat", "ShowDamagePreview", true,
                    "Shows a preview of damage dealt when attacking (only for basic attacks). White number = shield, Black number = shadow tiles, Blue number = mana, Red number = health");
            showUnlockableTraits = Config.Bind("General.Unlockables", "ShowUnlockableTraits", false,
                    "Whether to show unlockable traits in a tooltip (Top-left of gallery and new game \"GeneticTraits\" on starter selection screen)");
            highlightUnlockableSpecies = Config.Bind("General.Unlockables", "HighlightUnlockableSpecies", false,
                    "Whether to highlight unlockable species (in portals, combats, tavern hires, and character overview)");


            _harmony.Add(Harmony.CreateAndPatchAll(typeof(Plugin)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(DamagePreview)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(ExtraApDisplay)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(ScrollingFixes)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(SexTrainingFilter)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(UnlockIndicator)));

            _harmony.Add(Harmony.CreateAndPatchAll(typeof(SelectionMenuSorter)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(McTraitSorter)));
            _harmony.Add(Harmony.CreateAndPatchAll(typeof(StarterTraitSorter)));
        }

        private void OnDestroy()
        {
            foreach (var item in _harmony)
            {
                item.UnpatchSelf();
            }
            DamagePreview.OnDestroy();
            SelectionMenuSorter.OnDestroy();
            McTraitSorter.OnDestroy();
            StarterTraitSorter.OnDestroy();
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


        [HarmonyPatch(typeof(CharacterCreationManager), "Start")]
        [HarmonyPostfix]
        public static void AllowChangingWorldMod(CharacterCreationManager __instance)
        {
            if (!allowChangingWorldMod.Value)
            {
                return;
            }
            if (!__instance.worldModifier.activeSelf)
            {
                return; // Not unlocked TODO also the case when world mod is none...
            }
            if (__instance.worldModifier.GetComponent<Button>() != null)
            {
                return; // Already added
            }
            var button = __instance.worldModifier.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                switch (SaveController.instance.WorldModifier)
                {
                    case PermanentWorldModifier.None:
                    default:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Growth;
                        break;
                    case PermanentWorldModifier.Growth:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Diversity;
                        break;
                    case PermanentWorldModifier.Diversity:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Intelligence;
                        break;
                    case PermanentWorldModifier.Intelligence:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Plentiful;
                        break;
                    case PermanentWorldModifier.Plentiful:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Equality;
                        break;
                    case PermanentWorldModifier.Equality:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Historic;
                        break;
                    case PermanentWorldModifier.Historic:
                        if (allowChangingWorldModToAnything.Value)
                        {
                            SaveController.instance.WorldModifier = PermanentWorldModifier.TwinFusion;
                        }
                        else
                        {
                            SaveController.instance.WorldModifier = PermanentWorldModifier.None;
                        }
                        break;

                    // Not normally available:
                    case PermanentWorldModifier.TwinFusion:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.Empowering;
                        break;
                    case PermanentWorldModifier.Empowering:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.SexBound;
                        break;
                    case PermanentWorldModifier.SexBound:
                        SaveController.instance.WorldModifier = PermanentWorldModifier.None;
                        break;
                    case PermanentWorldModifier.FarmFocus: // Skipping this one - it doesn't do anything
                        SaveController.instance.WorldModifier = PermanentWorldModifier.None;
                        break;
                }

                __instance.worldModifier.GetComponentInChildren<TMP_Text>().text = "WorldModifier (click to change):\n<color=orange>" + SaveController.instance.WorldModifier + "</color>";
                ToolTipManager.instance.hideToolTip();
            });
            __instance.worldModifier.GetComponentInChildren<TMP_Text>().text = "WorldModifier (click to change):\n<color=orange>" + SaveController.instance.WorldModifier + "</color>";
        }
    }
}
