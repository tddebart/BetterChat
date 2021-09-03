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
                if (!BetterChat.clearMessageOnEnter.Value)
                {
                    BetterChat.inputField.selectionAnchorPosition = 0;
                    BetterChat.inputField.selectionFocusPosition = BetterChat.inputField.text.Length;
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(DevConsole), "RPCA_SendChat")]
    class Patch_rpca_dev
    {
        private static bool Prefix(string message, int playerViewID)
        {
            var player = PhotonNetwork.GetPhotonView(playerViewID);
            if (player.IsMine) return false;
            MenuControllerHandler.instance.GetComponent<PhotonView>().RPC("RPCA_CreateMessage", RpcTarget.All, "Unmodded user", player.GetComponent<Player>().teamID, message);
            return false;
        }
    }
}