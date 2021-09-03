using HarmonyLib;
using UnityEngine;

namespace BetterChat.Patches
{
    [HarmonyPatch(typeof(MenuControllerHandler),"Start")]
    class Patch_HandlerStart
    {
        // ReSharper disable once UnusedMember.Local
        private static void Postfix(MenuControllerHandler __instance)
        {
            if(__instance == MenuControllerHandler.instance)
            {
                __instance.gameObject.AddComponent<ChatMonoGameManager>();
            }
        }
    }
}