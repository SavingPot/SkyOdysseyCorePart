using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class IdentityCenter
    {
        public static readonly List<IdentityComponent> identities = new();
        public static readonly List<UIIdentity> uiIdentities = new();
        public static readonly List<ButtonIdentity> buttonIdentities = new();
        public static readonly List<PanelIdentity> panelIdentities = new();
        public static readonly List<InputFieldIdentity> inputFieldIdentities = new();
        public static readonly List<ImageIdentity> imageIdentities = new();
        public static readonly List<RawImageIdentity> rawImageIdentities = new();
        public static readonly List<SliderIdentity> sliderIdentities = new();
        public static readonly List<TextIdentity> textIdentities = new();
        public static readonly List<ToggleIdentity> toggleIdentities = new();
        public static readonly List<ImageTextButtonIdentity> imageTextButtonIdentities = new();
        public static readonly List<InputButtonIdentity> inputButtonIdentities = new();
        public static readonly List<ScrollViewIdentity> scrollViewIdentities = new();


        public static UIIdentity CompareUIIdentity(string id) => CompareIdentity(uiIdentities, id);
        public static ButtonIdentity CompareButtonIdentity(string id) => CompareIdentity(buttonIdentities, id);
        public static PanelIdentity ComparePanelIdentity(string id) => CompareIdentity(panelIdentities, id);
        public static InputFieldIdentity CompareInputFieldIdentity(string id) => CompareIdentity(inputFieldIdentities, id);
        public static ImageIdentity CompareImageIdentity(string id) => CompareIdentity(imageIdentities, id);
        public static RawImageIdentity CompareRawImageIdentity(string id) => CompareIdentity(rawImageIdentities, id);
        public static SliderIdentity CompareSliderIdentity(string id) => CompareIdentity(sliderIdentities, id);
        public static TextIdentity CompareTextIdentity(string id) => CompareIdentity(textIdentities, id);
        public static ToggleIdentity CompareToggleIdentity(string id) => CompareIdentity(toggleIdentities, id);
        public static ImageTextButtonIdentity CompareImageTextButtonIdentity(string id) => CompareIdentity(imageTextButtonIdentities, id);
        public static ImageTextButtonIdentity CompareInputButtonIdentity(string id) => CompareIdentity(imageTextButtonIdentities, id);
        public static ScrollViewIdentity CompareScrollViewIdentity(string id) => CompareIdentity(scrollViewIdentities, id);

        public static IdentityComponent CompareIdentity(string id) => CompareIdentity(identities, id);
        public static T CompareIdentity<T>(List<T> idMessages, string messageId) where T : IdentityComponent
        {
            var messages = idMessages.Where(p => p.id == messageId).ToArray();

            if (messages.Length > 0)
                return messages[0];

            return null;
        }


        public static void Remove(IdentityComponent identity)
        {
            identities.Remove(identity);

            if (identity is UIIdentity ui)
            {
                if (uiIdentities.Remove(ui))
                {
                    if (identity is ButtonIdentity button)
                    {
                        if (buttonIdentities.Remove(button))
                            return;
                    }
                    else if (identity is PanelIdentity panel)
                    {
                        if (panelIdentities.Remove(panel))
                            return;
                    }

                    else if (identity is InputFieldIdentity inputField)
                    {
                        if (inputFieldIdentities.Remove(inputField))
                            return;
                    }

                    else if (identity is ImageIdentity image)
                    {
                        if (imageIdentities.Remove(image))
                            return;
                    }

                    else if (identity is RawImageIdentity rawImage)
                    {
                        if (rawImageIdentities.Remove(rawImage))
                            return;
                    }

                    else if (identity is SliderIdentity slider)
                    {
                        if (sliderIdentities.Remove(slider))
                            return;
                    }

                    else if (identity is TextIdentity text)
                    {
                        if (textIdentities.Remove(text))
                            return;
                    }

                    else if (identity is ToggleIdentity toggle)
                    {
                        if (toggleIdentities.Remove(toggle))
                            return;
                    }

                    else if (identity is InputButtonIdentity inputButton)
                    {
                        if (inputButtonIdentities.Remove(inputButton))
                            return;
                    }

                    else if (identity is ImageTextButtonIdentity imageTextButton)
                    {
                        if (imageTextButtonIdentities.Remove(imageTextButton))
                            return;
                    }

                    else if (identity is ScrollViewIdentity scrollView)
                    {
                        if (scrollViewIdentities.Remove(scrollView))
                            return;
                    }

                    return;
                }
            }

            Debug.LogWarning($"删除ID消息 {identity.id} 时失败");
        }
    }
}
