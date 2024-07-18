using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.UI;
using Org.BouncyCastle.Tls.Crypto;

namespace GameCore.UI
{
    public class TextIdentity : UIIdentity<TextIdentity>
    {
        private TMP_Text _text;

        public TMP_Text text { get { if (_text == null) _text = GetComponent<TMP_Text>(); return _text; } }

        public bool autoCompareText = true;

        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.textIdentities.Add(this);
        }

        protected override void InternalRefreshUI()
        {
            base.InternalRefreshUI();

            if (autoCompareText)
                text.text = GameUI.CompareText(id);
        }

        public TextIdentity DisableAutoCompare()
        {
            autoCompareText = false;
            return this;
        }

        public TextIdentity SetAutoCompare(bool autoCompare)
        {
            autoCompareText = autoCompare;
            return this;
        }

        public TextIdentity SetText(object text)
        {
            this.text.text = text.ToString();
            return this;
        }

        public TextIdentity SetText(string text)
        {
            this.text.text = text;
            return this;
        }
    }
}
