using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnboundLib;
using UnboundLib.Extensions;
using UnboundLib.Utils;
using UnboundLib.Utils.UI;
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
        public static Image mainPanelImg;

        public RectTransform mainPanel;

        public static TMP_InputField inputField;

        private TimeSince timeSinceTyped = 10;

        public static readonly List<GameObject> messageObjs = new List<GameObject>();

        public static bool isLockingInput;
        public static bool chatHidden;
        public bool isTyping;
        public bool indicatorOn;

        public static BetterChat instance;

        public static int Width
        {
            get
            {
                return PlayerPrefs.GetInt(GetConfigKey("width"), 550);
            }
            set
            {
                PlayerPrefs.SetInt(GetConfigKey("width"), value);
            }
        }
        public static int Height
        {
            get
            {
                return PlayerPrefs.GetInt(GetConfigKey("height"), 400);
            }
            set
            {
                PlayerPrefs.SetInt(GetConfigKey("height"), value);
            }
        }
        public static int XLoc
        {
            get
            {
                return PlayerPrefs.GetInt(GetConfigKey("xLoc"), 25);
            }
            set
            {
                PlayerPrefs.SetInt(GetConfigKey("xLoc"), value);
            }
        }
        public static int YLoc
        {
            get
            {
                return PlayerPrefs.GetInt(GetConfigKey("yLoc"), 25);
            }
            set
            {
                PlayerPrefs.SetInt(GetConfigKey("yLoc"), value);
            }
        }

        public static bool TextOnRightSide
        {
            get
            {
                return PlayerPrefs.GetInt(GetConfigKey("textOnRightSide"), 1) == 1;
            }
            set
            {
                PlayerPrefs.SetInt(GetConfigKey("textOnRightSide"), value ? 1 : 0);
            }
        }
        public static float TimeBeforeTextGone
        {
            get
            {
                return PlayerPrefs.GetFloat(GetConfigKey("timeBeforeTextGone"), 6.5f);
            }
            set
            {
                PlayerPrefs.SetFloat(GetConfigKey("timeBeforeTextGone"), value);
            }
        }
        public static bool ClearMessageOnEnter
        {
            get
            {
                return PlayerPrefs.GetInt(GetConfigKey("clearMessageOnEnter"), 1) == 1;
            }
            set
            {
                PlayerPrefs.SetInt(GetConfigKey("clearMessageOnEnter"), value ? 1 : 0);
            }
        }
        public static float BackgroundOpacity
        {
            get
            {
                return PlayerPrefs.GetFloat(GetConfigKey("backgroundOpacity"), 70f);
            }
            set
            {
                PlayerPrefs.SetFloat(GetConfigKey("backgroundOpacity"), value);
            }
        }

        public static readonly List<string> pastMessages = new List<string>();
        public static int currentPastIndex;

        static string GetConfigKey(string key) => $"{BetterChat.ModName}_{key}";

        private void Start()
        {
            instance = this;
            
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
            
            Unbound.RegisterClientSideMod(ModId);
            
            timeSinceTyped = 10;
            
            Unbound.RegisterMenu("Better chat", () =>
            {
                MenuControllerHandler.instance.GetComponent<ChatMonoGameManager>().CreateLocalMessage("Player1", 0, "Lorem ipsum dolor sit amet,", "Remove");
                MenuControllerHandler.instance.GetComponent<ChatMonoGameManager>().CreateLocalMessage("Player2", 1, "consectetur adipiscing elit,", "Remove");
                MenuControllerHandler.instance.GetComponent<ChatMonoGameManager>().CreateLocalMessage("Player3", 2, "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.", "Remove");
                MenuControllerHandler.instance.GetComponent<ChatMonoGameManager>().CreateLocalMessage("Player4", 3, "Ut enim ad minim veniam", "Remove");
                ShowChat();
                inputField.enabled = false;
            }, MenuBuilder, null, true);
            
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

            mainPanel = chatCanvas.transform.Find("Panel").gameObject.GetComponent<RectTransform>();
            mainPanelImg = mainPanel.GetComponent<Image>();

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
                    //inputField.DeactivateInputField();
                    //inputField.ReleaseSelection();
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

                        var colorID = localPlayer != null ? localPlayer.colorID() : -1;

                        var localNickName = PhotonNetwork.LocalPlayer.NickName;
                        if (String.IsNullOrWhiteSpace(localNickName)) localNickName = "Player1";
                        
                        // Create the message
                        MenuControllerHandler.instance.GetComponent<PhotonView>().RPC("RPCA_CreateMessage", RpcTarget.All, localNickName, colorID, text);
                        currentPastIndex = 0;
                        pastMessages.Insert(0, text);
                    }
                    
                    // Reset things
                    if (ClearMessageOnEnter)
                    {
                        inputField.text = string.Empty;
                        inputField.selectionAnchorPosition = 0;
                    }
                    else
                    {
                        inputField.text = text;
                    }
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

        public void MenuBuilder( GameObject menu)
        {
            MenuHandler.CreateText("If you want to open the vanilla chat windows press shift + enter", menu, out _, 40);

            MenuHandler.CreateText(" ", menu, out _);

            MenuHandler.CreateSlider("Width", menu, 50, 150, 1000, Width, value =>
            {
                Width = (int)value;
            }, out var widthSlider, true);
            MenuHandler.CreateSlider("Height", menu, 50, 150, 1000, Height, value =>
            {
                Height = (int)value;
            }, out var heightSlider, true);
            
            MenuHandler.CreateSlider("X location", menu, 50, 0, 1750, XLoc, value =>
            {
                XLoc = (int)value;
            }, out var xSlider, true);
            MenuHandler.CreateSlider("Y location", menu, 50, 0, 950, YLoc, value =>
            {
                YLoc = (int)value;
            }, out var ySlider, true);

            var toggle = MenuHandler.CreateToggle(TextOnRightSide, "Text on right side", menu, value =>
            {
                TextOnRightSide = value;
                foreach (var chat in chatContentTrans.GetComponentsInChildren<MessageMono>())
                {
                    chat.GetComponent<TextMeshProUGUI>().alignment = TextOnRightSide
                        ? TextAlignmentOptions.MidlineRight
                        : TextAlignmentOptions.MidlineLeft;
                }
            }, 50);
            
            MenuHandler.CreateSlider("Seconds before message disappears", menu, 50, 1, 15, TimeBeforeTextGone, value =>
            {
                TimeBeforeTextGone = value;
            }, out var secondsSlider);
            
            var toggle2 = MenuHandler.CreateToggle(ClearMessageOnEnter, "Clear message on enter", menu, value =>
            {
                ClearMessageOnEnter = value;
            }, 50);
            
            MenuHandler.CreateSlider("Background opacity", menu, 50, 0, 100, BackgroundOpacity, value =>
            {
                BackgroundOpacity = value;
            }, out var opacitySlider, true);
            
            
            MenuHandler.CreateButton("Reset all", menu, () =>
            {
                Width = 550;
                Height = 400;
                XLoc = 25;
                YLoc = 25;
                TextOnRightSide = true;
                TimeBeforeTextGone = 6.5f;
                ClearMessageOnEnter = true;
                BackgroundOpacity = 70f;

                widthSlider.value = Width;
                heightSlider.value = Height;
                xSlider.value = XLoc;
                ySlider.value = YLoc;
                toggle.GetComponent<Toggle>().isOn = TextOnRightSide;
                secondsSlider.value = TimeBeforeTextGone;
                toggle2.GetComponent<Toggle>().isOn = ClearMessageOnEnter;
                opacitySlider.value = BackgroundOpacity;
            }, 40);
            
            // Create back actions
            menu.GetComponentInChildren<GoBack>(true).goBackEvent.AddListener(() =>
            {
               HideChat();
               foreach (Transform obj in chatContentTrans)
               {
                   if (obj.name == "Remove")
                   {
                       Destroy(obj.gameObject);
                   }
               }
               inputField.enabled = true;
            });
            menu.transform.Find("Group/Back").gameObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                HideChat();
                foreach (Transform obj in chatContentTrans)
                {
                    if (obj.name == "Remove")
                    {
                        Destroy(obj.gameObject);
                    }
                }
                inputField.enabled = true;
            });
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
                //messageObjs.Remove(chat.gameObject);
                Destroy(chat.gameObject);
            }

            pastMessages.Clear();
            currentPastIndex = 0;
        }

        void Update()
        {
            Unbound.lockInputBools["chatLock"] = isLockingInput;
            
            // Arrow message history
            if (isLockingInput && Input.GetKeyDown(KeyCode.UpArrow) && currentPastIndex != pastMessages.Count-1)
            {
                currentPastIndex++;
                inputField.text = pastMessages[currentPastIndex];
            }
            if (isLockingInput && Input.GetKeyDown(KeyCode.DownArrow) && currentPastIndex != 0)
            {
                currentPastIndex--;
                inputField.text = pastMessages[currentPastIndex];
            }
            
            // Enable and disable background for singular messages
            if (messageObjs.Count > 0 && chatHidden)
            {
                contentPanel.enabled = true;
            }
            else
            {
                contentPanel.enabled = false;
            }

            // Active typing indicator
            if (timeSinceTyped < 6)
            {
                isTyping = true;
            }
            else
            {
                isTyping = false;
            }

            // Update opacity
            if (mainPanelImg.color.a != BackgroundOpacity / 100f ||
                contentPanel.color.a != BackgroundOpacity / 100f)
            {
                mainPanelImg.SetAlpha(BackgroundOpacity / 100f);
                contentPanel.SetAlpha(BackgroundOpacity / 100f);
            }

            // Update width and height
            if (mainPanel.sizeDelta.x != Width || mainPanel.sizeDelta.y != Height)
            {
                mainPanel.sizeDelta = new Vector2(Width, Height);
            }
            
            // Update x and y position
            if (mainPanel.anchoredPosition.x != XLoc || mainPanel.anchoredPosition.y != YLoc)
            {
                mainPanel.anchoredPosition = new Vector2(-XLoc, YLoc);
            }
            
            
            if (isTyping && !indicatorOn)
            {
                if (PlayerManager.instance.players.Count != 0)
                {
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
                    var localPlayer = PlayerManager.instance.players.First(pl => pl.GetComponent<PhotonView>().IsMine);
                    var localViewID = localPlayer.data.view.ViewID;
                    MenuControllerHandler.instance.GetComponent<PhotonView>().RPC("RPCA_DeActivateIndicator", RpcTarget.All, localViewID);
                    indicatorOn = false;
                }
            }
        }
    }
}