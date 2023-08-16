using Cysharp.Threading.Tasks;
using DG.Tweening;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GameCore.UI
{
    public static class GameUI
    {
        public static string defaultTextId = "ori:zh_cn";

        public class PanelPage
        {
            public PanelPage last;
            public UIIdMessage ui;
            public DisappearType disappearType;
            public AppearType appearType;

            public PanelPage(PanelPage lastPage, UIIdMessage ui, DisappearType disappearType, AppearType appearType)
            {
                this.last = lastPage;
                this.ui = ui;
                this.disappearType = disappearType;
                this.appearType = appearType;
            }
        }

        public enum DisappearType : byte
        {
            PositionUpToDown,
            PositionDownToUp,
            PositionLeftToRight,
            PositionRightToLeft,
            Alpha,
            ScaleX,
            ScaleY,
        }

        public enum AppearType : byte
        {
            PositionUpToDown,
            PositionDownToUp,
            PositionLeftToRight,
            PositionRightToLeft,
            Alpha,
            ScaleX,
            ScaleY,
        }

        public static PanelPage page { get; private set; }

        public static void SetPage(UIIdMessage value, DisappearType disappearType = DisappearType.PositionDownToUp, AppearType appearType = AppearType.PositionUpToDown)
        {
            //播放消失
            if (page != null)
            {
                if (page.ui)
                    Disappear(page.ui, page.disappearType);
            }

            //设置页面
            page = new(page, value, disappearType, appearType);

            //播放出现
            if (page.ui)
                Appear(page.ui, page.appearType);//, page.appearType);
        }

        public static void SetPageBack()
        {
            if (page == null)
            {
                Debug.LogError("页面值为空");
                return;
            }

            if (page.last == null)
            {
                Debug.LogError("上个页面值为空");
                return;
            }

            SetPage(page.last.ui);
        }

        public static float disAndAppearingWaitTime = 0.1f;
        public static readonly List<Transform> disAndAppearingTransforms = new();
        public static GameObject uiWantToSelect;

        public static Action<Transform> BeforeUIDisappear = _ => { };
        public static Action<Transform> AfterUIDisappear = _ => { };

        public static Action<Transform> BeforeUIAppear = _ => { };
        public static Action<Transform> AfterUIAppear = _ => { };

        public static Action<Graphic> BeforeFadeOut = _ => { };
        public static Action<Graphic> AfterFadeOut = _ => { };

        public static Action<Graphic> BeforeFadeIn = _ => { };
        public static Action<Graphic> AfterFadeIn = _ => { };



        #region UI 示例获取
        private static EventSystem _eventSystem;
        private static InputSystemUIInputModule _inputModule;
        private static Canvas _canvas;
        private static RectTransform _canvasRT;
        private static CanvasScaler _canvasScaler;
        private static Canvas _worldSpaceCanvas;
        private static RectTransform _worldSpaceCanvasRT;
        private static CanvasScaler _worldSpaceCanvasScaler;

        public static EventSystem eventSystem { get { if (!_eventSystem) _eventSystem = EventSystem.current; return _eventSystem; } }
        public static InputSystemUIInputModule engineInputMod
        {
            get
            {
                if (!_inputModule)
                    _inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();

                return _inputModule;
            }
        }
        public static Canvas canvas { get { if (!_canvas) _canvas = GameObject.Find("/Canvas").GetComponent<Canvas>(); return _canvas; } }
        public static RectTransform canvasRT { get { if (!_canvasRT) _canvasRT = _canvas.GetComponent<RectTransform>(); return _canvasRT; } }
        public static CanvasScaler canvasScaler { get { if (!_canvasScaler) _canvasScaler = canvas.GetComponent<CanvasScaler>(); return _canvasScaler; } }
        public static Canvas worldSpaceCanvas { get { if (!_worldSpaceCanvas) _worldSpaceCanvas = GameObject.Find("/WSCanvas").GetComponent<Canvas>(); return _worldSpaceCanvas; } }
        public static RectTransform worldSpaceCanvasRT { get { if (!_worldSpaceCanvasRT) _worldSpaceCanvasRT = _worldSpaceCanvas.GetComponent<RectTransform>(); return _worldSpaceCanvasRT; } }
        public static CanvasScaler worldSpaceCanvasScaler { get { if (!_worldSpaceCanvasScaler) _worldSpaceCanvasScaler = canvas.GetComponent<CanvasScaler>(); return _worldSpaceCanvasScaler; } }
        #endregion



        #region 文本获取
        private static FinalLang _defaultLang;
        public static FinalLang defaultLang
        {
            get
            {
                _defaultLang ??= ModFactory.CompareFinalDatumText(defaultTextId);
                return _defaultLang;
            }
            set => _currentLang = value;
        }
        private static FinalLang _currentLang;
        public static FinalLang currentLang
        {
            get
            {
                if (_currentLang == null || _currentLang.id != GFiles.settings.langId)
                    SetCurrentLang();

                return _currentLang;
            }
            set => _currentLang = value;
        }
        public static FinalLang SetCurrentLang()
        {
            if (GFiles.settings.langId != null)
                _currentLang = ModFactory.CompareFinalDatumText(GFiles.settings.langId);
            if (_currentLang == null)
                _currentLang = defaultLang;

            return _currentLang;
        }

        public static GameLang_Text CompareText(string id)
        {
            if (currentLang.TryCompareText(id, out GameLang_Text cText))
            {
                return cText;
            }
            else
            {
                if (defaultLang.TryCompareText(id, out GameLang_Text dText))
                {
                    return dText;
                }

                foreach (var data in ModFactory.finalTextData)
                {
                    if (data.id != currentLang.id && data.id != defaultLang.id)
                    {
                        if (data.TryCompareText(id, out GameLang_Text aText))
                        {
                            return aText;
                        }
                    }
                }

                return new() { id = id, text = id };
            }
        }
        #endregion



        public static Vector2 ScreenUIPosInConstantCanvas(Vector2 anchoredPos, CanvasScaler constantCanvasScaler)
        {
            // Vector2 pos = anchoredPos * constantCanvasScaler.referenceResolution / Tools.resolution;
            // return pos - constantCanvasScaler.referenceResolution / 2;

            return new(anchoredPos.x * constantCanvasScaler.referenceResolution.x / Tools.resolution.x - constantCanvasScaler.referenceResolution.x / 2,
                        anchoredPos.y * constantCanvasScaler.referenceResolution.y / Tools.resolution.y - constantCanvasScaler.referenceResolution.y / 2);
        }



        private static T InstantiateIdMsg<T>(T original, string id) where T : IdMessage
        {
            T msg = GameObject.Instantiate<T>(original);
            msg.SetID(id);

            return msg;
        }



        #region 添加 UI

        #region 添加图片
        public static ImageIdMessage AddImage(Vector4 positionCurrent, string id, string spriteId, GameObject gameObject)
        => AddImage(positionCurrent, id, spriteId, gameObject.transform);

        public static ImageIdMessage AddImage(Vector4 positionCurrent, string id, string spriteId, UIIdMessage panelIdMessage)
        => AddImage(positionCurrent, id, spriteId, panelIdMessage.transform);

        public static ImageIdMessage AddImage(Vector4 positionCurrent, string id, string spriteId, string panelId)
        => AddImage(positionCurrent, id, spriteId, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static ImageIdMessage AddImage(Vector4 positionCurrent, string id, string spriteId, Transform trans = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.imagePrefab, id);

            msg.rectTransform.SetParent(trans == null ? canvas.transform : trans);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;

            if (!spriteId.IsNullOrWhiteSpace())
                msg.image.sprite = ModFactory.CompareTexture(spriteId)?.sprite;

            return msg;
        }
        #endregion

        #region 添加原始图片
        public static RawImageIdMessage AddRawImage(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddRawImage(positionCurrent, id, gameObject.transform);

        public static RawImageIdMessage AddRawImage(Vector4 positionCurrent, string id, UIIdMessage panelIdMessage)
        => AddRawImage(positionCurrent, id, panelIdMessage.transform);

        public static RawImageIdMessage AddRawImage(Vector4 positionCurrent, string id, string panelId)
        => AddRawImage(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static RawImageIdMessage AddRawImage(Vector4 positionCurrent, string id, Transform trans = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.rawImagePrefab, id);

            msg.rectTransform.SetParent(trans == null ? canvas.transform : trans);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;

            return msg;
        }
        #endregion

        #region 添加滚动视图
        public static ScrollViewIdMessage AddScrollView(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddScrollView(positionCurrent, id, gameObject.transform);

        public static ScrollViewIdMessage AddScrollView(Vector4 positionCurrent, string id, UIIdMessage panelIdMessage)
        => AddScrollView(positionCurrent, id, panelIdMessage.transform);

        public static ScrollViewIdMessage AddScrollView(Vector4 positionCurrent, string id, string panelId)
        => AddScrollView(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static ScrollViewIdMessage AddScrollView(Vector4 positionCurrent, string id, Transform trans = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.scrollViewPrefab, id);

            msg.rectTransform.SetParent(trans == null ? canvas.transform : trans);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            return msg;
        }
        #endregion

        #region 添加文本
        public static TextIdMessage AddText(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddText(positionCurrent, id, gameObject.transform);

        public static TextIdMessage AddText(Vector4 positionCurrent, string id, UIIdMessage panelIdMessage)
        => AddText(positionCurrent, id, panelIdMessage.transform);

        public static TextIdMessage AddText(Vector4 positionCurrent, string id, string panelId)
        => AddText(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static TextIdMessage AddText(Vector4 positionCurrent, string id, Transform trans = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.textPrefab, id);

            msg.rectTransform.SetParent(trans == null ? canvas.transform : trans);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            return msg;
        }
        #endregion

        #region 添加开关
        public static ToggleIdMessage AddToggle(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddToggle(positionCurrent, id, gameObject.transform);

        public static ToggleIdMessage AddToggle(Vector4 positionCurrent, string id, UIIdMessage panelIdMessage)
        => AddToggle(positionCurrent, id, panelIdMessage.transform);

        public static ToggleIdMessage AddToggle(Vector4 positionCurrent, string id, string panelId)
        => AddToggle(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static ToggleIdMessage AddToggle(Vector4 positionCurrent, string id, Transform trans = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.togglePrefab, id);

            msg.rectTransform.SetParent(trans == null ? canvas.transform : trans);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            msg.textBg.image.sprite = ModFactory.CompareTexture("ori:button_flat").sprite;
            msg.bg.image.sprite = ModFactory.CompareTexture("ori:square_button_low").sprite;
            RefreshIt(msg.toggle.isOn);
            void RefreshIt(bool b)
            {
                if (b)
                    msg.checkmark.image.sprite = ModFactory.CompareTexture("ori:enabled").sprite;
                else
                    msg.checkmark.image.sprite = ModFactory.CompareTexture("ori:disabled").sprite;
            }
            msg.AddMethod(RefreshIt);
            msg.AddMethod(_ => GAudio.Play(AudioID.Button));

            return msg;
        }
        #endregion

        #region 添加输入框
        public static InputFieldIdMessage AddInputField(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddInputField(positionCurrent, id, gameObject.transform);

        public static InputFieldIdMessage AddInputField(Vector4 positionCurrent, string id, UIIdMessage panelIdMessage)
        => AddInputField(positionCurrent, id, panelIdMessage.transform);

        public static InputFieldIdMessage AddInputField(Vector4 positionCurrent, string id, string panelId)
        => AddInputField(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static InputFieldIdMessage AddInputField(Vector4 positionCurrent, string id, Transform trans = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.inputFieldPrefab, id);

            msg.image.sprite = ModFactory.CompareTexture("ori:button").sprite;
            msg.rectTransform.SetParent(trans == null ? canvas.transform : trans);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            return msg;
        }
        #endregion

        #region 添加图文按钮
        public static ImageTextButtonIdMessage AddImageTextButton(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddImageTextButton(positionCurrent, id, gameObject.transform);

        public static ImageTextButtonIdMessage AddImageTextButton(Vector4 positionCurrent, string id, UIIdMessage panelIdMessage)
        => AddImageTextButton(positionCurrent, id, panelIdMessage.transform);

        public static ImageTextButtonIdMessage AddImageTextButton(Vector4 positionCurrent, string id, string panelId)
        => AddImageTextButton(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static ImageTextButtonIdMessage AddImageTextButton(Vector4 positionCurrent, string id, Transform trans = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.imageTextButtonPrefab, id);

            msg.rectTransform.SetParent(trans == null ? canvas.transform : trans);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            msg.button.image.sprite = ModFactory.CompareTexture("ori:button_flat").sprite;
            msg.AddMethod(() => GAudio.Play(AudioID.Button));

            return msg;
        }
        #endregion

        #region 添加输入按钮
        public static InputButtonIdMessage AddInputButton(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddInputButton(positionCurrent, id, gameObject.transform);

        public static InputButtonIdMessage AddInputButton(Vector4 positionCurrent, string id, IRectTransform irt)
        => AddInputButton(positionCurrent, id, irt.rectTransform);

        public static InputButtonIdMessage AddInputButton(Vector4 positionCurrent, string id, string panelId)
        => AddInputButton(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static InputButtonIdMessage AddInputButton(Vector4 positionCurrent, string id, Transform trans = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.inputButtonPrefab, id);

            msg.rt.SetParent(trans == null ? canvas.transform : trans);
            msg.rt.localPosition = Vector2.zero;
            msg.rt.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rt.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rt.localScale = Vector2.one;
            msg.button.image.sprite = ModFactory.CompareTexture("ori:square_button").sprite;
            msg.field.image.sprite = ModFactory.CompareTexture("ori:button_flat").sprite;
            msg.AddMethod(() => GAudio.Play(AudioID.Button));

            return msg;
        }
        #endregion

        #region 添加滑动条
        public static SliderIdMessage AddSlider(Vector4 positionCurrent, string id, GameObject gameObject)
        => AddSlider(positionCurrent, id, gameObject.transform);

        public static SliderIdMessage AddSlider(Vector4 positionCurrent, string id, IRectTransform irt)
        => AddSlider(positionCurrent, id, irt.rectTransform);

        public static SliderIdMessage AddSlider(Vector4 positionCurrent, string id, string panelId)
        => AddSlider(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform);

        public static SliderIdMessage AddSlider(Vector4 positionCurrent, string id, Transform trans = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.sliderPrefab, id);

            msg.rt.SetParent(trans ? trans : canvas.transform);
            msg.rt.localPosition = Vector2.zero;
            msg.rt.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rt.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rt.localScale = Vector2.one;

            msg.bgImage.image.sprite = ModFactory.CompareTexture("ori:button_flat").sprite;
            msg.fillImage.image.sprite = ModFactory.CompareTexture("ori:square_button_flat").sprite;
            msg.handleImage.image.sprite = ModFactory.CompareTexture("ori:square_button_flat").sprite;

            return msg;
        }
        #endregion

        #region 添加面板
        public static PanelIdMessage AddPanel(string id) => AddPanel(id, false, null);

        public static PanelIdMessage AddPanel(string id, IRectTransform irt) => AddPanel(id, false, irt.rectTransform);
        public static PanelIdMessage AddPanel(string id, IRectTransform irt, bool disable) => AddPanel(id, disable, irt.rectTransform);

        public static PanelIdMessage AddPanel(string id, string parentId) => AddPanel(id, false, IdMessageCenter.ComparePanelMessage(parentId).transform);
        public static PanelIdMessage AddPanel(string id, string parentId, bool disable) => AddPanel(id, disable, IdMessageCenter.ComparePanelMessage(parentId).transform);

        public static PanelIdMessage AddPanel(string id, GameObject go) => AddPanel(id, false, go.transform);
        public static PanelIdMessage AddPanel(string id, GameObject go, bool disable) => AddPanel(id, disable, go.transform);

        public static PanelIdMessage AddPanel(string id, Transform trans) => AddPanel(id, false, trans);

        public static PanelIdMessage AddPanel(string id, bool disable, Transform trans)
        {
            var msg = InstantiateIdMsg(GInit.instance.panelPrefab, id);

            msg.rt.SetParent(trans ? trans : canvas.transform);
            msg.rt.anchorMin = Vector2.zero;
            msg.rt.anchorMax = Vector2.one;
            msg.rt.localScale = Vector2.one;
            msg.ap = Vector2.zero;
            msg.rt.sizeDelta = Vector2.zero;

            if (disable)
                msg.gameObject.SetActive(false);

            return msg;
        }
        #endregion

        #region 添加按钮
        public static ButtonIdMessage AddButton(Vector4 positionCurrent, string id)
        => AddButtonInternal(positionCurrent, id, canvas.transform, "ori:button");

        public static ButtonIdMessage AddButton(Vector4 positionCurrent, string id, Transform trans, string buttonSpriteId = "ori:button")
        => AddButtonInternal(positionCurrent, id, trans, buttonSpriteId);

        public static ButtonIdMessage AddButton(Vector4 positionCurrent, string id, UIIdMessage panelIdMessage, string buttonSpriteId = "ori:button")
        => AddButtonInternal(positionCurrent, id, panelIdMessage.transform, buttonSpriteId);

        public static ButtonIdMessage AddButton(Vector4 positionCurrent, string id, string panelId, string buttonSpriteId = "ori:button")
        => AddButtonInternal(positionCurrent, id, IdMessageCenter.ComparePanelMessage(panelId).transform, buttonSpriteId);

        static internal ButtonIdMessage AddButtonInternal(Vector4 positionCurrent, string id, Transform trans, string buttonSpriteId = "ori:button")
        {
            var msg = InstantiateIdMsg(GInit.instance.buttonPrefab, id);

            msg.rectTransform.SetParent(trans);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            msg.image.sprite = ModFactory.CompareTexture(buttonSpriteId).sprite;
            msg.AddMethod(() => GAudio.Play(AudioID.Button));

            return msg;
        }
        #endregion

        public static (ImageIdMessage, ImageIdMessage, ImageIdMessage, ImageIdMessage, ImageIdMessage, ImageIdMessage, ImageIdMessage, ImageIdMessage) GenerateSkinShow(PlayerSkin skin, Transform parent)
        {
            float bodyWide = 20;
            Vector2 bodySD = new(bodyWide, bodyWide / skin.body.texture.width * skin.body.texture.height);
            Vector2 headSD = new(bodySD.x * 1.15f, bodySD.x * 1.3f);
            Vector2 rightArmSD = new (bodySD.y * skin.rightArm.texture.width / skin.rightArm.texture.height, bodySD.y);
            Vector2 leftArmSD = new (bodySD.y * skin.leftArm.texture.width / skin.leftArm.texture.height, bodySD.y);
            Vector2 rightLegSD = new(bodySD.x / 2, bodySD.x / 2 / skin.rightLeg.texture.width * skin.rightLeg.texture.height);
            Vector2 leftLegSD = new(bodySD.x / 2, bodySD.x / 2 / skin.leftLeg.texture.width * skin.leftLeg.texture.height);
            Vector2 rightFootSD = new(rightLegSD.x * 1.35f, rightLegSD.x * 1.35f / skin.rightFoot.texture.width * skin.rightFoot.texture.height);
            Vector2 leftFootSD = new(leftLegSD.x * 1.35f, leftLegSD.x * 1.35f / skin.leftFoot.texture.width * skin.leftFoot.texture.height);

            var body = InitI("body", UPC.middle, skin.body, BodyPartType.Body, bodySD, Vector2.zero, parent);
            var head = InitI("head", UPC.up, skin.head, BodyPartType.Head, headSD, new(-0.8f, headSD.y / 2 - 2.5f), body.transform);
            var rightArm = InitI("rightArm", UPC.upperLeft, skin.rightArm, BodyPartType.RightArm, rightArmSD, new(rightArmSD.x / 10, -rightArmSD.y / 2 - 3f), body.transform);
            var leftArm = InitI("leftArm", UPC.upperRight, skin.leftArm, BodyPartType.LeftArm, leftArmSD, new(-leftArmSD.x / 10, -leftArmSD.y / 2 - 3f), body.transform);
            var rightLeg = InitI("rightLeg", UPC.lowerLeft, skin.rightLeg, BodyPartType.RightLeg, rightLegSD, new(rightLegSD.x / 2, -rightLegSD.y / 2), body.transform);
            var leftLeg = InitI("leftLeg", UPC.lowerRight, skin.leftLeg, BodyPartType.LeftLeg, leftLegSD, new(-leftLegSD.x / 2, -leftLegSD.y / 2), body.transform);
            var rightFoot = InitI("rightFoot", UPC.down, skin.rightFoot, BodyPartType.RightFoot, rightFootSD, new(0, -rightFootSD.y / 2), rightLeg.transform);
            var leftFoot = InitI("leftFoot", UPC.down, skin.leftFoot, BodyPartType.LeftFoot, leftFootSD, new(0, -leftFootSD.y / 2), leftLeg.transform);

            rightLeg.transform.SetAsFirstSibling();
            leftLeg.transform.SetAsFirstSibling();
            rightFoot.transform.SetAsFirstSibling();
            leftFoot.transform.SetAsFirstSibling();
            leftArm.transform.SetAsFirstSibling();
            body.transform.SetAsFirstSibling();
            rightArm.transform.SetAsFirstSibling();
            head.transform.SetAsFirstSibling();

            ImageIdMessage InitI(string name, Vector4 pointer, Sprite sprite, BodyPartType type, Vector2 sd, Vector2 offset, Transform parent)
            {
                var img = GameUI.AddImage(pointer, $"ori:image.playerSkinNames.{skin.name}.{name}", null, parent);

                img.sd = sd;
                img.image.sprite = sprite;
                img.ap = offset;

                return img;
            }

            return (body, head, rightArm, leftArm, rightLeg, leftLeg, rightFoot, leftFoot);
        }

        public static (ImageIdMessage, ImageIdMessage, ImageIdMessage, TextIdMessage) GenerateLoadingBar(
            Vector4 positionCurrent,
            string bgId, string fullId, string textId,
            string backgroundSprite, string contentSprite, string mascotSprite,
            float mascotYDelta, float textYDelta,
            Vector2 barScale, Vector2 mascotScale,
            Func<float> GetProgress, Func<string> GetTextContent,
            Transform parent)
        {
            var barBg = AddImage(positionCurrent, bgId, backgroundSprite, parent);
            var barFull = AddImage(UPC.middle, fullId, contentSprite, barBg);
            var mascot = AddImage(UPC.left, fullId, mascotSprite, barBg);
            var progressText = AddText(UPC.middle, textId, barBg);

            barBg.sd = barScale;
            barBg.image.SetColorBrightness(0.35f);

            barFull.sd = barScale;
            barFull.image.type = Image.Type.Filled;
            barFull.image.fillMethod = Image.FillMethod.Horizontal;
            barFull.image.fillAmount = 0;
            barFull.OnUpdate += x =>
            {
                barFull.image.fillAmount = GetProgress();
            };

            mascot.sd = mascotScale;
            mascot.SetAPosY(barBg.sd.y / 2 + mascot.sd.y / 2 + mascotYDelta);
            mascot.OnUpdate += x =>
            {
                mascot.SetAPosX(barBg.sd.x * GetProgress());
            };

            progressText.autoCompareText = false;
            progressText.text.text = string.Empty;
            progressText.SetSizeDelta(barBg.sd.x, 40);
            progressText.text.SetFontSize(20);
            progressText.SetAPosY(barBg.sd.y / 2 + progressText.sd.y / 2 + textYDelta);
            progressText.OnUpdate += x =>
            {
                x.text.text = GetTextContent();
            };

            return (barBg, barFull, mascot, progressText);
        }

        public static KeyValuePair<PanelIdMessage, TextIdMessage> GenerateMask(string panelId, string textId, UnityAction<PanelIdMessage, TextIdMessage> afterFadingIn, UnityAction<PanelIdMessage, TextIdMessage> afterFadingOut)
        {
            //初始化 UI
            var panel = AddPanel(panelId);
            var text = AddText(UPC.middle, textId, panel);

            //将面板设为黑色
            panel.panelImage.SetColorBrightness(0);

            //绑定面板
            panel.CustomMethod += (type, _) =>
            {
                type ??= "fade_out";

                if (type == "fade_in")
                {
                    panel.panelImage.SetAlpha(0);

                    FadeIn(text.text);
                    FadeIn(panel.panelImage, true, 0, new(() => afterFadingIn?.Invoke(panel, text)));
                }
                else if (type == "fade_out")
                {
                    panel.panelImage.SetAlpha(1);

                    FadeOut(text.text);
                    FadeOut(panel.panelImage, true, 0, new(() => afterFadingOut?.Invoke(panel, text)));
                }
            };

            return new(panel, text);
        }
        #endregion

        #region 视觉效果
        #region 淡出入
        public static void FadeOut(Graphic target, bool setActiveToFalse = true, float delayTime = 0, UIAnimationAction? fadeAction = null)
        => InternalFade(target, true, setActiveToFalse, delayTime, fadeAction);

        public static void FadeIn(Graphic target, bool setActiveToTrue = true, float delayTime = 0, UIAnimationAction? fadeAction = null)
        => InternalFade(target, false, setActiveToTrue, delayTime, fadeAction);

        internal static async void InternalFade(Graphic target, bool isOut, bool setActive, float delayTime, UIAnimationAction? fadeAction = null)
        {
            //在开始动画前等待
            await delayTime;

            //执行 Before 委托
            if (isOut)
                BeforeFadeOut(target);
            else
                BeforeFadeIn(target);

            fadeAction?.beforeAnimation?.Invoke();

            //如果是淡入则先启用
            if (setActive && !isOut)
                target.gameObject.SetActive(true);

            //动画核心
            if (isOut)
            {
                await target.DOFade(0, 1);
            }
            else
            {
                await target.DOFade(1, 1);
            }

            if (target)
            {
                //如果是淡出则禁用
                if (setActive && isOut)
                    target.gameObject.SetActive(false);
            }

            //执行 After 委托
            if (isOut)
                AfterFadeOut(target);
            else
                AfterFadeIn(target);

            fadeAction?.afterAnimation?.Invoke();
        }
        #endregion

        #region 位置出入
        #region 并发
        public static void ChangeMain(IRectTransform disappearGameObject, IRectTransform appearGameObject)
        {
            Appear(appearGameObject);
            Disappear(disappearGameObject);
        }

        public static void ChangeMain(Transform disappearGameObject, Transform appearGameObject)
        {
            Appear(appearGameObject);
            Disappear(disappearGameObject);
        }

        public static void ChangeMain(GameObject disappearGameObject, GameObject appearGameObject)
        {
            Appear(appearGameObject);
            Disappear(disappearGameObject);
        }
        #endregion

        #region 消失
        public static void Disappear(IRectTransform ui, DisappearType type = DisappearType.PositionDownToUp) => Disappear(ui.rectTransform, type);

        public static void Disappear(GameObject targetGameObject, DisappearType type = DisappearType.PositionDownToUp) => Disappear(targetGameObject.transform, type);

        public static async void Disappear(Transform trans, DisappearType type = DisappearType.PositionDownToUp)
        {
            await UniTask.WaitWhile(() => disAndAppearingTransforms.Any(p => p == trans));

            lock (disAndAppearingTransforms)
                disAndAppearingTransforms.Add(trans);

            BeforeUIDisappear(trans);



            switch (type)
            {
                case DisappearType.PositionUpToDown:
                case DisappearType.PositionDownToUp:
                case DisappearType.PositionLeftToRight:
                case DisappearType.PositionRightToLeft:
                    Vector2 oldPos = trans.position;
                    CanvasGroup cg = trans.GetComponent<CanvasGroup>();
                    cg.interactable = false;
                    Vector2Int resolution = Tools.resolution;
                    Vector2 targetPos = type switch
                    {
                        DisappearType.PositionUpToDown => new(oldPos.x, oldPos.y - resolution.y),
                        DisappearType.PositionDownToUp => new(oldPos.x, oldPos.y + resolution.y),
                        DisappearType.PositionLeftToRight => new(oldPos.x + resolution.x, oldPos.y),
                        DisappearType.PositionRightToLeft => new(oldPos.x - resolution.x, oldPos.y),
                        _ => new(oldPos.x, oldPos.y + resolution.y),
                    };

                    await trans.DOMove(targetPos, 40 / GFiles.settings.uiSpeed);

                    trans.gameObject.SetActive(false);
                    trans.position = oldPos;

                    await disAndAppearingWaitTime;
                    trans.position = oldPos;
                    break;

                case DisappearType.Alpha:
                    if (trans.TryGetComponent<Graphic>(out var graphic))
                    {
                        FadeOut(graphic);
                    }
                    else
                    {
                        Debug.LogError("目标不包含 Graphic 组件");
                    }

                    break;
            }



            lock (disAndAppearingTransforms)
                disAndAppearingTransforms.Remove(trans);

            AfterUIDisappear(trans);
        }
        #endregion

        #region 出现
        public static void Appear(IRectTransform ui, AppearType type = AppearType.PositionUpToDown, UIAnimationAction? callbacks = null) => Appear(ui.rectTransform, type, callbacks);

        public static void Appear(GameObject targetGameObject, AppearType type = AppearType.PositionUpToDown, UIAnimationAction? callbacks = null) => Appear(targetGameObject.transform, type, callbacks);

        public static async void Appear(Transform trans, AppearType type = AppearType.PositionUpToDown, UIAnimationAction? callbacks = null)
        {
            await UniTask.WaitWhile(() => disAndAppearingTransforms.Any(p => p == trans));

            lock (disAndAppearingTransforms)
                disAndAppearingTransforms.Add(trans);

            callbacks?.beforeAnimation?.Invoke();
            BeforeUIAppear(trans);



            switch (type)
            {
                case AppearType.PositionUpToDown:
                case AppearType.PositionDownToUp:
                case AppearType.PositionLeftToRight:
                case AppearType.PositionRightToLeft:
                    Vector2 tarPos = trans.position;
                    trans.AddPos(type switch
                    {
                        AppearType.PositionUpToDown => new(0, Tools.resolution.y),
                        AppearType.PositionDownToUp => new(0, -Tools.resolution.y),
                        AppearType.PositionLeftToRight => new(-Tools.resolution.x, 0),
                        AppearType.PositionRightToLeft => new(Tools.resolution.x, 0),
                        _ => new(0, Tools.resolution.y),
                    });
                    CanvasGroup cg = trans.GetComponent<CanvasGroup>();
                    cg.interactable = false;
                    trans.gameObject.SetActive(true);

                    await trans.DOMove(tarPos, 40 / GFiles.settings.uiSpeed);

                    trans.SetPos(tarPos);
                    await disAndAppearingWaitTime;
                    cg.interactable = true;
                    SelectButton(uiWantToSelect);
                    trans.SetPos(tarPos);
                    break;

                case AppearType.Alpha:
                    if (trans.TryGetComponent<Graphic>(out var graphic))
                    {
                        FadeIn(graphic);
                    }
                    else
                    {
                        Debug.LogError("目标不包含 Graphic 组件");
                    }

                    break;
            }



            lock (disAndAppearingTransforms)
                disAndAppearingTransforms.Remove(trans);

            callbacks?.afterAnimation?.Invoke();
            AfterUIAppear(trans);
        }
        #endregion
        #endregion
        #endregion

        #region 选择 UI
        public static void SelectButton(GameObject go)
        {
            //必须是按钮
            eventSystem.SetSelectedGameObject(go);
        }

        public static void SetSelectButton(GameObject go)
        {
            //必须是按钮
            uiWantToSelect = go;
        }
        #endregion
    }

    public struct UIAnimationAction
    {
        public UIAnimationAction(UnityAction afterAnimation = null, UnityAction beforeAnimation = null)
        {
            this.afterAnimation = afterAnimation;
            this.beforeAnimation = beforeAnimation;
        }

        public UnityAction afterAnimation;
        public UnityAction beforeAnimation;
    }

    public static class UIExtensions
    {
        public static Button ToButton(this Image image) => image.gameObject.AddComponent<Button>();

        public static TMP_Text FadeIn(this TMP_Text text, bool setActive = true, float delayTime = 0, UIAnimationAction? fadeAction = null)
        { GameUI.FadeIn(text, setActive, delayTime, fadeAction); return text; }

        public static TMP_Text FadeOut(this TMP_Text text, bool setActive = true, float delayTime = 0, UIAnimationAction? fadeAction = null)
        { GameUI.FadeOut(text, setActive, delayTime, fadeAction); return text; }


        public static void ClearColorEffects(this Selectable s)
        {
            s.colors = new()
            {
                normalColor = s.colors.normalColor,
                highlightedColor = s.colors.normalColor,
                pressedColor = s.colors.normalColor,
                selectedColor = s.colors.normalColor,
                fadeDuration = s.colors.fadeDuration,
                disabledColor = s.colors.disabledColor,
                colorMultiplier = s.colors.colorMultiplier
            };
        }
    }
}
