using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using GameCore.UI;
using System;

namespace GameCore
{
    public class ButtonIdMessage : UIIdMessage<ButtonIdMessage>
    {
        private GameButton _button;
        private TextIdMessage _buttonText;
        private Image _image;


        public GameButton button { get { if (!_button) _button = GetComponent<GameButton>(); return _button; } }
        public TextIdMessage buttonText { get { if (!_buttonText) _buttonText = GetComponentInChildren<TextIdMessage>(); return _buttonText; } }
        public Image image { get { if (!_image) _image = GetComponent<Image>(); return _image; } }



        protected override void Awake()
        {
            base.Awake();

            IdMessageCenter.buttonMessages.Add(this);
        }

        public override void SetID(string id)
        {
            base.SetID(id);

            buttonText.id = $"{id}.text";
        }

        public ButtonIdMessage AddMethod(UnityAction call)
        {
            button.onClick.AddListener(call);
            return this;
        }
    }
}
