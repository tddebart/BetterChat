using HarmonyLib;
using Photon.Pun;
using TMPro;
using UnboundLib;
using UnityEngine;

namespace BetterChat
{
    public class ChatMonoGameManager : MonoBehaviour
    {
        public static bool firstTime = true;
        
        [PunRPC]
        public void RPCA_CreateMessage(string playerName, int colorID, string message)
        {
            CreateLocalMessage(playerName,colorID,message);
            if (ChatMonoGameManager.firstTime)
            {
                BetterChat.instance.ShowChat();
                this.ExecuteAfterFrames(1, () =>
                {
                    BetterChat.instance.HideChat();
                });

                firstTime = false;
            }
        }

        public void CreateLocalMessage(string playerName, int colorID, string message, string objName = "")
        {
            var messObj = Instantiate(BetterChat.chatMessageObj, BetterChat.chatContentTrans);
            if (objName != "") messObj.name = objName;
            var color = GetPlayerColor(colorID);
            var UGUI = messObj.GetComponent<TextMeshProUGUI>();
            UGUI.text = "<color=" + color + ">" + playerName + "</color>"+ ": " + message;
            UGUI.alignment = BetterChat.TextOnRightSide
                ? TextAlignmentOptions.MidlineRight
                : TextAlignmentOptions.MidlineLeft;
            messObj.AddComponent<MessageMono>();
        }

        [PunRPC]
        public void RPCA_ActivateIndicator(int playerViewID)
        {
            var player = PhotonNetwork.GetPhotonView(playerViewID);
            player.transform.Find("WobbleObjects/Typing indicator(Clone)").gameObject.SetActive(true);

        }
        
        [PunRPC]
        public void RPCA_DeActivateIndicator(int playerViewID)
        {
            var player = PhotonNetwork.GetPhotonView(playerViewID);
            player.transform.Find("WobbleObjects/Typing indicator(Clone)").gameObject.SetActive(false);
        }

        public static string GetPlayerColor(int colorID)
        {
            return colorID != -1 ? "#" + ColorUtility.ToHtmlStringRGB(PlayerSkinBank.GetPlayerSkinColors(colorID).color) : "white";
        }
    }
}