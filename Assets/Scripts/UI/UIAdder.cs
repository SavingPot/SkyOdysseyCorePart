using GameCore;
using GameCore.High;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SP.Tools.Unity;

namespace GameCore.UI
{
    public class UIAdder : MonoBehaviour
    {
        public Tools tools => Tools.instance;
        public ManagerAudio managerAudio => ManagerAudio.instance;
        public ManagerNetwork managerNetwork => ManagerNetwork.instance;
        public GM managerGame => GM.instance;

        public void ChangeMainUI(GameObject disappearObject, GameObject appearObject) => GameUI.ChangeMain(disappearObject, appearObject);

        public void ChangeMainUI(Transform disappearObject, Transform appearObject) => GameUI.ChangeMain(disappearObject, appearObject);

        public void ChangeMainUI(IRectTransform disappearObject, IRectTransform appearObject) => GameUI.ChangeMain(disappearObject, appearObject);

        public void UIDisappear(UIIdMessage ui) => UIDisappear(ui.rectTransform);

        public void UIDisappear(GameObject targetGameObject) => UIDisappear(targetGameObject.transform);

        public void UIDisappear(Transform trans) => GameUI.Disappear(trans);

        public void UIAppear(GameObject targetGameObject) => UIAppear(targetGameObject.transform);

        public void UIAppear(UIIdMessage ui) => UIAppear(ui.rectTransform);

        public void UIAppear(Transform trans) => GameUI.Appear(trans);

        #region 添加按钮
        public ButtonIdMessage AddButton(Vector4 positionCurrent, string buttonId)
        => AddButtonInternal(positionCurrent, buttonId, GameUI.canvas.transform, "ori:button");

        public ButtonIdMessage AddButton(Vector4 positionCurrent, string buttonId, Transform trans, string buttonSpriteId = "ori:button")
        => AddButtonInternal(positionCurrent, buttonId, trans, buttonSpriteId);

        public ButtonIdMessage AddButton(Vector4 positionCurrent, string buttonId, UIIdMessage panelIdMessage, string buttonSpriteId = "ori:button")
        => AddButtonInternal(positionCurrent, buttonId, panelIdMessage.transform, buttonSpriteId);

        public ButtonIdMessage AddButton(Vector4 positionCurrent, string buttonId, string panelId, string buttonSpriteId = "ori:button")
        => AddButtonInternal(positionCurrent, buttonId, IdMessageCenter.ComparePanelMessage(panelId).transform, buttonSpriteId);

        internal ButtonIdMessage AddButtonInternal(Vector4 positionCurrent, string buttonId, Transform trans, string buttonSpriteId = "ori:button")
        => GameUI.AddButtonInternal(positionCurrent, buttonId, trans, buttonSpriteId);
        #endregion

        #region 添加面板
        public static PanelIdMessage AddPanel(string panelId, IRectTransform irt, bool disable = false) => AddPanel(panelId, disable, irt.rectTransform);

        public static PanelIdMessage AddPanel(string panelId, Transform trans, bool disable = false) => AddPanel(panelId, disable, trans);

        public static PanelIdMessage AddPanel(string panelId, string parentId, bool disable = false) => AddPanel(panelId, disable, IdMessageCenter.ComparePanelMessage(parentId).transform);

        public static PanelIdMessage AddPanel(string panelId, GameObject go, bool disable = false) => AddPanel(panelId, disable, go.transform);

        public static PanelIdMessage AddPanel(string panelId, bool disable) => AddPanel(panelId, disable, null);

        public static PanelIdMessage AddPanel(string panelId, bool disable = false, Transform trans = null) => GameUI.AddPanel(panelId, disable, trans);
        #endregion

        #region 添加输入按钮
        public InputFieldIdMessage AddInputField(Vector4 positionCurrent, string inputFieldId, GameObject gameObject)
        => AddInputField(positionCurrent, inputFieldId, gameObject.transform);

        public InputFieldIdMessage AddInputField(Vector4 positionCurrent, string inputFieldId, UIIdMessage panelIdMessage)
        => AddInputField(positionCurrent, inputFieldId, panelIdMessage.transform);

