using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using GameCore.High;

namespace GameCore
{
    public class ImageTextButtonIdentity : UIIdentity<ImageTextButtonIdentity>
    {
        private ButtonIdentity _button;
        private ImageIdentity _image;
        private TextIdentity _buttonTextUp;
        private TextIdentity _buttonTextDown;

        public ButtonIdentity button { get { if (!_button) _button = transform.Find("ButtonPrefab").GetComponent<ButtonIdentity>(); return _button; } }
        public ImageIdentity image { get { if (!_image) _image = transform.Find("ImagePrefab").GetComponent<ImageIdentity>(); return _image; } }
        public TextIdentity buttonTextUp { get { if (!_buttonTextUp) _buttonTextUp = transform.Find("ButtonPrefab/TextPrefabUp").GetComponent<TextIdentity>(); return _buttonTextUp; } }
        public TextIdentity buttonTextDown { get { if (!_buttonTextDown) _buttonTextDown = transform.Find("ButtonPrefab/TextPrefabDown").GetComponent<TextIdentity>(); return _buttonTextDown; } }

        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.imageTextButtonMessage.Add(this);
        }

        public override void SetID(string id)
        {
            base.SetID(id);

            image.id = $"{id}.image";
            button.id = $"{id}.button";
            buttonTextUp.id = $"{id}.buttonTextUp";
            buttonTextDown.id = $"{id}.buttonTextDown";
        }

        public ImageTextButtonIdentity AddMethod(UnityAction call)
        {
            button.button.onClick.AddListener(call);
            return this;
        }

        public TextIdentity CreateText(string name, TextAlignmentOptions alignmentOptions)
        {
            TextIdentity text = Instantiate(buttonTextDown);
            text.rt.SetParent(buttonTextDown.rt.parent);
            text.rt.localScale = Vector3.one;
            text.gameObject.name = name;
            text.id = $"{id}.{name}";
            text.rt.sizeDelta = buttonTextDown.rt.sizeDelta;
            text.rt.anchoredPosition = buttonTextDown.rt.anchoredPosition;
            text.text.alignment = alignmentOptions;

            return text;
        }

        public void ResetStatusInScrollView(ScrollViewIdentity scrollView)
        {
            SetScale(new(scrollView.gridLayoutGroup.cellSize.x, scrollView.gridLayoutGroup.cellSize.y));
        }

        [Sirenix.OdinInspector.Button("设置尺寸")]
        public void SetScale(Vector2 vec)
        {
            //获取最小值并设置自身
            float min = Mathf.Min(vec.x, vec.y);
            rt.sizeDelta = vec;

            //设置图片大小
            image.rt.sizeDelta = new(min, min);
            image.rt.anchoredPosition = new(image.rt.sizeDelta.x / 2, 0);

            //设置按钮大小
            button.rt.sizeDelta = new(vec.x - image.rt.sizeDelta.x, image.rt.sizeDelta.y);
            button.rt.anchoredPosition = new(image.rt.anchoredPosition.x, 0);

            //设置文本大小
            buttonTextUp.rt.sizeDelta = button.rt.sizeDelta;
            buttonTextDown.rt.sizeDelta = button.rt.sizeDelta;
        }
    }
}
