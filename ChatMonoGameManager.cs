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
            if (BetterChat.chatGroupsDict[groupName].receiveMessageCondition(senderPlayerID, localPlayer != null ? localPlayer.playerID : 0))
            {
                var extra = "";
                if (senderPlayerID != -1)
                {
                    if (BetterChat.deadChat)
                    {
                        if (senderPlayer == null) return;
                        if (senderPlayer.data.dead && !localPlayer.data.dead)
                        {
                            return;
                        }
                    }

                    extra = BetterChat.deadChat && senderPlayer.data.dead ? "<color=red>*DEAD*</color> " : "";
                }

                //CreateLocalMessage(extra, groupName, playerName, colorID,message);
                // CreateLocalMessage($"{extra}({groupName}) " + playerName,colorID,message,groupName);
                //if (groupName != "ALL")
                //{
                    // CreateLocalMessage($"{extra}({extra + groupName}) " + playerName,colorID,message);
                    CreateLocalMessage(extra,"ALL",playerName,colorID,message,groupName);
                //}
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

        public void CreateLocalMessage(string prefix, [CanBeNull] string groupName, string playerName, int colorID, string message, string visualGroupName = null, string objName = "")
        {
            if(groupName == null)
                groupName = "ALL";

            var groupNameBegin = groupName;
            var messObj = Instantiate(BetterChat.chatMessageObj, BetterChat.chatGroupsDict[groupName].content);
            if (objName != "") messObj.name = objName;
            var color = GetPlayerColor(colorID);
            var UGUI = messObj.GetComponent<TextMeshProUGUI>();
            if (visualGroupName != null)
            {
                groupName = visualGroupName;
            }
            
            if (BetterChat.UsePlayerColors && colorID >= 0)
            {
                playerName = UnboundLib.Utils.ExtraPlayerSkins.GetTeamColorName(colorID);
            }
            
            if (groupName != "")
            {
                UGUI.text = $"<color={color}>{prefix}({groupName}) {playerName}</color>: {message}";
            }
            else
            {
                UGUI.text = $"<color={color}>{prefix} {playerName}</color>: {message}";
            }
            UGUI.alignment = BetterChat.TextOnRightSide
                ? TextAlignmentOptions.MidlineRight
                : TextAlignmentOptions.MidlineLeft;
            var mono = messObj.AddComponent<MessageMono>();
            if (groupNameBegin != "ALL")
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