using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    public class UnlockIndicator
    {

        static void OnDestroy()
        {
        }

        static bool HasUnlockableTrait(List<GeneticTrait> traits)
        {
            foreach (var trait in traits)
            {
                if (!trait.unlocked && trait.unlockInfo != "Can't be unlocked.")
                {
                    return true;
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(OWParty), nameof(OWParty.act2))]
        [HarmonyPostfix]
        public static void RefreshAura(OWParty __instance)
        {
            __instance.updateAura();
        }

        [HarmonyPatch(typeof(OWParty), nameof(OWParty.updateAura))]
        [HarmonyPrefix]
        public static void UpdateAura(OWParty __instance, ref bool __runOriginal)
        {
            bool hasUnlocks = false;
            foreach (var character in __instance.info.characters)
            {
                if (!SaveController.instance.hasSpeciesInGallery(character.species))
                {
                    hasUnlocks = true;
                    break;
                }
            }

            if (hasUnlocks)
            {
                __instance.sprites[1].color = new(0.8f, 0f, 0.8f, 0.6f);
                __runOriginal = false;
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.setUI))]
        [HarmonyPostfix]
        public static void UpdateBattleAura(Character __instance)
        {
            if (__instance.IsPlayerChar)
            {
                return;
            }

            var renderer = __instance.GetComponentsInChildren<SpriteRenderer>()[1];
            if (!SaveController.instance.hasSpeciesInGallery(__instance.stats.species))
            {
                renderer.sprite = TileHighlighter.instance.TileHighlightPrefab.GetComponent<SpriteRenderer>().sprite;
                renderer.color = new(0.8f, 0f, 0.8f, 0.6f);
            }
            else if (HasUnlockableTrait(__instance.stats.GeneticTraits))
            {
                renderer.sprite = TileHighlighter.instance.TileHighlightPrefab.GetComponent<SpriteRenderer>().sprite;
                renderer.color = new(0f, 0.8f, 0.8f, 1.0f);
            }
        }

        [HarmonyPatch(typeof(TownInterfaceController), nameof(TownInterfaceController.showNoticeBoardTab))]
        [HarmonyPostfix]
        public static void ShowNoticeBoardTab(int index, TownInterfaceController __instance)
        {
            // Highlight characters in the tavern that have unlockables
            if (index == 8)
            {
                int count = __instance.hireCharacterRoster.transform.childCount;
                int charCount = SaveController.instance.tavernCharacterOffers.Count;
                for (int m = 0; m < charCount; m++)
                {
                    var panel = __instance.hireCharacterRoster.transform.GetChild(count - charCount + m).gameObject;
                    var image = panel.GetComponentsInChildren<Image>()[1];
                    Stats s = SaveController.instance.tavernCharacterOffers[m].stats;

                    if (!SaveController.instance.hasSpeciesInGallery(s.species))
                    {
                        image.color = new(0.8f, 0f, 0.8f, 1.0f);
                    }
                    else if (HasUnlockableTrait(s.GeneticTraits))
                    {
                        image.color = new(0f, 0.8f, 0.8f, 1.0f);
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(QuickCharacterInfo), nameof(QuickCharacterInfo.setFilteredInfo))]
        [HarmonyPostfix]
        public static void SetFilteredInfo(int v, QuickCharacterInfo __instance)
        {
            if (v != 0)
            {
                return;
            }

            if (!SaveController.instance.hasSpeciesInGallery(__instance.character.species))
            {
                __instance.GetComponentInChildren<TMP_Text>().text = "<color=green>S</color> " + __instance.GetComponentInChildren<TMP_Text>().text;
            }
            else if (HasUnlockableTrait(__instance.character.GeneticTraits))
            {
                __instance.GetComponentInChildren<TMP_Text>().text = "<color=green>T</color> " + __instance.GetComponentInChildren<TMP_Text>().text;
            }
        }
    }
}
