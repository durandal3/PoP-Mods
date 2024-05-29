using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace InterfaceTweaks
{
    public class ExtraApDisplay
    {


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
