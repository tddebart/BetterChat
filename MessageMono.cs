using TMPro;
using UnboundLib;
using UnboundLib.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace BetterChat
{
    public class MessageMono : MonoBehaviour
    {
        public TimeSince timeSinceAwake;
        public bool hideWhenChatHidden;
        public bool shouldDisappearOverTime=  true;

        private TextMeshProUGUI uGUI;
        private LayoutElement layout;

        public bool startCalled;

        private void Start()
        {
            timeSinceAwake = 0;
            uGUI = GetComponent<TextMeshProUGUI>();
            layout = GetComponent<LayoutElement>();
            BetterChat.messageObjs.Add(gameObject);
            BetterChat.instance.ExecuteAfterFrames(1, () =>
            {
                startCalled = true;
            });
        }

        public void Update()
        {
            if (!startCalled) return;
            
            if (timeSinceAwake > BetterChat.TimeBeforeTextGone || !shouldDisappearOverTime)
            {
                hideWhenChatHidden = true;
                BetterChat.messageObjs.Remove(gameObject);
            }

            if (hideWhenChatHidden && BetterChat.chatHidden)
            {
                layout.ignoreLayout = true;
                uGUI.enabled = false;
            }
            else
            {
                uGUI.enabled = true;
                layout.ignoreLayout = false;
            }
        }

        void OnDestroy()
        {
            BetterChat.messageObjs.Remove(gameObject);
        }
    }
}