        public InputFieldIdMessage AddInputField(Vector4 positionCurrent, string inputFieldId, string panelId)
        => AddInputField(positionCurrent, inputFieldId, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public InputFieldIdMessage AddInputField(Vector4 positionCurrent, string inputFieldId, Transform trans = null)
        => GameUI.AddInputField(positionCurrent, inputFieldId, trans);
        #endregion

        #region 添加图文按钮
        public ImageTextButtonIdMessage AddImageTextButton(Vector4 positionCurrent, string imageTextButtonId, GameObject gameObject)
        => AddImageTextButton(positionCurrent, imageTextButtonId, gameObject.transform);

        public ImageTextButtonIdMessage AddImageTextButton(Vector4 positionCurrent, string imageTextButtonId, UIIdMessage panelIdMessage)
        => AddImageTextButton(positionCurrent, imageTextButtonId, panelIdMessage.transform);

        public ImageTextButtonIdMessage AddImageTextButton(Vector4 positionCurrent, string imageTextButtonId, string panelId)
        => AddImageTextButton(positionCurrent, imageTextButtonId, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public ImageTextButtonIdMessage AddImageTextButton(Vector4 positionCurrent, string imageTextButtonId, Transform trans = null)
        => GameUI.AddImageTextButton(positionCurrent, imageTextButtonId, trans);
        #endregion

        #region 添加滑动条
        public SliderIdMessage AddSlider(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddSlider(positionCurrent, id, gameObject.transform);

        public SliderIdMessage AddSlider(Vector4 positionCurrent, string id, IRectTransform irt)
        => AddSlider(positionCurrent, id, irt.rectTransform);

        public SliderIdMessage AddSlider(Vector4 positionCurrent, string id, string panelId)
        => AddSlider(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public SliderIdMessage AddSlider(Vector4 positionCurrent, string id, Transform trans = null)
        => GameUI.AddSlider(positionCurrent, id, trans);
        #endregion

        #region 添加输入按钮
        public static InputButtonIdMessage AddInputButton(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddInputButton(positionCurrent, id, gameObject.transform);

        public static InputButtonIdMessage AddInputButton(Vector4 positionCurrent, string id, IRectTransform irt)
        => AddInputButton(positionCurrent, id, irt.rectTransform);

        public static InputButtonIdMessage AddInputButton(Vector4 positionCurrent, string id, string panelId)
        => AddInputButton(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static InputButtonIdMessage AddInputButton(Vector4 positionCurrent, string id, Transform trans = null)
        => GameUI.AddInputButton(positionCurrent, id, trans);
        #endregion

        #region 添加滚动视图
        public ScrollViewIdMessage AddScrollView(Vector4 positionCurrent, string scrollViewId, GameObject gameObject)
        => AddScrollView(positionCurrent, scrollViewId, gameObject.transform);

        public ScrollViewIdMessage AddScrollView(Vector4 positionCurrent, string scrollViewId, UIIdMessage panelIdMessage)
        => AddScrollView(positionCurrent, scrollViewId, panelIdMessage.transform);

        public ScrollViewIdMessage AddScrollView(Vector4 positionCurrent, string scrollViewId, string panelId)
        => AddScrollView(positionCurrent, scrollViewId, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public ScrollViewIdMessage AddScrollView(Vector4 positionCurrent, string scrollViewId, Transform trans = null)
        => GameUI.AddScrollView(positionCurrent, scrollViewId, trans);
        #endregion

        #region 添加图片
        public ImageIdMessage AddImage(Vector4 positionCurrent, string imageId, string spriteId, GameObject gameObject)
        => AddImage(positionCurrent, imageId, spriteId, gameObject.transform);

        public ImageIdMessage AddImage(Vector4 positionCurrent, string imageId, string spriteId, UIIdMessage panelIdMessage)
        => AddImage(positionCurrent, imageId, spriteId, panelIdMessage.transform);

        public ImageIdMessage AddImage(Vector4 positionCurrent, string imageId, string spriteId, string panelId)
        => AddImage(positionCurrent, imageId, spriteId, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public ImageIdMessage AddImage(Vector4 positionCurrent, string imageId, string spriteId, Transform trans = null)
        => GameUI.AddImage(positionCurrent, imageId, spriteId, trans);
        #endregion

        #region 添加原始图片
        public RawImageIdMessage AddRawImage(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddRawImage(positionCurrent, id, gameObject.transform);

        public RawImageIdMessage AddRawImage(Vector4 positionCurrent, string id, UIIdMessage panelIdMessage)
        => AddRawImage(positionCurrent, id, panelIdMessage.transform);

        public RawImageIdMessage AddRawImage(Vector4 positionCurrent, string id, string panelId)
        => AddRawImage(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public RawImageIdMessage AddRawImage(Vector4 positionCurrent, string id, Transform trans = null) => GameUI.AddRawImage(positionCurrent, id, trans);
        #endregion

        #region 添加文本
        public TextIdMessage AddText(Vector4 positionCurrent, string textId, GameObject gameObject)
        => AddText(positionCurrent, textId, gameObject.transform);

        public TextIdMessage AddText(Vector4 positionCurrent, string textId, UIIdMessage panelIdMessage)
        => AddText(positionCurrent, textId, panelIdMessage.transform);

        public TextIdMessage AddText(Vector4 positionCurrent, string textId, string panelId)
        => AddText(positionCurrent, textId, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public TextIdMessage AddText(Vector4 positionCurrent, string textId, Transform trans = null)
        => GameUI.AddText(positionCurrent, textId, trans);
        #endregion

        #region 添加开关
        public static ToggleIdMessage AddToggle(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddToggle(positionCurrent, id, gameObject.transform);

        public static ToggleIdMessage AddToggle(Vector4 positionCurrent, string id, UIIdMessage panelIdMessage)
        => AddToggle(positionCurrent, id, panelIdMessage.transform);

        public static ToggleIdMessage AddToggle(Vector4 positionCurrent, string id, string panelId)
        => AddToggle(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static ToggleIdMessage AddToggle(Vector4 positionCurrent, string id, Transform trans = null)
        => GameUI.AddToggle(positionCurrent, id, trans);
        #endregion
    }
}
