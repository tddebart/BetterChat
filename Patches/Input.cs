using System;
using HarmonyLib;
using UnityEngine;

namespace BetterChat.Patches
{
    [HarmonyPatch(typeof(Input),"GetKeyDown", new Type[] {typeof(KeyCode)})]
    class Patch_Input 
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix(Input __instance, KeyCode key)
        {
            if (BetterChat.isLockingInput && key != KeyCode.Return)
            {
                return false;
            }

            return true;
        }
    }
}