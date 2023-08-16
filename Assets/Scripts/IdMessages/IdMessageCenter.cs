using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class IdMessageCenter
    {
        public static readonly List<IdMessage> messages = new();
        public static readonly List<UIIdMessage> uiMessages = new();
        public static readonly List<ButtonIdMessage> buttonMessages = new();
        public static readonly List<PanelIdMessage> panelMessages = new();
        public static readonly List<InputFieldIdMessage> inputFieldMessages = new();
        public static readonly List<ImageIdMessage> imageMessages = new();
        public static readonly List<RawImageIdMessage> rawImageMessages = new();
        public static readonly List<SliderIdMessage> sliderMessages = new();
        public static readonly List<TextIdMessage> textMessages = new();
        public static readonly List<ToggleIdMessage> toggleMessages = new();
        public static readonly List<ImageTextButtonIdMessage> imageTextButtonMessage = new();
        public static readonly List<InputButtonIdMessage> inputButtonMessage = new();
        public static readonly List<ScrollViewIdMessage> scrollViewMessage = new();

        public static IdMessage CompareMessage(string id) => CompareMessage(messages, id);
        public static UIIdMessage CompareUIMessage(string id) => CompareMessage(uiMessages, id);
        public static ButtonIdMessage CompareButtonMessage(string id) => CompareMessage(buttonMessages, id);
        public static PanelIdMessage ComparePanelMessage(string id) => CompareMessage(panelMessages, id);
        public static InputFieldIdMessage CompareInputFieldMessage(string id) => CompareMessage(inputFieldMessages, id);
        public static ImageIdMessage CompareImageMessage(string id) => CompareMessage(imageMessages, id);
        public static RawImageIdMessage CompareRawImageMessage(string id) => CompareMessage(rawImageMessages, id);
        public static SliderIdMessage CompareSliderMessage(string id) => CompareMessage(sliderMessages, id);
        public static TextIdMessage CompareTextMessage(string id) => CompareMessage(textMessages, id);
        public static ToggleIdMessage CompareToggleMessage(string id) => CompareMessage(toggleMessages, id);
        public static ImageTextButtonIdMessage CompareImageTextButtonMessage(string id) => CompareMessage(imageTextButtonMessage, id);
        public static ImageTextButtonIdMessage CompareInputButtonMessage(string id) => CompareMessage(imageTextButtonMessage, id);
        public static ScrollViewIdMessage CompareScrollViewMessage(string id) => CompareMessage(scrollViewMessage, id);


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

        public static T CompareMessage<T>(List<T> idMessages, string messageId) where T : IdMessage
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

        public static void Remove(IdMessage msg)
        {
            messages.Remove(msg);

            if (msg is UIIdMessage)
            {
                if (uiMessages.Remove((UIIdMessage)msg))
                {
                    if (msg is ButtonIdMessage)
                    {
                        if (buttonMessages.Remove((ButtonIdMessage)msg))
                            return;
                    }
                    else if (msg is PanelIdMessage)
                    {
                        if (panelMessages.Remove((PanelIdMessage)msg))
                            return;
                    }

                    else if (msg is InputFieldIdMessage)
                    {
                        if (inputFieldMessages.Remove((InputFieldIdMessage)msg))
                            return;
                    }

                    else if (msg is ImageIdMessage)
                    {
                        if (imageMessages.Remove((ImageIdMessage)msg))
                            return;
                    }

                    else if (msg is RawImageIdMessage)
                    {
                        if (rawImageMessages.Remove((RawImageIdMessage)msg))
                            return;
                    }

                    else if (msg is SliderIdMessage)
                    {
                        if (sliderMessages.Remove((SliderIdMessage)msg))
                            return;
                    }

                    else if (msg is TextIdMessage)
                    {
                        if (textMessages.Remove((TextIdMessage)msg))
                            return;
                    }

                    else if (msg is ToggleIdMessage)
                    {
                        if (toggleMessages.Remove((ToggleIdMessage)msg))
                            return;
                    }

                    else if (msg is InputButtonIdMessage)
                    {
                        if (inputButtonMessage.Remove((InputButtonIdMessage)msg))
                            return;
                    }

                    else if (msg is ImageTextButtonIdMessage)
                    {
                        if (imageTextButtonMessage.Remove((ImageTextButtonIdMessage)msg))
                            return;
                    }

                    else if (msg is ScrollViewIdMessage)
                    {
                        if (scrollViewMessage.Remove((ScrollViewIdMessage)msg))
                            return;
                    }

                    return;
                }
            }

            Debug.LogWarning($"删除ID消息 {msg.id} 时失败");
        }
    }
}
