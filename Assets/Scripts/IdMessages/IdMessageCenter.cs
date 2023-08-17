using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class IdentityCenter
    {
        public static readonly List<IdentityComponent> messages = new();
        public static readonly List<UIIdentity> uiMessages = new();
        public static readonly List<ButtonIdentity> buttonMessages = new();
        public static readonly List<PanelIdentity> panelMessages = new();
        public static readonly List<InputFieldIdentity> inputFieldMessages = new();
        public static readonly List<ImageIdentity> imageMessages = new();
        public static readonly List<RawImageIdentity> rawImageMessages = new();
        public static readonly List<SliderIdentity> sliderMessages = new();
        public static readonly List<TextIdentity> textMessages = new();
        public static readonly List<ToggleIdentity> toggleMessages = new();
        public static readonly List<ImageTextButtonIdentity> imageTextButtonMessage = new();
        public static readonly List<InputButtonIdentity> inputButtonMessage = new();
        public static readonly List<ScrollViewIdentity> scrollViewMessage = new();

        public static IdentityComponent CompareMessage(string id) => CompareMessage(messages, id);
        public static UIIdentity CompareUIMessage(string id) => CompareMessage(uiMessages, id);
        public static ButtonIdentity CompareButtonMessage(string id) => CompareMessage(buttonMessages, id);
        public static PanelIdentity ComparePanelMessage(string id) => CompareMessage(panelMessages, id);
        public static InputFieldIdentity CompareInputFieldMessage(string id) => CompareMessage(inputFieldMessages, id);
        public static ImageIdentity CompareImageMessage(string id) => CompareMessage(imageMessages, id);
        public static RawImageIdentity CompareRawImageMessage(string id) => CompareMessage(rawImageMessages, id);
        public static SliderIdentity CompareSliderMessage(string id) => CompareMessage(sliderMessages, id);
        public static TextIdentity CompareTextMessage(string id) => CompareMessage(textMessages, id);
        public static ToggleIdentity CompareToggleMessage(string id) => CompareMessage(toggleMessages, id);
        public static ImageTextButtonIdentity CompareImageTextButtonMessage(string id) => CompareMessage(imageTextButtonMessage, id);
        public static ImageTextButtonIdentity CompareInputButtonMessage(string id) => CompareMessage(imageTextButtonMessage, id);
        public static ScrollViewIdentity CompareScrollViewMessage(string id) => CompareMessage(scrollViewMessage, id);


        [RuntimeInitializeOnLoadMethod]
        private static void BindMethod()
        {
            MethodAgent.AddUpdate(Update);
        }

        private static void Update()
        {
            // CheckMessages(messages);
            // CheckMessages(uiMessages);
            // CheckMessages(buttonMessages);
            // CheckMessages(panelMessages);
            // CheckMessages(inputFieldMessages);
            // CheckMessages(imageMessages);
            // CheckMessages(rawImageMessages);
            // CheckMessages(sliderMessages);
            // CheckMessages(textMessages);
            // CheckMessages(toggleMessages);
            // CheckMessages(inputButtonMessage);
            // CheckMessages(imageTextButtonMessage);
            // CheckMessages(scrollViewMessage);
        }

        public static T CompareMessage<T>(List<T> idMessages, string messageId) where T : IdentityComponent
        {
            var messages = idMessages.Where(p => p.id == messageId).ToArray();

            if (messages.Length > 0)
                return messages[0];

            return null;
        }

        // public static void CheckMessages<T>(List<T> messages) where T : IdMessage
        // {
        //     for (int i = 0; i < messages.Count; i++)
        //         if (!messages[i])
        //             messages.RemoveAt(i);
        // }

        public static void Remove(IdentityComponent msg)
        {
            messages.Remove(msg);

            if (msg is UIIdentity)
            {
                if (uiMessages.Remove((UIIdentity)msg))
                {
                    if (msg is ButtonIdentity)
                    {
                        if (buttonMessages.Remove((ButtonIdentity)msg))
                            return;
                    }
                    else if (msg is PanelIdentity)
                    {
                        if (panelMessages.Remove((PanelIdentity)msg))
                            return;
                    }

                    else if (msg is InputFieldIdentity)
                    {
                        if (inputFieldMessages.Remove((InputFieldIdentity)msg))
                            return;
                    }

                    else if (msg is ImageIdentity)
                    {
                        if (imageMessages.Remove((ImageIdentity)msg))
                            return;
                    }

                    else if (msg is RawImageIdentity)
                    {
                        if (rawImageMessages.Remove((RawImageIdentity)msg))
                            return;
                    }

                    else if (msg is SliderIdentity)
                    {
                        if (sliderMessages.Remove((SliderIdentity)msg))
                            return;
                    }

                    else if (msg is TextIdentity)
                    {
                        if (textMessages.Remove((TextIdentity)msg))
                            return;
                    }

                    else if (msg is ToggleIdentity)
                    {
                        if (toggleMessages.Remove((ToggleIdentity)msg))
                            return;
                    }

                    else if (msg is InputButtonIdentity)
                    {
                        if (inputButtonMessage.Remove((InputButtonIdentity)msg))
                            return;
                    }

                    else if (msg is ImageTextButtonIdentity)
                    {
                        if (imageTextButtonMessage.Remove((ImageTextButtonIdentity)msg))
                            return;
                    }

                    else if (msg is ScrollViewIdentity)
                    {
                        if (scrollViewMessage.Remove((ScrollViewIdentity)msg))
                            return;
                    }

                    return;
                }
            }

            Debug.LogWarning($"删除ID消息 {msg.id} 时失败");
        }
    }
}
