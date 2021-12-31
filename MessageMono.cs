using TMPro;
using UnboundLib.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace BetterChat
{
    public class MessageMono : MonoBehaviour
    {
        private TimeSince timeSinceAwake;
        public bool hideWhenChatHidden;

        private TextMeshProUGUI uGUI;
        private LayoutElement layout;

        private void Awake()
        {
            timeSinceAwake = 0;
            uGUI = GetComponent<TextMeshProUGUI>();
            layout = GetComponent<LayoutElement>();
            BetterChat.messageObjs.Add(gameObject);
        }

        private void Update()
        {
            if (timeSinceAwake > BetterChat.TimeBeforeTextGone)
            {
                hideWhenChatHidden = true;
                BetterChat.messageObjs.Remove(gameObject);
            }

            if (hideWhenChatHidden && BetterChat.chatHidden)
            {
                uGUI.enabled = false;
                layout.ignoreLayout = true;
            }
            else
            {
                layout.ignoreLayout = false;
                uGUI.enabled = true;
            }
        }

        void OnDestroy()
        {
            BetterChat.messageObjs.Remove(gameObject);
        }
    }
}