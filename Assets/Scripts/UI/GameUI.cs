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
            public UIIdentity ui;
            public DisappearType disappearType;
            public AppearType appearType;

            public PanelPage(PanelPage lastPage, UIIdentity ui, DisappearType disappearType, AppearType appearType)
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
            Scale,
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
            Scale,
            ScaleX,
            ScaleY,
        }

        public static PanelPage page { get; private set; }

        public static void SetPage(UIIdentity value, DisappearType disappearType = DisappearType.PositionDownToUp, AppearType appearType = AppearType.PositionUpToDown)
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
                Appear(page.ui, page.appearType);
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

        public static Action<UIIdentity> BeforeFadeOutGroup = _ => { };
        public static Action<UIIdentity> AfterFadeOutGroup = _ => { };

        public static Action<Graphic> BeforeFadeIn = _ => { };
        public static Action<Graphic> AfterFadeIn = _ => { };

        public static Action<UIIdentity> BeforeFadeInGroup = _ => { };
        public static Action<UIIdentity> AfterFadeInGroup = _ => { };



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
        public static RectTransform canvasRT { get { if (!_canvasRT) _canvasRT = canvas.GetComponent<RectTransform>(); return _canvasRT; } }
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

            _currentLang ??= defaultLang;

            return _currentLang;
        }

        public static string CompareTextNullable(string id)
        {
            if (currentLang.TryCompareText(id, out var cText))
            {
                return cText;
            }
            else
            {
                if (defaultLang.TryCompareText(id, out var dText))
                {
                    return dText;
                }

                foreach (var data in ModFactory.finalLangs)
                {
                    if (data.id != currentLang.id && data.id != defaultLang.id)
                    {
                        if (data.TryCompareText(id, out var aText))
                        {
                            return aText;
                        }
                    }
                }

                return null;
            }
        }

        public static bool TryCompareTextNullable(string id, out string result)
        {
            result = CompareTextNullable(id);

            return result != null;
        }

        public static string CompareText(string id)
        {
            var nullable = CompareTextNullable(id);

            return nullable ?? id;
        }

        #endregion



        public static Vector2 ScreenUIPosInConstantCanvas(Vector2 anchoredPos, CanvasScaler constantCanvasScaler)
        {
            // Vector2 pos = anchoredPos * constantCanvasScaler.referenceResolution / Tools.resolution;
            // return pos - constantCanvasScaler.referenceResolution / 2;

            return new(anchoredPos.x * constantCanvasScaler.referenceResolution.x / Tools.resolution.x - constantCanvasScaler.referenceResolution.x / 2,
                        anchoredPos.y * constantCanvasScaler.referenceResolution.y / Tools.resolution.y - constantCanvasScaler.referenceResolution.y / 2);
        }



        private static T InstantiateIdMsg<T>(T original, string id) where T : UIIdentity
        {
            T msg = GameObject.Instantiate<T>(original);
            msg.SetID(id);

            return msg;
        }



        #region 添加 UI

        #region 添加画布

        public static Canvas AddCanvas(Transform parent = null)
        {
            var canvas = GameObject.Instantiate(GInit.instance.canvasPrefab, parent);

            return canvas;
        }

        public static Canvas AddWorldSpaceCanvas(Transform parent = null)
        {
            var canvas = AddCanvas(parent);

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.localPosition = Vector3.zero;

            return canvas;
        }

        #endregion

        #region 添加图片
        public static ImageIdentity AddImage(Vector4 positionCurrent, string id, string spriteId, GameObject parent)
        => AddImage(positionCurrent, id, spriteId, parent.transform);

        public static ImageIdentity AddImage(Vector4 positionCurrent, string id, string spriteId, UIIdentity parent)
        => AddImage(positionCurrent, id, spriteId, parent.transform);

        public static ImageIdentity AddImage(Vector4 positionCurrent, string id, string spriteId, string parentId)
        => AddImage(positionCurrent, id, spriteId, IdentityCenter.ComparePanelIdentity(parentId).transform);

        public static ImageIdentity AddImage(Vector4 positionCurrent, string id, string spriteId, Transform parent = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.imagePrefab, id);

            msg.rectTransform.SetParent(parent == null ? canvas.transform : parent);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;

            if (!spriteId.IsNullOrWhiteSpace())
                msg.image.sprite = ModFactory.CompareTexture(spriteId)?.sprite;

            return msg;
        }
        #endregion

        #region 添加图问
        public static TextImageIdentity AddTextImage(Vector4 positionCurrent, string id, string spriteId, GameObject parent)
        => AddTextImage(positionCurrent, id, spriteId, parent.transform);

        public static TextImageIdentity AddTextImage(Vector4 positionCurrent, string id, string spriteId, UIIdentity parent)
        => AddTextImage(positionCurrent, id, spriteId, parent.transform);

        public static TextImageIdentity AddTextImage(Vector4 positionCurrent, string id, string spriteId, string parentId)
        => AddTextImage(positionCurrent, id, spriteId, IdentityCenter.ComparePanelIdentity(parentId).transform);

        public static TextImageIdentity AddTextImage(Vector4 positionCurrent, string id, string spriteId, Transform parent = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.textImagePrefab, id);

            msg.rectTransform.SetParent(parent == null ? canvas.transform : parent);
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
        public static RawImageIdentity AddRawImage(Vector4 positionCurrent, string id, GameObject parent)
        => AddRawImage(positionCurrent, id, parent.transform);

        public static RawImageIdentity AddRawImage(Vector4 positionCurrent, string id, IRectTransform parent)
        => AddRawImage(positionCurrent, id, parent.rectTransform);

        public static RawImageIdentity AddRawImage(Vector4 positionCurrent, string id, string parentId)
        => AddRawImage(positionCurrent, id, IdentityCenter.ComparePanelIdentity(parentId).transform);

        public static RawImageIdentity AddRawImage(Vector4 positionCurrent, string id, Transform parent = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.rawImagePrefab, id);

            msg.rectTransform.SetParent(parent == null ? canvas.transform : parent);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;

            return msg;
        }
        #endregion

        #region 添加滚动视图
        public static ScrollViewIdentity AddScrollView(Vector4 positionCurrent, string id, GameObject parent)
        => AddScrollView(positionCurrent, id, parent.transform);

        public static ScrollViewIdentity AddScrollView(Vector4 positionCurrent, string id, IRectTransform parent)
        => AddScrollView(positionCurrent, id, parent.rectTransform);

        public static ScrollViewIdentity AddScrollView(Vector4 positionCurrent, string id, string parentId)
        => AddScrollView(positionCurrent, id, IdentityCenter.ComparePanelIdentity(parentId).transform);

        public static ScrollViewIdentity AddScrollView(Vector4 positionCurrent, string id, Transform parent = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.scrollViewPrefab, id);

            msg.rectTransform.SetParent(parent == null ? canvas.transform : parent);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            return msg;
        }
        #endregion

        #region 添加文本
        public static TextIdentity AddText(Vector4 positionCurrent, string id, GameObject parent)
        => AddText(positionCurrent, id, parent.transform);

        public static TextIdentity AddText(Vector4 positionCurrent, string id, IRectTransform parent)
        => AddText(positionCurrent, id, parent.rectTransform);

        public static TextIdentity AddText(Vector4 positionCurrent, string id, string parentId)
        => AddText(positionCurrent, id, IdentityCenter.ComparePanelIdentity(parentId).transform);

        public static TextIdentity AddText(Vector4 positionCurrent, string id, Transform parent = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.textPrefab, id);

            msg.rectTransform.SetParent(parent == null ? canvas.transform : parent);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            return msg;
        }
        #endregion

        #region 添加开关
        public static ToggleIdentity AddToggle(Vector4 positionCurrent, string id, GameObject parent)
        => AddToggle(positionCurrent, id, parent.transform);

        public static ToggleIdentity AddToggle(Vector4 positionCurrent, string id, IRectTransform parent)
        => AddToggle(positionCurrent, id, parent.rectTransform);

        public static ToggleIdentity AddToggle(Vector4 positionCurrent, string id, string parentId)
        => AddToggle(positionCurrent, id, IdentityCenter.ComparePanelIdentity(parentId).transform);

        public static ToggleIdentity AddToggle(Vector4 positionCurrent, string id, Transform parent = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.togglePrefab, id);

            msg.rectTransform.SetParent(parent == null ? canvas.transform : parent);
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
            msg.OnValueChangeBind(RefreshIt);
            msg.OnValueChangeBind(_ => GAudio.Play(AudioID.Button));

            return msg;
        }
        #endregion

        #region 添加输入框
        public static InputFieldIdentity AddInputField(Vector4 positionCurrent, string id, GameObject parent)
        => AddInputField(positionCurrent, id, parent.transform);

        public static InputFieldIdentity AddInputField(Vector4 positionCurrent, string id, IRectTransform parent)
        => AddInputField(positionCurrent, id, parent.rectTransform);

        public static InputFieldIdentity AddInputField(Vector4 positionCurrent, string id, string parentId)
        => AddInputField(positionCurrent, id, IdentityCenter.ComparePanelIdentity(parentId).transform);

        public static InputFieldIdentity AddInputField(Vector4 positionCurrent, string id, Transform parent = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.inputFieldPrefab, id);

            msg.image.sprite = ModFactory.CompareTexture("ori:button").sprite;
            msg.rectTransform.SetParent(parent == null ? canvas.transform : parent);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            return msg;
        }
        #endregion

        #region 添加图文按钮
        public static ImageTextButtonIdentity AddImageTextButton(Vector4 positionCurrent, string id, GameObject parent)
        => AddImageTextButton(positionCurrent, id, parent.transform);

        public static ImageTextButtonIdentity AddImageTextButton(Vector4 positionCurrent, string id, IRectTransform parent)
        => AddImageTextButton(positionCurrent, id, parent.rectTransform);

        public static ImageTextButtonIdentity AddImageTextButton(Vector4 positionCurrent, string id, string parentId)
        => AddImageTextButton(positionCurrent, id, IdentityCenter.ComparePanelIdentity(parentId).transform);

        public static ImageTextButtonIdentity AddImageTextButton(Vector4 positionCurrent, string id, Transform parent = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.imageTextButtonPrefab, id);

            msg.rectTransform.SetParent(parent == null ? canvas.transform : parent);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            msg.button.image.sprite = ModFactory.CompareTexture("ori:button_flat").sprite;
            msg.OnClickBind(() => GAudio.Play(AudioID.Button));

            return msg;
        }
        #endregion

        #region 添加输入按钮
        public static InputButtonIdentity AddInputButton(Vector4 positionCurrent, string id, GameObject parent)
        => AddInputButton(positionCurrent, id, parent.transform);

        public static InputButtonIdentity AddInputButton(Vector4 positionCurrent, string id, IRectTransform parent)
        => AddInputButton(positionCurrent, id, parent.rectTransform);

        public static InputButtonIdentity AddInputButton(Vector4 positionCurrent, string id, string parentId)
        => AddInputButton(positionCurrent, id, IdentityCenter.ComparePanelIdentity(parentId).transform);

        public static InputButtonIdentity AddInputButton(Vector4 positionCurrent, string id, Transform parent = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.inputButtonPrefab, id);

            msg.rt.SetParent(parent == null ? canvas.transform : parent);
            msg.rt.localPosition = Vector2.zero;
            msg.rt.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rt.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rt.localScale = Vector2.one;
            msg.button.image.sprite = ModFactory.CompareTexture("ori:square_button").sprite;
            msg.field.image.sprite = ModFactory.CompareTexture("ori:button_flat").sprite;
            msg.OnClickBind(() => GAudio.Play(AudioID.Button));

            return msg;
        }
        #endregion

        #region 添加滑动条
        public static SliderIdentity AddSlider(Vector4 positionCurrent, string id, GameObject parent)
        => AddSlider(positionCurrent, id, parent.transform);

        public static SliderIdentity AddSlider(Vector4 positionCurrent, string id, IRectTransform parent)
        => AddSlider(positionCurrent, id, parent.rectTransform);

        public static SliderIdentity AddSlider(Vector4 positionCurrent, string id, string parentId)
        => AddSlider(positionCurrent, id, IdentityCenter.ComparePanelIdentity(parentId).transform);

        public static SliderIdentity AddSlider(Vector4 positionCurrent, string id, Transform parent = null)
        {
            var msg = InstantiateIdMsg(GInit.instance.sliderPrefab, id);

            msg.rt.SetParent(parent ? parent : canvas.transform);
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
        public static PanelIdentity AddPanel(string id) => AddPanel(id, parent: (Transform)null, false);

        public static PanelIdentity AddPanel(string id, IRectTransform parent) => AddPanel(id, parent.rectTransform, false);
        public static PanelIdentity AddPanel(string id, IRectTransform parent, bool disable) => AddPanel(id, parent.rectTransform, disable);

        public static PanelIdentity AddPanel(string id, string parentId) => AddPanel(id, IdentityCenter.ComparePanelIdentity(parentId).transform, false);
        public static PanelIdentity AddPanel(string id, string parentId, bool disable) => AddPanel(id, IdentityCenter.ComparePanelIdentity(parentId).transform, disable);

        public static PanelIdentity AddPanel(string id, GameObject parent) => AddPanel(id, parent.transform, false);
        public static PanelIdentity AddPanel(string id, GameObject parent, bool disable) => AddPanel(id, parent.transform, disable);

        public static PanelIdentity AddPanel(string id, Transform parent) => AddPanel(id, parent, false);
        public static PanelIdentity AddPanel(string id, Transform parent, bool disable)
        {
            var msg = InstantiateIdMsg(GInit.instance.panelPrefab, id);

            msg.rt.SetParent(parent ? parent : canvas.transform);
            msg.rt.anchorMin = Vector2.zero;
            msg.rt.anchorMax = Vector2.one;
            msg.rt.localScale = Vector2.one;
            msg.ap = Vector2.zero;
            msg.rt.sizeDelta = Vector2.zero;

            if (disable)
                msg.gameObject.SetActive(false);

            return msg;
        }

        public static PanelIdentity AddBackpackFormedPanel(string id, IRectTransform parent, bool disable = false) => AddBackpackFormedPanel(id, parent.rectTransform, disable);
        public static PanelIdentity AddBackpackFormedPanel(string id, Transform parent, bool disable = false)
        {
            var result = AddPanel(id, parent, disable);

            result.SetSizeDelta(PlayerUI.backpackPanelWidth, PlayerUI.backpackPanelHeight);
            result.SetAnchorMinMax(UIA.Middle);
            result.panelImage.sprite = ModFactory.CompareTexture("ori:backpack_inventory_background").sprite;
            result.panelImage.color = Color.white;

            return result;
        }
        #endregion

        #region 添加按钮
        public static ButtonIdentity AddButton(Vector4 positionCurrent, string id)
        => AddButtonInternal(positionCurrent, id, canvas.transform, "ori:button");

        public static ButtonIdentity AddButton(Vector4 positionCurrent, string id, Transform parent, string buttonSpriteId = "ori:button")
        => AddButtonInternal(positionCurrent, id, parent, buttonSpriteId);

        public static ButtonIdentity AddButton(Vector4 positionCurrent, string id, IRectTransform parent, string buttonSpriteId = "ori:button")
        => AddButtonInternal(positionCurrent, id, parent.rectTransform, buttonSpriteId);

        public static ButtonIdentity AddButton(Vector4 positionCurrent, string id, string parentId, string buttonSpriteId = "ori:button")
        => AddButtonInternal(positionCurrent, id, IdentityCenter.ComparePanelIdentity(parentId).transform, buttonSpriteId);

        static internal ButtonIdentity AddButtonInternal(Vector4 positionCurrent, string id, Transform parent, string buttonSpriteId = "ori:button")
        {
            var msg = InstantiateIdMsg(GInit.instance.buttonPrefab, id);

            msg.rectTransform.SetParent(parent);
            msg.rectTransform.localPosition = Vector2.zero;
            msg.rectTransform.anchorMin = new(positionCurrent.x, positionCurrent.y);
            msg.rectTransform.anchorMax = new(positionCurrent.z, positionCurrent.w);
            msg.rectTransform.localScale = Vector2.one;
            msg.BindButtonAudio();
            if (!buttonSpriteId.IsNullOrWhiteSpace()) msg.image.sprite = ModFactory.CompareTexture(buttonSpriteId).sprite;

            return msg;
        }
        #endregion

        //TODO: recomplete it
        public static (ImageIdentity, ImageIdentity, ImageIdentity, ImageIdentity, ImageIdentity, ImageIdentity, ImageIdentity, ImageIdentity) GenerateSkinShow(PlayerSkin skin, Transform parent)
        {
            float bodyWide = 20;
            Vector2 bodySD = new(bodyWide, bodyWide / skin.body.texture.width * skin.body.texture.height);
            Vector2 headSD = new(bodySD.x * 1.15f, bodySD.x * 1.3f);
            Vector2 rightArmSD = new(bodySD.y * skin.rightArm.texture.width / skin.rightArm.texture.height, bodySD.y);
            Vector2 leftArmSD = new(bodySD.y * skin.leftArm.texture.width / skin.leftArm.texture.height, bodySD.y);
            Vector2 rightLegSD = new(bodySD.x / 2, bodySD.x / 2 / skin.rightLeg.texture.width * skin.rightLeg.texture.height);
            Vector2 leftLegSD = new(bodySD.x / 2, bodySD.x / 2 / skin.leftLeg.texture.width * skin.leftLeg.texture.height);
            Vector2 rightFootSD = new(rightLegSD.x * 1.35f, rightLegSD.x * 1.35f / skin.rightFoot.texture.width * skin.rightFoot.texture.height);
            Vector2 leftFootSD = new(leftLegSD.x * 1.35f, leftLegSD.x * 1.35f / skin.leftFoot.texture.width * skin.leftFoot.texture.height);

            var body = InitI("body", UIA.Middle, skin.body, BodyPartType.Body, bodySD, Vector2.zero, parent);
            var head = InitI("head", UIA.Up, skin.head, BodyPartType.Head, headSD, new(-0.8f, headSD.y / 2 - 2.5f), body.transform);
            var rightArm = InitI("rightArm", UIA.UpperLeft, skin.rightArm, BodyPartType.RightArm, rightArmSD, new(rightArmSD.x / 10, -rightArmSD.y / 2 - 3f), body.transform);
            var leftArm = InitI("leftArm", UIA.UpperRight, skin.leftArm, BodyPartType.LeftArm, leftArmSD, Vector2.zero, body.transform);
            var rightLeg = InitI("rightLeg", UIA.LowerLeft, skin.rightLeg, BodyPartType.RightLeg, rightLegSD, new(rightLegSD.x / 2, -rightLegSD.y / 2), body.transform);
            var leftLeg = InitI("leftLeg", UIA.LowerRight, skin.leftLeg, BodyPartType.LeftLeg, leftLegSD, new(-leftLegSD.x / 2, -leftLegSD.y / 2), body.transform);
            var rightFoot = InitI("rightFoot", UIA.Down, skin.rightFoot, BodyPartType.RightFoot, rightFootSD, new(0, -rightFootSD.y / 2), rightLeg.transform);
            var leftFoot = InitI("leftFoot", UIA.Down, skin.leftFoot, BodyPartType.LeftFoot, leftFootSD, new(0, -leftFootSD.y / 2), leftLeg.transform);

            rightLeg.transform.SetAsFirstSibling();
            leftLeg.transform.SetAsFirstSibling();
            rightFoot.transform.SetAsFirstSibling();
            leftFoot.transform.SetAsFirstSibling();
            leftArm.transform.SetAsFirstSibling();
            body.transform.SetAsFirstSibling();
            rightArm.transform.SetAsFirstSibling();
            head.transform.SetAsFirstSibling();

            //使得左手在身体的下面
            leftArm.SetAnchorMinMax(UIA.Middle);
            leftArm.transform.SetParent(body.transform.parent);
            leftArm.SetAPosOnBySizeRight(body, -leftArmSD.x / 2);
            leftArm.AddAPosY(-3f);
            body.transform.SetAsLastSibling();

            ImageIdentity InitI(string name, Vector4 pointer, Sprite sprite, BodyPartType type, Vector2 sd, Vector2 offset, Transform parent)
            {
                var img = AddImage(pointer, $"ori:image.playerSkinNames.{skin.name}.{name}", null, parent);

                img.sd = sd;
                img.image.sprite = sprite;
                img.ap = offset;

                return img;
            }

            return (body, head, rightArm, leftArm, rightLeg, leftLeg, rightFoot, leftFoot);
        }

        public static (ImageIdentity barBg, ImageIdentity barFull, ImageIdentity mascot, TextIdentity progressText) GenerateLoadingBar(
            Vector4 positionCurrent,
            string bgId, string fullId, string mascotId, string textId,
            string backgroundSprite, string contentSprite, string mascotSprite,
            float mascotYDelta, float textYDelta,
            Vector2 barScale, Vector2 mascotScale,
            Func<float> GetProgress, Func<string> GetTextContent,
            Transform parent)
        {
            var barBg = AddImage(positionCurrent, bgId, backgroundSprite, parent);
            var barFull = AddImage(UIA.StretchMiddle, fullId, contentSprite, barBg);
            var mascot = AddImage(UIA.Left, mascotId, mascotSprite, barBg);
            var progressText = AddText(UIA.Middle, textId, barBg);

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

        public static (PanelIdentity panel, TextIdentity text) GenerateMask(string panelId, string textId, UnityAction<PanelIdentity, TextIdentity> afterFadingIn, UnityAction<PanelIdentity, TextIdentity> afterFadingOut)
        {
            //初始化 UI
            var panel = AddPanel(panelId);
            var text = AddText(UIA.Middle, textId, panel);

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
                    FadeIn(panel.panelImage, true, 1.5f, new(() => afterFadingIn?.Invoke(panel, text)));
                }
                else if (type == "fade_out")
                {
                    panel.panelImage.SetAlpha(1);

                    FadeOut(text.text);
                    FadeOut(panel.panelImage, true, 1, new(() => afterFadingOut?.Invoke(panel, text)));
                }
            };

            return new(panel, text);
        }

        public static (PanelIdentity panel, TextIdentity text) RegionGenerationMask(UnityAction<PanelIdentity, TextIdentity> afterFadingIn, UnityAction<PanelIdentity, TextIdentity> afterFadingOut)
        {
            return GenerateMask("ori:panel.wait_generating_the_region", "ori:text.wait_generating_the_region", afterFadingIn, afterFadingOut);
        }

        public static (PanelIdentity panel, TextIdentity text) LeavingGameMask(UnityAction<PanelIdentity, TextIdentity> afterFadingIn, UnityAction<PanelIdentity, TextIdentity> afterFadingOut)
        {
            return GenerateMask("ori:panel.mask.left_game", "ori:text.leaving_game", afterFadingIn, afterFadingOut);
        }





        public static (ImageIdentity backgroundImage, ButtonIdentity button) GenerateSidebarSwitchButton(string buttonBackgroundId, string buttonId, string switchButtonTexture, IRectTransform parent, int sidebarIndex, bool destroyButtonText = true) => GenerateSidebarSwitchButton(buttonBackgroundId, buttonId, switchButtonTexture, parent.rectTransform, sidebarIndex, destroyButtonText);
        public static (ImageIdentity backgroundImage, ButtonIdentity button) GenerateSidebarSwitchButton(string buttonBackgroundId, string buttonId, string switchButtonTexture, Transform parent, int sidebarIndex, bool destroyButtonText = true)
        {
            var switchButtonBackground = AddImage(UIA.UpperLeft, buttonBackgroundId, "ori:backpack_panel_switch_button", parent);
            switchButtonBackground.sd = new(50, 50);
            switchButtonBackground.SetAPos(switchButtonBackground.sd.x / 2 + (switchButtonBackground.sd.x + 10) * sidebarIndex, switchButtonBackground.sd.y / 2);

            var switchButton = AddButton(UIA.Middle, buttonId, switchButtonBackground, switchButtonTexture);
            switchButton.sd = switchButtonBackground.sd;
            if (destroyButtonText)
                GameObject.Destroy(switchButton.buttonText.gameObject);

            return (switchButtonBackground, switchButton);
        }

        #endregion





        public static void SetUILayer(UIIdentity ui, int layer)
        {
            int lowestIndex = ui.transform.parent.childCount - 1;
            ui.transform.SetSiblingIndex(Mathf.Max(lowestIndex, lowestIndex - layer));
        }

        public static void SetUILayerToOverTop(UIIdentity ui)
        {
            SetUILayer(ui, 0);
        }

        public static void SetUILayerToTop(UIIdentity ui)
        {
            SetUILayer(ui, 1);
        }

        public static void SetUILayerToFirst(UIIdentity ui)
        {
            SetUILayer(ui, 2);
        }





        #region 视觉效果
        #region 淡出入
        public static async void FadeOutGroup(UIIdentity target, bool setActiveToFalse = true, float duration = 1, UIAnimationAction? fadeAction = null)
        {
            BeforeFadeOutGroup(target);
            fadeAction?.beforeAnimation?.Invoke();



            //播放动画
            await target.canvasGroup.DOFade(0, duration);



            if (target && setActiveToFalse)
                target.gameObject.SetActive(false);

            AfterFadeInGroup(target);
            fadeAction?.afterAnimation?.Invoke();
        }

        public static async void FadeOut(Graphic target, bool setActiveToFalse = true, float duration = 1, UIAnimationAction? fadeAction = null)
        {
            BeforeFadeOut(target);
            fadeAction?.beforeAnimation?.Invoke();



            //播放动画
            await target.DOFade(0, duration);



            if (target && setActiveToFalse)
                target.gameObject.SetActive(false);

            AfterFadeIn(target);
            fadeAction?.afterAnimation?.Invoke();
        }

        public static async void FadeInGroup(UIIdentity target, bool setActiveToTrue = true, float duration = 1, UIAnimationAction? fadeAction = null)
        {
            BeforeFadeInGroup(target);
            fadeAction?.beforeAnimation?.Invoke();

            if (setActiveToTrue)
                target.gameObject.SetActive(true);



            //播放动画
            await target.canvasGroup.DOFade(1, duration);



            AfterFadeOutGroup(target);
            fadeAction?.afterAnimation?.Invoke();
        }

        public static async void FadeIn(Graphic target, bool setActiveToTrue = true, float duration = 1, UIAnimationAction? fadeAction = null)
        {
            BeforeFadeIn(target);
            fadeAction?.beforeAnimation?.Invoke();

            if (setActiveToTrue)
                target.gameObject.SetActive(true);



            //播放动画
            await target.DOFade(1, duration);



            AfterFadeOut(target);
            fadeAction?.afterAnimation?.Invoke();
        }


        static readonly List<FadeInThenOutArgs> fadeInThenOutList = new();
        public static async void FadeInThenOut(FadeInThenOutArgs args)
        {
            bool hasInstance = false;
            foreach (var item in fadeInThenOutList)
            {
                if (item.target.GetInstanceID() == args.target.GetInstanceID())
                {
                    item.fadeOutWaitedTime = 0;
                    args.preparingToFadeOut = item.preparingToFadeOut;

                    hasInstance = true;
                }
            }

            //停止淡出动画
            Tools.KillTweensOf(args.target);

            if (!hasInstance)
                fadeInThenOutList.Add(args);

            //播放淡入动画
            if (args.target.color.a == 1) args.target.SetAlpha(0);
            FadeIn(args.target, fadeAction: args.fadeInAction);

            //这层检查是为了可以打断准备播放的淡出动画，因为 while 不会停止
            if (!args.preparingToFadeOut)
            {
                args.preparingToFadeOut = true;

                //等待淡出间隔
                while (args.fadeOutWaitedTime < args.duration)
                {
                    args.fadeOutWaitedTime += Tools.deltaTime;

                    await UniTask.NextFrame();
                }

                //杀死淡入动画
                Tools.KillTweensOf(args.target);

                //淡出动画
                args.target.SetAlpha(1);
                FadeOut(args.target, true, 1, args.fadeOutAction);

                //还原参数
                args.fadeOutWaitedTime = 0;
                args.preparingToFadeOut = false;
                fadeInThenOutList.Remove(args); //在 if 之内而不是之外删除，是为了把删除权利交给最先发起动画的方法调用
            }
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

                case DisappearType.Scale:
                case DisappearType.ScaleX:
                case DisappearType.ScaleY:
                    throw new NotImplementedException();
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
                    CanvasGroup cg = trans.gameObject.GetOrAddComponent<CanvasGroup>();
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

                case AppearType.Scale:
                case AppearType.ScaleX:
                case AppearType.ScaleY:
                    throw new NotImplementedException();
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

    public class FadeInThenOutArgs
    {
        public Graphic target;
        public float duration;
        public UIAnimationAction? fadeInAction;
        public UIAnimationAction? fadeOutAction;
        internal float fadeOutWaitedTime;
        internal bool preparingToFadeOut;

        public FadeInThenOutArgs(Graphic target, float duration, UIAnimationAction? fadeInAction = null, UIAnimationAction? fadeOutAction = null)
        {
            this.target = target;
            this.duration = duration;
            this.fadeInAction = fadeInAction;
            this.fadeOutAction = fadeOutAction;
        }
    }
}
