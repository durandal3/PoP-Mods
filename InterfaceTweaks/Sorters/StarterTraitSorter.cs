using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    public class StarterTraitSorter
    {

        private static List<GeneticTraitType> sortedTraits = [];

        public static void OnDestroy()
        {
            RemoveButtons();
        }

        static void RemoveButtons()
        {
            if (CharacterCreationManager.instance?.characterTraitRoster == null)
            {
                return;
            }
            foreach (var c in GetParentTransform().GetComponentsInChildren<Marker>())
            {
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }

        private static Transform GetParentTransform()
        {
            return CharacterCreationManager.instance.characterTraitRoster.transform.parent.parent.parent;
        }

        private static void DoStarterTraitSort(Util.SortOrder traitOrder)
        {
            if (!sortedTraits.Any())
            {
                sortedTraits = new List<GeneticTraitType>(SaveController.instance.permanentInfo.availableStartingCharacterTraits);
            }
            var roster = CharacterCreationManager.instance.characterTraitRoster.transform;

            Dictionary<GeneticTraitType, Transform> previousButtons = [];
            foreach (var trait in sortedTraits)
            {
                var child = roster.GetChild(0);
                previousButtons.Add(trait, child);
                child.SetParent(null, true);
            }
            switch (traitOrder)
            {
                case Util.SortOrder.ORIGINAL:
                    sortedTraits = new List<GeneticTraitType>(SaveController.instance.permanentInfo.availableStartingCharacterTraits);
                    break;
                case Util.SortOrder.ALPHABETIC:
                    sortedTraits = Enumerable.OrderBy(sortedTraits, (e) => e.ToString()).ToList();
                    break;
                case Util.SortOrder.COST:
                    sortedTraits = Enumerable.OrderBy(sortedTraits, CharacterCreationManager.getPointCostForTrait).ToList();
                    break;
            }

            int index = 0;
            foreach (var trait in sortedTraits)
            {
                var child = previousButtons[trait];
                child.SetParent(roster, true);
                child.GetComponent<Button>().onClick.RemoveAllListeners();
                var num = index;
                child.GetComponent<Button>().onClick.AddListener(delegate
                {
                    CharacterCreationManager.instance.selectStarterTrait(num);
                });
                index++;
            }
        }

        [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.showTab))]
        [HarmonyPrefix]
        public static void CreateButtons()
        {
            var parent = GetParentTransform();
            if (parent.GetComponentInChildren<Marker>() == null)
            {
                if (Plugin.addSortButtons.Value)
                {
                    var traitLabel = parent.GetChild(12);

                    Util.ChangePosition(traitLabel, -100, 0);
                    var p = traitLabel.localPosition;
                    Util.MakeSortButtons(typeof(Marker), parent, p.x + 150, p.y, DoStarterTraitSort, [
                        Util.SortOrder.ORIGINAL,
                        Util.SortOrder.ALPHABETIC,
                        Util.SortOrder.COST,
                    ]);
                }
                sortedTraits = new List<GeneticTraitType>(SaveController.instance.permanentInfo.availableStartingCharacterTraits);
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.showTab))]
        [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.selectStarterTrait))]
        public static IEnumerable<CodeInstruction> UpdateStarterTraitReference(IEnumerable<CodeInstruction> instructions)
        {
            // Change CharacterCreationManager.showTab/selectStarterTrait to get the trait list from this sortedTraits field
            // instead of the base list in SaveController

            var ldsInstance = new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(SaveController), nameof(SaveController.instance)));
            var ldInfo = new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SaveController), nameof(SaveController.instance.permanentInfo)));
            var ldTraits = new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PermanentInfo), nameof(PermanentInfo.availableStartingCharacterTraits)));
            var sortedTraitsField = AccessTools.Field(typeof(StarterTraitSorter), nameof(sortedTraits));
            return new CodeMatcher(instructions)
                    .MatchForward(false, ldsInstance, ldInfo, ldTraits)
                    .Repeat(matcher => matcher
                            .SetOperandAndAdvance(sortedTraitsField)
                            .RemoveInstruction()
                            .RemoveInstruction()
                    )
                    .InstructionEnumeration();
        }

        public class Marker : MonoBehaviour { }
    }
}
