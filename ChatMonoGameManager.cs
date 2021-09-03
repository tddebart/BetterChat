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
        public void RPCA_CreateMessage(string playerName, int teamID, string message)
        {
            var messObj = Instantiate(BetterChat.chatMessageObj, BetterChat.chatContentTrans);
            var color = GetPlayerColor(teamID);
            messObj.GetComponent<TextMeshProUGUI>().text = "<color=" + color + ">" + playerName + "</color>"+ ": " + message;
            messObj.AddComponent<MessageMono>();
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

        public static string GetPlayerColor(int teamID)
        {
            return teamID != -1 ? "#" + ColorUtility.ToHtmlStringRGB(PlayerSkinBank.GetPlayerSkinColors(teamID).color) : "white";
        }
    }
}