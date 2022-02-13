using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Photon.Pun;
using TMPro;
using UnboundLib;
using UnityEngine;

namespace BetterChat
{
    public class ChatMonoGameManager : MonoBehaviour
    {
        public static bool firstTime = true;
        public static ChatMonoGameManager Instance;

        public void Awake()
        {
            Instance = this;   
        }

        [PunRPC]
        public void RPCA_CreateMessage(string playerName, int colorID, string message, string groupName, int senderPlayerID)
        {
            var localPlayer = PlayerManager.instance.players.FirstOrDefault(p => p.data.view.IsMine);
            var senderPlayer = PlayerManager.instance.GetPlayerById(senderPlayerID);
            if (BetterChat.chatContentDict[groupName].ReceiveMessageCondition(senderPlayerID, localPlayer != null ? localPlayer.playerID : 0))
            {
                if (BetterChat.deadChat)
                {
                    if (senderPlayer == null) return;
                    if (senderPlayer.data.dead && !localPlayer.data.dead)
                    {
                        return;
                    }
                }

                var extra = BetterChat.deadChat && senderPlayer.data.dead ? "*DEAD* " : "";
                CreateLocalMessage($"{extra}({groupName}) " + playerName,colorID,message,groupName);
                if (groupName != "ALL")
                {
                    CreateLocalMessage($"{extra}({extra + groupName}) " + playerName,colorID,message);
                }
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
        }

        public void CreateLocalMessage(string playerName, int colorID, string message,string groupName = null, string objName = "")
        {
            if(groupName == null)
                groupName = "ALL";
            
            var messObj = Instantiate(BetterChat.chatMessageObj, BetterChat.chatContentDict[groupName].content);
            if (objName != "") messObj.name = objName;
            var color = GetPlayerColor(colorID);
            var UGUI = messObj.GetComponent<TextMeshProUGUI>();
            UGUI.text = "<color=" + color + ">" + playerName + "</color>"+ ": " + message;
            UGUI.alignment = BetterChat.TextOnRightSide
                ? TextAlignmentOptions.MidlineRight
                : TextAlignmentOptions.MidlineLeft;
            var mono = messObj.AddComponent<MessageMono>();
            if (groupName != "ALL")
            {
                mono.shouldDisappearOverTime = false;
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

        public static string GetPlayerColor(int colorID)
        {
            return colorID != -1 ? "#" + ColorUtility.ToHtmlStringRGB(PlayerSkinBank.GetPlayerSkinColors(colorID).color) : "white";
        }
    }
}