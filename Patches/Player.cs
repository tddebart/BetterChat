using HarmonyLib;
using UnityEngine;

namespace BetterChat.Patches
{
    [HarmonyPatch(typeof(Player),"Start")]
    class Patch_PlayerStart
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(Player __instance)
        {
            __instance.transform.Find("WobbleObjects/Canvas_Chat").GetComponent<Canvas>().enabled = false;
            var indicator = GameObject.Instantiate(BetterChat.typingIndicatorObj, __instance.transform.Find("WobbleObjects"));
            indicator.SetActive(false);
        }
    }
}