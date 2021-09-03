using Photon.Pun;
using TMPro;
using UnityEngine;

namespace BetterChat
{
    public class ChatMono : MonoBehaviour
    {
        [PunRPC]
        public void RPCA_CreateMessage(string playerName, string message)
        {
            var messObj = Instantiate(BetterChat.chatMessageObj, BetterChat.chatContentTrans);
            messObj.GetComponent<TextMeshProUGUI>().text = playerName + ": " + message;
            messObj.AddComponent<MessageMono>();
        }

        [PunRPC]
        public void RPCA_ActivateIndicator(int playerViewID)
        {
            var player = PhotonNetwork.GetPhotonView(playerViewID);
            player.transform.Find("WobbleObjects/Typing indicator(Clone)").gameObject.SetActive(true);
            BetterChat.indicatorOn = true;
        }
        
        [PunRPC]
        public void RPCA_DeActivateIndicator(int playerViewID)
        {
            var player = PhotonNetwork.GetPhotonView(playerViewID);
            player.transform.Find("WobbleObjects/Typing indicator(Clone)").gameObject.SetActive(false);
            BetterChat.indicatorOn = false;
        }
    }
}