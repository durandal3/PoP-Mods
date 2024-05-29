using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    public class ScrollingFixes
    {

        private static void FixAllChildren(GameObject parent)
        {
            for (int j = 0; j < parent.transform.childCount; j++)
            {
                FixButtonScrolling(parent.transform.GetChild(j).gameObject);
            }
        }

        private static void FixButtonScrolling(GameObject gameObject)
        {
            EventTrigger eventTrigger = gameObject.GetComponentInChildren<EventTrigger>();
            if (eventTrigger == null)
            {
                return;
            }
            // TODO maybe shouldn't do the drags? Will still trigger a click if the same button is under the mouse after the drag.
            AddEvent(eventTrigger, EventTriggerType.BeginDrag, (e) =>
            {
                eventTrigger.gameObject.GetComponentInParent<ScrollRect>()?.SendMessage("OnBeginDrag", e);
            });
            AddEvent(eventTrigger, EventTriggerType.Drag, (e) =>
            {
                eventTrigger.gameObject.GetComponentInParent<ScrollRect>()?.SendMessage("OnDrag", e);
            });
            AddEvent(eventTrigger, EventTriggerType.EndDrag, (e) =>
            {
                eventTrigger.gameObject.GetComponentInParent<ScrollRect>()?.SendMessage("OnEndDrag", e);
            });
            AddEvent(eventTrigger, EventTriggerType.Scroll, (e) =>
            {
                eventTrigger.gameObject.GetComponentInParent<ScrollRect>()?.SendMessage("OnScroll", e);
            });
        }

        private static void AddEvent(EventTrigger eventTrigger, EventTriggerType type, Action<object> action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = type;
            entry.callback.AddListener((e) => action(e));
            eventTrigger.triggers.Add(entry);
        }


        [HarmonyPatch(typeof(CharacterManager), nameof(CharacterManager.showSelectionMenu))]
        [HarmonyPostfix]
        public static void ShowSelectionMenu(CharacterManager __instance)
        {
            FixAllChildren(__instance.genericChoiceRoster);
        }

        [HarmonyPatch(typeof(MaincharacterScreenManager), nameof(MaincharacterScreenManager.updateGeneralContent))]
        [HarmonyPostfix]
        public static void UpdateGeneralContent(MaincharacterScreenManager __instance)
        {
            FixAllChildren(__instance.skillSelectionRoster);
        }

        [HarmonyPatch(typeof(CharacterCreationManager), nameof(CharacterCreationManager.updateMainCharacterTraitRoster))]
        [HarmonyPostfix]
        public static void UpdateMainCharacterTraitRoster(CharacterCreationManager __instance)
        {
            FixAllChildren(__instance.mainCharacterTraitRoster);
        }

        [HarmonyPatch(typeof(CharacterCreationManager), "Start")]
        [HarmonyPostfix]
        public static void Start(CharacterCreationManager __instance)
        {
            FixAllChildren(__instance.characterTraitRoster);
        }


        [HarmonyPatch(typeof(TownInterfaceController), nameof(TownInterfaceController.startGenericSelection))]
        [HarmonyPostfix]
        public static void StartGenericSelection(TownInterfaceController __instance)
        {
            FixAllChildren(__instance.genericSelectionRoster.gameObject);
        }
    }
}
