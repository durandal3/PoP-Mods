using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace InterfaceTweaks
{
    public class McTraitSorter
    {

        private static List<MainCharacterTrait> mcTraits = new List<MainCharacterTrait>();

        public static void OnDestroy()
        {
            RemoveButtons();
        }

        static void RemoveButtons()
        {
            if (CharacterCreationManager.instance?.mainCharacterTraitRoster == null)
            {
                return;
            }
            foreach (var c in getParentTransform().GetComponentsInChildren<Util.Marker>())
            {
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }

        private static Transform getParentTransform()
        {
            return CharacterCreationManager.instance.mainCharacterTraitRoster.transform.parent.parent.parent;
        }

        private static void DoMcTraitSort(Util.SortOrder mcTraitOrder)
        {
            if (!mcTraits.Any())
            {
                mcTraits = new List<MainCharacterTrait>(SaveController.instance.permanentInfo.availableMainCharacterTraits);
            }
            switch (mcTraitOrder)
            {
                case Util.SortOrder.ORIGINAL:
                    mcTraits = new List<MainCharacterTrait>(SaveController.instance.permanentInfo.availableMainCharacterTraits);
                    break;
                case Util.SortOrder.ALPHABETIC:
                    mcTraits = Enumerable.OrderBy(mcTraits, (e) => e.ToString()).ToList();
                    break;
                case Util.SortOrder.RARITY:
                    FieldInfo bonusTraitsField = typeof(CharacterCreationManager).GetField("bonusTraits", BindingFlags.NonPublic | BindingFlags.Instance);
                    var bonusTraits = (List<MainCharacterTrait>)bonusTraitsField.GetValue(CharacterCreationManager.instance);
                    mcTraits = Enumerable.OrderBy(mcTraits, (e) => bonusTraits.Contains(e) ? 0 : 1).ToList();
                    break;
                case Util.SortOrder.COST:
                    mcTraits = Enumerable.OrderBy(mcTraits, (e) => CharacterCreationManager.instance.getCostForMcTrait(e)).ToList();
                    break;
            }
            CharacterCreationManager.instance.updateMainCharacterTraitRoster();
        }

        [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.updateMainCharacterTraitRoster))]
        [HarmonyPostfix]
        public static void UpdateMainCharacterTraitRoster()
        {
            if (!Plugin.addSortButtons.Value)
            {
                return;
            }
            var parent = getParentTransform();
            if (parent.GetComponentInChildren<Util.Marker>() == null)
            {
                var traitLabel = parent.GetChild(9);
                var professionPanel = parent.GetChild(6);
                Util.ChangePosition(traitLabel, -20, 0);
                Util.ChangePosition(professionPanel, 0, 10);
                var rect = professionPanel.GetComponentInChildren<RectTransform>();
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y - 20);


                var p = traitLabel.localPosition;
                Util.MakeSortButtons(parent, p.x + 70, p.y + 30, DoMcTraitSort, [
                    Util.SortOrder.ORIGINAL,
                        Util.SortOrder.ALPHABETIC,
                        Util.SortOrder.RARITY,
                        Util.SortOrder.COST,
                    ]);
                mcTraits = new List<MainCharacterTrait>(SaveController.instance.permanentInfo.availableMainCharacterTraits);
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.updateMainCharacterTraitRoster))]
        public static IEnumerable<CodeInstruction> UpdateMainCharacterTraitRosterTranspile(IEnumerable<CodeInstruction> instructions)
        {
            // Change CharacterCreationManager.updateMainCharacterTraitRoster to get the trait list from this mcTraits field
            // instead of the base list in SaveController

            var ldsInstance = new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(SaveController), nameof(SaveController.instance)));
            var ldInfo = new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(SaveController), nameof(SaveController.instance.permanentInfo)));
            var ldTraits = new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PermanentInfo), nameof(PermanentInfo.availableMainCharacterTraits)));
            var mcTraitsField = AccessTools.Field(typeof(McTraitSorter), nameof(mcTraits));
            return new CodeMatcher(instructions)
                    .MatchForward(false, ldsInstance, ldInfo, ldTraits)
                    .Repeat(matcher => matcher
                            .SetOperandAndAdvance(mcTraitsField)
                            .RemoveInstruction()
                            .RemoveInstruction()
                    )
                    .InstructionEnumeration();
        }
    }
}
