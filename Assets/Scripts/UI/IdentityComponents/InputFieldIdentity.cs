using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameCore.High;
using SP.Tools;
using GameCore.UI;

namespace GameCore.UI
{
    public class InputFieldIdentity : UIIdentity<InputFieldIdentity>
    {
        private TMP_InputField _field;
        public TMP_InputField field { get { if (!_field) _field = GetComponent<TMP_InputField>(); return _field; } }

        private RectTransform _textArea;
        public RectTransform textArea { get { if (!_textArea) _textArea = transform.Find("Text Area").GetComponent<RectTransform>(); return _textArea; } }

        private RectMask2D _mask;
        public RectMask2D mask { get { if (!_mask) _mask = textArea.GetComponent<RectMask2D>(); return _mask; } }

        private TMP_Text _placeholder;
        public TMP_Text placeholder { get { if (!_placeholder) _placeholder = textArea.Find("Placeholder").GetComponent<TMP_Text>(); return _placeholder; } }

        private Image _image;
        public Image image { get { if (!_image) _image = GetComponent<Image>(); return _image; } }

        public LockType lockType;
        public bool autoCompareText = true;

        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.inputFieldIdentities.Add(this);
        }

        protected override void Update()
        {
            base.Update();

            if (lockType == LockType.IntNumber)
            {
                field.contentType = TMP_InputField.ContentType.IntegerNumber;

                if (field.text.ToLong() > int.MaxValue)
                    field.text = int.MaxValue.ToString();
            }
        }

        protected override void InternalRefreshUI()
        {
            base.InternalRefreshUI();

            if (autoCompareText && GameUI.TryCompareTextNullable(id, out var result))
            {
                placeholder.text = result;
            }
        }

        public InputFieldIdentity DisableAutoCompare()
        {
            autoCompareText = false;
            return this;
        }

        public InputFieldIdentity SetAutoCompare(bool autoCompare)
        {
            autoCompareText = autoCompare;
            return this;
        }



        public enum LockType
        {
            None,
            IntNumber
        }
    }
}
