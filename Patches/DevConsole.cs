using HarmonyLib;
using Photon.Pun;
using UnboundLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterChat.Patches
{
    [HarmonyPatch(typeof(DevConsole),"Update")]
    class Patch_Update_dev
    {
        // ReSharper disable once UnusedMember.Local
        private static bool Prefix()
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Return))
            {
                return true;
            }
            if (Input.GetKeyDown(KeyCode.Return) && PhotonNetwork.IsConnected)
            {
                BetterChat.inputField.Select();
                BetterChat.inputField.ActivateInputField();
                BetterChat.inputField.OnSelect(new BaseEventData(EventSystem.current));
            }

            return false;
        }
    }
}