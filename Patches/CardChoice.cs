using HarmonyLib;

namespace BetterChat.Patches
{
    [HarmonyPatch(typeof(CardChoice), "DoPlayerSelect")]
    [HarmonyPriority(Priority.First)]
    class CardChoice_Patch_DoPlayerSelect
    {
        static bool Prefix()
        {
            if (BetterChat.isLockingInput) { return false; }
            else { return true; }
        }
    }
}
