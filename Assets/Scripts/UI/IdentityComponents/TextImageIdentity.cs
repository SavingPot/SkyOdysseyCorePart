using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using GameCore.UI;
using System;
using SP.Tools.Unity;
using Sirenix.OdinInspector;

namespace GameCore.UI
{
    public class TextImageIdentity : UIIdentity<TextImageIdentity>
    {
        private TextIdentity _text;
        private Image _image;


        public TextIdentity text { get { if (!_text) _text = GetComponentInChildren<TextIdentity>(); return _text; } }
        public Image image { get { if (!_image) _image = GetComponent<Image>(); return _image; } }



        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.textImageIdentities.Add(this);
        }

        public override void SetID(string id)
        {
            base.SetID(id);

            text.id = $"{id}.text";
        }

        public void SetText(object text)
        {
            this.text.text.text = text.ToString();
        }

        public void SetText(string text)
        {
            this.text.text.text = text;
        }

        public void SetSizeDeltaBoth(float x, float y) => SetSizeDeltaBoth(new(x, y));

        public void SetSizeDeltaBoth(Vector2 sizeDelta)
        {
            sd = sizeDelta;
            text.sd = new(sizeDelta.x * 4, sizeDelta.y * 0.75f);
        }

        [Button]
        public void SetTextAttach(TextAttach attach)
        {
            switch (attach)
            {
                case TextAttach.Right:
                    text.SetAPos(sd.x / 2 + text.sd.x / 2, 0);
                    text.text.alignment = TextAlignmentOptions.Left;
                    break;

                case TextAttach.Left:
                    text.SetAPos(-sd.x / 2 - text.sd.x / 2, 0);
                    text.text.alignment = TextAlignmentOptions.Right;
                    break;

                default:
                    throw new();
            }
        }



        public enum TextAttach : byte
        {
            Right,
            Left
        }
    }
}
