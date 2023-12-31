using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.UI;

namespace GameCore
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
                text.text = GameUI.CompareText(id).text;
        }
    }
}
