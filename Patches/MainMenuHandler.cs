using System;
using HarmonyLib;
using UnboundLib;
using UnityEngine;

namespace BetterChat.Patches
{
    [HarmonyPatch(typeof(MainMenuHandler), "Awake")]
    internal class MainMenuHandlerPatch
    {
        static void Prefix()
        {
            BetterChat.instance.ExecuteAfterSeconds(0.2f, () =>
            {
                BetterChat.instance.HideChat();
                BetterChat.ResetChat();
            });
        }
    }
}