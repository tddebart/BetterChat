using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using TMPro;
using UnboundLib;
using UnboundLib.Extensions;
using UnboundLib.GameModes;
using UnboundLib.Networking;
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
        public const string Version = "1.1.0";

        internal static AssetBundle chatAsset;

        public static GameObject chatCanvas;

        public static GameObject chatMessageObj;
        public static GameObject typingIndicatorObj;

        // public static Transform chatContentTrans;
        public static Dictionary<string, GroupSettings> chatGroupsDict = new Dictionary<string, GroupSettings>();
        public static string currentGroup = "ALL";

        public static Image contentPanel => chatGroupsDict[currentGroup].content.GetComponent<Image>();
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
            get => PlayerPrefs.GetInt(GetConfigKey("width"), 550);
            set => PlayerPrefs.SetInt(GetConfigKey("width"), value);
        }
        public static int Height
        {
            get => PlayerPrefs.GetInt(GetConfigKey("height"), 400);
            set => PlayerPrefs.SetInt(GetConfigKey("height"), value);
        }
        public static int XLoc
        {
            get => PlayerPrefs.GetInt(GetConfigKey("xLoc"), 25);
            set => PlayerPrefs.SetInt(GetConfigKey("xLoc"), value);
        }
        public static int YLoc
        {
            get => PlayerPrefs.GetInt(GetConfigKey("yLoc"), 25);
            set => PlayerPrefs.SetInt(GetConfigKey("yLoc"), value);
        }

        public static bool TextOnRightSide
        {
            get => PlayerPrefs.GetInt(GetConfigKey("textOnRightSide"), 1) == 1;
            set => PlayerPrefs.SetInt(GetConfigKey("textOnRightSide"), value ? 1 : 0);
        }
        public static float TimeBeforeTextGone
        {
            get => PlayerPrefs.GetFloat(GetConfigKey("timeBeforeTextGone"), 6.5f);
            set => PlayerPrefs.SetFloat(GetConfigKey("timeBeforeTextGone"), value);
        }
        public static bool ClearMessageOnEnter
        {
            get => PlayerPrefs.GetInt(GetConfigKey("clearMessageOnEnter"), 1) == 1;
            set => PlayerPrefs.SetInt(GetConfigKey("clearMessageOnEnter"), value ? 1 : 0);
        }
        public static float BackgroundOpacity
        {
            get => PlayerPrefs.GetFloat(GetConfigKey("backgroundOpacity"), 70f);
            set => PlayerPrefs.SetFloat(GetConfigKey("backgroundOpacity"), value);
        }

        private static ConfigEntry<bool> _deadChat;
        public static bool deadChat { get; private set; }

        public static bool UsePlayerColors;

        public static readonly List<string> pastMessages = new List<string>();
        public static int currentPastIndex;

        static string GetConfigKey(string key) => $"{BetterChat.ModName}_{key}";

        //https://www.google.com/search?q=gmod+team+chat&client=firefox-b-d&channel=trow5&sxsrf=APq-WBs8EAcLAb1DOJ0jnNGA815yBMa3TQ:1644414874749&source=lnms&tbm=isch&sa=X&ved=2ahUKEwifpePj4vL1AhVhlP0HHRO4CJ8Q_AUoAnoECAEQBA&biw=2261&bih=1147&dpr=1.13#imgrc=SABTW78NhYAg1M
        
        private void Start()
        {
            instance = this;
            
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
            
            Unbound.RegisterClientSideMod(ModId);
            
            timeSinceTyped = 10;
            
            Unbound.RegisterMenu("Better chat", () =>
            {
                MenuControllerHandler.instance.GetComponent<ChatMonoGameManager>().CreateLocalMessage("","ALL", "Player1", 0, "Lorem ipsum dolor sit amet,", "","Remove");
                MenuControllerHandler.instance.GetComponent<ChatMonoGameManager>().CreateLocalMessage("","ALL","Player2", 1, "consectetur adipiscing elit,", "","Remove");
                MenuControllerHandler.instance.GetComponent<ChatMonoGameManager>().CreateLocalMessage("","ALL","Player3", 2, "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.", "","Remove");
                MenuControllerHandler.instance.GetComponent<ChatMonoGameManager>().CreateLocalMessage("","ALL","Player4", 3, "Ut enim ad minim veniam", "","Remove");
                ShowChat();
                inputField.enabled = false;
            }, MenuBuilder, null, true);
            
            _deadChat = Config.Bind("BetterChat", "DeadChat", false , "Enable dead chat");
            deadChat = _deadChat.Value;
            Unbound.RegisterHandshake(ModId, OnHandShakeCompleted);
            
            chatAsset = AssetUtils.LoadAssetBundleFromResources("chatbundle", typeof(BetterChat).Assembly);
            if (chatAsset == null)
            {
                UnityEngine.Debug.LogError("Couldn't find chatBundle?");
            }

            var canvasObj = chatAsset.LoadAsset<GameObject>("ChatCanvas");
            chatCanvas = Instantiate(canvasObj);
            DontDestroyOnLoad(chatCanvas);
            chatCanvas.AddComponent<CanvasGroup>().blocksRaycasts = false;

            // chatContentTrans = chatCanvas.transform.Find("Panel/Chats/ALL/Viewport/Content");
            // chatContentDict.Add("ALL", chatCanvas.transform.Find("Panel/Chats/ALL/Viewport/Content"));
            CreateGroup("ALL",  new GroupSettings()
            {
                receiveMessageCondition = (i,j) => true,
                keyBind = KeyCode.T
            } );
            CreateGroup("TEAM",  new GroupSettings()
            {
                receiveMessageCondition = (i, j) => PlayerManager.instance.GetPlayerById(i)?.teamID == PlayerManager.instance.GetPlayerById(j)?.teamID,
                keyBind = KeyCode.Y,
            });

            chatMessageObj = chatAsset.LoadAsset<GameObject>("ChatMessage");
            typingIndicatorObj = chatAsset.LoadAsset<GameObject>("Typing indicator");

            mainPanel = chatCanvas.transform.Find("Panel").gameObject.GetComponent<RectTransform>();
            mainPanelImg = mainPanel.GetComponent<Image>();

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
                    // Deselect the input field
                    //inputField.DeactivateInputField();
                    //inputField.ReleaseSelection();
                    inputField.OnDeselect(new BaseEventData(EventSystem.current));
                    HideChat();
                    
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
                        MenuControllerHandler.instance.GetComponent<PhotonView>().RPC("RPCA_CreateMessage", RpcTarget.All, localNickName, colorID, text,currentGroup, localPlayer != null ? localPlayer.playerID : 0);
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
                    
                    currentGroup = "ALL";
                    chatGroupsDict["ALL"].groupButton.onClick.Invoke();
                });
            });
            inputField.onValueChanged.AddListener(text =>
            {
                timeSinceTyped = 0;
            });
            HideChat();
            
            
            // Register on game start
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, OnBattleStart);
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
                foreach (var chat in chatGroupsDict["ALL"].content.GetComponentsInChildren<MessageMono>())
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
            
            GameObject deadChatToggle = null;
            if (!GameManager.instance.isPlaying)
            {
                deadChatToggle = MenuHandler.CreateToggle(_deadChat.Value, "Enable dead chat", menu, value =>
                {
                    if (!GameManager.instance.isPlaying)
                    {
                        _deadChat.Value = value;
                        deadChat = value;
                    }
                });
            }
            
            
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
                if (!GameManager.instance.isPlaying)
                {
                    _deadChat.Value = false;
                    deadChat = false;
                }

                widthSlider.value = Width;
                heightSlider.value = Height;
                xSlider.value = XLoc;
                ySlider.value = YLoc;
                toggle.GetComponent<Toggle>().isOn = TextOnRightSide;
                secondsSlider.value = TimeBeforeTextGone;
                toggle2.GetComponent<Toggle>().isOn = ClearMessageOnEnter;
                opacitySlider.value = BackgroundOpacity;
                if (deadChatToggle != null && !GameManager.instance.isPlaying)
                {
                    deadChatToggle.GetComponent<Toggle>().isOn = _deadChat.Value;
                }
            }, 40);
            
            // Create back actions
            menu.GetComponentInChildren<GoBack>(true).goBackEvent.AddListener(() =>
            {
               HideChat();
               foreach (Transform obj in chatGroupsDict["ALL"].content)
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
                foreach (Transform obj in chatGroupsDict["ALL"].content)
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
            // Background image
            chatCanvas.GetComponentInChildren<Image>(true).enabled = false;
            // Input field
            chatCanvas.GetComponentInChildren<TMP_InputField>(true).gameObject.SetActive(false);
            // Tabs
            chatCanvas.transform.Find("Panel/Groups").gameObject.SetActive(false);
            chatCanvas.GetComponentInChildren<CanvasGroup>().blocksRaycasts = false;
            chatHidden = true;
            timeSinceTyped = 10;
        }
        public void ShowChat()
        {
            // Background image
            chatCanvas.GetComponentInChildren<Image>(true).enabled = true;
            // Input field
            chatCanvas.GetComponentInChildren<TMP_InputField>(true).gameObject.SetActive(true);
            // Tabs
            chatCanvas.transform.Find("Panel/Groups").gameObject.SetActive(true);
            chatCanvas.GetComponentInChildren<CanvasGroup>().blocksRaycasts = true;
            chatHidden = false;
        }

        public static void ResetChat()
        {
            foreach (var chat in chatGroupsDict.Values.SelectMany(obj => obj.content.GetComponentsInChildren<MessageMono>()))
            {
                //messageObjs.Remove(chat.gameObject);
                Destroy(chat.gameObject);
            }

            foreach (var chat in chatGroupsDict.ToDictionary(obj => obj.Key, obj => obj.Value))
            {
                chatGroupsDict.Remove(chat.Key);
                Destroy(chat.Value.ChatObj);
                Destroy(chat.Value.groupButton.gameObject);
            }

            pastMessages.Clear();
            currentPastIndex = 0;

            UsePlayerColors = false;
            
            CreateGroup("ALL",  new GroupSettings()
            {
                receiveMessageCondition = (i,j) => true,
                keyBind = KeyCode.T
            } );
            CreateGroup("TEAM",  new GroupSettings()
            {
                receiveMessageCondition = (i, j) => PlayerManager.instance.GetPlayerById(i)?.teamID == PlayerManager.instance.GetPlayerById(j)?.teamID,
                keyBind = KeyCode.Y
            });
        }
        
        
        public static void CreateGroup(string groupName, GroupSettings groupSettings)
        {
            var chatObj = Instantiate(chatCanvas.transform.Find("Panel/Chats/object"), chatCanvas.transform.Find("Panel/Chats"));
            chatObj.name = groupName;
            
            var groupObj = Instantiate(chatCanvas.transform.Find("Panel/Groups/Scroll View/Viewport/Content/TabObject"), chatCanvas.transform.Find("Panel/Groups/Scroll View/Viewport/Content"));
            groupObj.name = groupName;
            groupObj.GetComponentInChildren<TextMeshProUGUI>().text = groupName.ToUpper();
            groupObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                foreach (var value in chatGroupsDict)
                {
                    value.Value.ChatObj.SetActive(false);
                    foreach (Transform chat in value.Value.content.transform)
                    {
                        chat.GetComponent<MessageMono>().Update();
                    }
                    chatCanvas.transform.Find($"Panel/Groups/Scroll View/Viewport/Content/{value.Key}").GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
                chatGroupsDict[groupName].ChatObj.SetActive(true);
                chatCanvas.transform.Find($"Panel/Groups/Scroll View/Viewport/Content/{groupName}").GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 1f);
                currentGroup = groupName;
                

                groupSettings.onGroupButtonClicked?.Invoke();
            });
            groupObj.gameObject.SetActive(true);
            chatGroupsDict.Add(groupName, new GroupSettings(groupSettings, chatObj.transform.Find("Viewport/Content"), chatObj.transform.Find("Viewport/Content").parent.parent.gameObject, groupObj.GetComponent<Button>()));
            if (groupName == "ALL")
            {
                instance.ExecuteAfterSeconds(1,() => groupObj.GetComponent<Button>().onClick.Invoke());
                
            }
        }

        public class GroupSettings
        {
            public Func<int, int, bool> receiveMessageCondition;
            public KeyCode keyBind;
            public Action onGroupButtonClicked;
            public Func<int, bool> canSeeGroup;
            
            public Transform content;
            public GameObject ChatObj;
            public Button groupButton;

            /// <summary>
            /// </summary>
            /// <param name="receiveMessageCondition">
            /// A condition if the message should be received.
            /// First int is sender playerID,
            /// Second int is receiver playerID,
            /// </param>
            /// <param name="keyBind"></param>
            /// <param name="onGroupButtonClicked"></param>
            /// <param name="canSeeGroup"></param>
            public GroupSettings(Func<int, int, bool> receiveMessageCondition =null, KeyCode keyBind = KeyCode.None, Action onGroupButtonClicked = null, Func<int, bool> canSeeGroup = null)
            {
                this.receiveMessageCondition = receiveMessageCondition;
                this.keyBind = keyBind;
                this.onGroupButtonClicked = onGroupButtonClicked;
                this.canSeeGroup = canSeeGroup;
            }
            
            internal GroupSettings(Transform content, GameObject chatObj, Func<int, int, bool> receiveMessageCondition = null, KeyCode keyBind = KeyCode.None, Action onGroupButtonClicked = null)
            {
                this.content = content;
                this.ChatObj = chatObj;
                this.receiveMessageCondition = receiveMessageCondition;
                this.keyBind = keyBind;
                this.onGroupButtonClicked = onGroupButtonClicked;
            }

            internal GroupSettings(GroupSettings settings, Transform content, GameObject chatObj, Button groupButton)
            {
                this.content = content;
                this.ChatObj = chatObj;
                this.receiveMessageCondition = settings.receiveMessageCondition;
                this.keyBind = settings.keyBind;
                this.onGroupButtonClicked = settings.onGroupButtonClicked;
                this.canSeeGroup = settings.canSeeGroup;
                this.groupButton = groupButton;
            }
        }

        public static void OpenChatForGroup(string key, GroupSettings group)
        {
            var localPlayer = PlayerManager.instance.GetLocalPlayer();
            if ((!chatGroupsDict[key].canSeeGroup?.Invoke(localPlayer.playerID)) ?? false)
            {
                return;
            }
            isLockingInput = true;
            currentGroup = key;
            group.groupButton.onClick.Invoke();
                
            instance.ShowChat();
                
            inputField.OnSelect(new BaseEventData(EventSystem.current));
            if (!ClearMessageOnEnter)
            {
                inputField.selectionAnchorPosition = 0;
                inputField.selectionFocusPosition = inputField.text.Length;
            }
        }
        
        //DeadChat setting sync
        internal static void OnHandShakeCompleted()
        {
            if (PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            {
                NetworkingManager.RPC(typeof(BetterChat), nameof(BetterChat.SyncSettings), BetterChat._deadChat.Value);
            }
        }
        
        [UnboundRPC]
        public static void SyncSettings(bool _deadChat)
        {
            BetterChat.deadChat = _deadChat;
        }

        public static void SetDeadChat(bool value) 
        {
            BetterChat.deadChat = value;
        }

        public static IEnumerator OnBattleStart(IGameModeHandler gameModeHandler)
        {
            EvaluateCanSeeGroup();
            yield break;
        }

        public static void EvaluateCanSeeGroup(string key)
        {
            var localPlayer = PlayerManager.instance.players.First(p => p.data.view.IsMine);
            var group = chatGroupsDict[key];
            if (group.canSeeGroup?.Invoke(localPlayer.playerID) ?? true)
            {
                group.groupButton.gameObject.SetActive(true);
            } else
            {
                group.groupButton.gameObject.SetActive(false);
            }
        }

        public static void EvaluateCanSeeGroup()
        {
            foreach (var group in BetterChat.chatGroupsDict)
            {
                EvaluateCanSeeGroup(group.Key);
            }
            
            OpenChatForGroup("ALL", BetterChat.chatGroupsDict["ALL"]);
            instance.HideChat();
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
            
            // Check for all group keybinds
            foreach (var group in chatGroupsDict.Where(group => Input.GetKeyDown(group.Value.keyBind)))
            {
                var localPlayer = PlayerManager.instance.players.First(p => p.data.view.IsMine);
                if(!PhotonNetwork.IsConnected || (!group.Value.canSeeGroup?.Invoke(localPlayer.playerID) ?? false)) break;
                
                OpenChatForGroup(group.Key, group.Value);
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

public static class PlayerManagerExtensions
{
    [CanBeNull]
    public static Player GetPlayerById(this PlayerManager playerManager, int playerID)
    {
        return playerManager.players.FirstOrDefault(pl => pl.playerID == playerID);
    }

    public static Player GetLocalPlayer(this PlayerManager pl)
    {
        return pl.players.FirstOrDefault(p => p.data.view.IsMine);
    }
}