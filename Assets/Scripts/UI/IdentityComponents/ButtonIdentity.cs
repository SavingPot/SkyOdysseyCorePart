using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using GameCore.UI;
using System;

namespace GameCore.UI
{
    public class ButtonIdentity : UIIdentity<ButtonIdentity>
    {
        private GameButton _button;
        private TextIdentity _buttonText;
        private Image _image;


        public GameButton button { get { if (!_button) _button = GetComponent<GameButton>(); return _button; } }
        public TextIdentity buttonText { get { if (!_buttonText) _buttonText = GetComponentInChildren<TextIdentity>(); return _buttonText; } }
        public Image image { get { if (!_image) _image = GetComponent<Image>(); return _image; } }




        public void SetSprite(Sprite sprite)
        {
            image.sprite = sprite;
        }

        public void SetColor(Color color)
        {
            image.color = color;
        }

        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.buttonIdentities.Add(this);
        }

        public override void SetID(string id)
        {
            base.SetID(id);

            buttonText.id = $"{id}.text";
        }

        public void SetText(object text)
        {
            buttonText.text.text = text.ToString();
        }

        public void SetText(string text)
        {
            buttonText.text.text = text;
        }

        public ButtonIdentity OnClickBind(UnityAction call)
        {
            button.onClick.AddListener(call);
            return this;
        }
    }
}
