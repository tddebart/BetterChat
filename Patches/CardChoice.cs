using HarmonyLib;

namespace BetterChat.Patches
{
    [HarmonyPatch(typeof(CardChoice), "DoPlayerSelect")]
    [HarmonyPriority(Priority.First)]
    class CardChoice_Patch_DoPlayerSelect
    {
        static bool Prefix()
        {
            return !BetterChat.isLockingInput;
        }
    }
}
