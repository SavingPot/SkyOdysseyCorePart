using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using GameCore.High;

namespace GameCore
{
    public class ImageTextButtonIdMessage : UIIdMessage<ImageTextButtonIdMessage>
    {
        private ButtonIdMessage _button;
        private ImageIdMessage _image;
        private TextIdMessage _buttonTextUp;
        private TextIdMessage _buttonTextDown;

        public ButtonIdMessage button { get { if (!_button) _button = transform.Find("ButtonPrefab").GetComponent<ButtonIdMessage>(); return _button; } }
        public ImageIdMessage image { get { if (!_image) _image = transform.Find("ImagePrefab").GetComponent<ImageIdMessage>(); return _image; } }
        public TextIdMessage buttonTextUp { get { if (!_buttonTextUp) _buttonTextUp = transform.Find("ButtonPrefab/TextPrefabUp").GetComponent<TextIdMessage>(); return _buttonTextUp; } }
        public TextIdMessage buttonTextDown { get { if (!_buttonTextDown) _buttonTextDown = transform.Find("ButtonPrefab/TextPrefabDown").GetComponent<TextIdMessage>(); return _buttonTextDown; } }

        protected override void Awake()
        {
            base.Awake();

            IdMessageCenter.imageTextButtonMessage.Add(this);
        }

        public override void SetID(string id)
        {
            base.SetID(id);

            image.id = $"{id}.image";
            button.id = $"{id}.button";
            buttonTextUp.id = $"{id}.buttonTextUp";
            buttonTextDown.id = $"{id}.buttonTextDown";
        }

        public ImageTextButtonIdMessage AddMethod(UnityAction call)
        {
            button.button.onClick.AddListener(call);
            return this;
        }

        public TextIdMessage CreateText(string name, TextAlignmentOptions alignmentOptions)
        {
            TextIdMessage text = Instantiate(buttonTextDown);
            text.rt.SetParent(buttonTextDown.rt.parent);
            text.rt.localScale = Vector3.one;
            text.gameObject.name = name;
            text.id = $"{id}.{name}";
            text.rt.sizeDelta = buttonTextDown.rt.sizeDelta;
            text.rt.anchoredPosition = buttonTextDown.rt.anchoredPosition;
            text.text.alignment = alignmentOptions;

            return text;
        }

        public void ResetStatusInScrollView(ScrollViewIdMessage scrollView)
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
