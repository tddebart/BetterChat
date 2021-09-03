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
        public static bool isTyping;
        public static bool indicatorOn;

        private void Start()
        {
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

            inputField = chatCanvas.transform.Find("Panel/Input").GetComponent<TMP_InputField>();
            inputField.onEndEdit.AddListener(text =>
            {
                isLockingInput = false;
                if (!Input.GetKeyDown(KeyCode.Return)) return;
                this.ExecuteAfterSeconds(0.05f, () =>
                {
                    timeSinceTyped = 10;
                    inputField.DeactivateInputField(true);
                    inputField.ReleaseSelection();
                    inputField.OnDeselect(new BaseEventData(EventSystem.current));
                    if (string.IsNullOrWhiteSpace(text)) return;
                    typeof(DevConsole).InvokeMember("Send", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic, null, MenuControllerHandler.instance.GetComponent<DevConsole>(), new object[]{text});
                    var localPlayer = PlayerManager.instance.players.First(pl => pl.GetComponent<PhotonView>().IsMine);
                    localPlayer.GetComponent<PhotonView>().RPC("RPCA_CreateMessage", RpcTarget.All, "Player" + (localPlayer.playerID+1f), text);
                    inputField.text = string.Empty;
                    chatCanvas.GetComponentInChildren<Scrollbar>(true).value = 0;
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
            inputField.onValueChanged.AddListener(arg0 =>
            {
                timeSinceTyped = 0;
            });
            HideChat();
        }

        void HideChat()
        {
            chatCanvas.GetComponentInChildren<Image>().enabled = false;
            chatCanvas.GetComponentInChildren<ScrollRect>().enabled = false;
            chatCanvas.transform.Find("Panel/Chats/Scrollbar Vertical").gameObject.SetActive(false);
            chatCanvas.GetComponentInChildren<TMP_InputField>().gameObject.SetActive(false);
            chatHidden = true;
        }
        void ShowChat()
        {
            chatCanvas.GetComponentInChildren<Image>(true).enabled = true;
            chatCanvas.GetComponentInChildren<ScrollRect>(true).enabled = true;
            chatCanvas.transform.Find("Panel/Chats/Scrollbar Vertical").gameObject.SetActive(true);
            chatCanvas.GetComponentInChildren<TMP_InputField>(true).gameObject.SetActive(true);
            chatHidden = false;
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
                UnityEngine.Debug.LogWarning("ActivateIndicator");
                var localPlayer = PlayerManager.instance.players.First(pl => pl.GetComponent<PhotonView>().IsMine);
                var localViewID = localPlayer.data.view.ViewID;
                localPlayer.data.view.RPC("RPCA_ActivateIndicator", RpcTarget.All, localViewID);
            }
            else if (!isTyping && indicatorOn)
            {
                UnityEngine.Debug.LogWarning("DeActivateIndicator");
                var localPlayer = PlayerManager.instance.players.First(pl => pl.GetComponent<PhotonView>().IsMine);
                var localViewID = localPlayer.data.view.ViewID;
                localPlayer.data.view.RPC("RPCA_DeActivateIndicator", RpcTarget.All, localViewID);
            }
        }
    }
}