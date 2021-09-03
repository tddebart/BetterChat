using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using Photon.Pun;
using TMPro;
using UnboundLib;
using UnboundLib.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterChat
{
    [BepInDependency("com.willis.rounds.unbound")]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class BetterChat : BaseUnityPlugin
    {
        private const string ModId = "com.bosssloth.rounds.BetterChat";
        private const string ModName = "BetterChat";
        public const string Version = "1.0.0";
        
        internal static AssetBundle chatAsset;

        public static GameObject chatCanvas;
        
        public static GameObject chatMessageObj;
        public static GameObject typingIndicatorObj;
        
        public static Transform chatContentTrans;

        public static Image contentPanel;
        
        public static TMP_InputField inputField;

        private TimeSince timeSinceTyped = 10;

        public static readonly List<GameObject> messageObjs = new List<GameObject>();

        public static bool isLockingInput;
        public static bool chatHidden;
        public bool isTyping;
        public bool indicatorOn;

        public static BetterChat instance;

        private void Start()
        {
            instance = this;
            
            var harmony = new Harmony(ModId);
            harmony.PatchAll();

            timeSinceTyped = 10;
            
            chatAsset = AssetUtils.LoadAssetBundleFromResources("chatbundle", typeof(BetterChat).Assembly);
            if (chatAsset == null)
            {
                UnityEngine.Debug.LogError("Couldn't find chatBundle?");
            }

            var canvasObj = chatAsset.LoadAsset<GameObject>("ChatCanvas");
            chatCanvas = Instantiate(canvasObj);
            DontDestroyOnLoad(chatCanvas);

            chatContentTrans = chatCanvas.transform.Find("Panel/Chats/Viewport/Content");

            chatMessageObj = chatAsset.LoadAsset<GameObject>("ChatMessage");
            typingIndicatorObj = chatAsset.LoadAsset<GameObject>("Typing indicator");

            contentPanel = chatContentTrans.GetComponent<Image>();

            On.MainMenuHandler.Awake += (orig, self) =>
            {
                this.ExecuteAfterSeconds(0.2f, () =>
                {
                    HideChat();
                    ResetChat();
                });
                
                orig(self);
            };

            inputField = chatCanvas.transform.Find("Panel/Input").GetComponent<TMP_InputField>();
            inputField.onEndEdit.AddListener(text =>
            {
                isLockingInput = false;
                if (!Input.GetKeyDown(KeyCode.Return)) return;
                this.ExecuteAfterSeconds(0.05f, () =>
                {
                    // Deselect the inputfield
                    inputField.DeactivateInputField(true);
                    inputField.ReleaseSelection();
                    inputField.OnDeselect(new BaseEventData(EventSystem.current));
                    
                    // don't send text if it isn't anything
                    if (string.IsNullOrWhiteSpace(text)) return;

                    if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.Players.Count != 0)
                    {
                        Player localPlayer = null;
                        // Send text to vanilla system so people without the mod can still see
                        if (PlayerManager.instance.players.Count != 0)
                        {
                            typeof(DevConsole).InvokeMember("Send", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, MenuControllerHandler.instance.GetComponent<DevConsole>(), new object[]{text});
                            localPlayer = PlayerManager.instance.players.First(pl => pl.GetComponent<PhotonView>().IsMine);
                        }

                        var teamID = localPlayer != null ? localPlayer.teamID : -1;

                        var localNickName = PhotonNetwork.LocalPlayer.NickName;
                        if (String.IsNullOrWhiteSpace(localNickName)) localNickName = "Player1";
                        
                        // Create the message
                        MenuControllerHandler.instance.GetComponent<PhotonView>().RPC("RPCA_CreateMessage", RpcTarget.All, localNickName, teamID, text);
                    }
                    
                    // Reset things
                    inputField.text = string.Empty;
                    chatCanvas.GetComponentInChildren<Scrollbar>(true).value = 0;
                    timeSinceTyped = 10;
                });
            });
            inputField.onSelect.AddListener(text =>
            {
                isLockingInput = true;
                ShowChat();
            });
            inputField.onDeselect.AddListener(text =>
            {
                isLockingInput = false;
                HideChat();
                timeSinceTyped = 10;
            });
            inputField.onValueChanged.AddListener(text =>
            {
                timeSinceTyped = 0;
            });
            HideChat();
        }

        public void HideChat()
        {
            chatCanvas.GetComponentInChildren<Image>(true).enabled = false;
            chatCanvas.GetComponentInChildren<ScrollRect>(true).enabled = false;
            chatCanvas.transform.Find("Panel/Chats/Scrollbar Vertical").gameObject.SetActive(false);
            chatCanvas.GetComponentInChildren<TMP_InputField>(true).gameObject.SetActive(false);
            chatHidden = true;
        }
        public void ShowChat()
        {
            chatCanvas.GetComponentInChildren<Image>(true).enabled = true;
            chatCanvas.GetComponentInChildren<ScrollRect>(true).enabled = true;
            chatCanvas.transform.Find("Panel/Chats/Scrollbar Vertical").gameObject.SetActive(true);
            chatCanvas.GetComponentInChildren<TMP_InputField>(true).gameObject.SetActive(true);
            chatHidden = false;
        }

        public void ResetChat()
        {
            foreach (var chat in chatContentTrans.GetComponentsInChildren<MessageMono>())
            {
                messageObjs.Remove(chat.gameObject);
                Destroy(chat.gameObject);
            }
        }

        void Update()
        {
            Unbound.lockInputBools["chatLock"] = isLockingInput;
            if (messageObjs.Count > 0 && chatHidden)
            {
                contentPanel.enabled = true;
            }
            else
            {
                contentPanel.enabled = false;
            }

            if (timeSinceTyped < 6)
            {
                isTyping = true;
            }
            else
            {
                isTyping = false;
            }
            
            if (isTyping && !indicatorOn)
            {
                if (PlayerManager.instance.players.Count != 0)
                {
                    UnityEngine.Debug.LogWarning("ActivateIndicator");
                    var localPlayer = PlayerManager.instance.players.First(pl => pl.GetComponent<PhotonView>().IsMine);
                    if (localPlayer == null) return;
                    var localViewID = localPlayer.data.view.ViewID;
                    MenuControllerHandler.instance.GetComponent<PhotonView>().RPC("RPCA_ActivateIndicator", RpcTarget.All, localViewID);
                    indicatorOn = true;
                }
            }
            else if (!isTyping && indicatorOn)
            {
                if (PlayerManager.instance.players.Count != 0)
                {
                    UnityEngine.Debug.LogWarning("DeActivateIndicator");
                    var localPlayer = PlayerManager.instance.players.First(pl => pl.GetComponent<PhotonView>().IsMine);
                    var localViewID = localPlayer.data.view.ViewID;
                    MenuControllerHandler.instance.GetComponent<PhotonView>().RPC("RPCA_DeActivateIndicator", RpcTarget.All, localViewID);
                    indicatorOn = false;
                }
            }
        }
    }
}