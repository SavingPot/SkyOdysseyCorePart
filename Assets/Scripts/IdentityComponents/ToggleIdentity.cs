using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using GameCore.High;
using SP.Tools.Unity;

namespace GameCore
{
    public class ToggleIdentity : UIIdentity<ToggleIdentity>
    {
        private Toggle _toggle;
        private ImageIdentity _bg;
        private ImageIdentity _checkmark;
        private ImageIdentity _textBg;
        private TextIdentity _text;

        public Toggle toggle { get { if (!_toggle) _toggle = GetComponent<Toggle>(); return _toggle; } }
        public ImageIdentity bg { get { if (!_bg) _bg = this.FindComponent<ImageIdentity>("Background"); return _bg; } }
        public ImageIdentity checkmark { get { if (!_checkmark) _checkmark = bg.FindComponent<ImageIdentity>("Checkmark"); return _checkmark; } }
        public ImageIdentity textBg { get { if (!_textBg) _textBg = this.FindComponent<ImageIdentity>("TextBackground"); return _textBg; } }
        public TextIdentity text { get { if (!_text) _text = textBg.FindComponent<TextIdentity>("Text"); return _text; } }



        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.toggleIdentities.Add(this);
        }

        public override void SetID(string id)
        {
            base.SetID(id);

            bg.id = $"{id}.bg";
            checkmark.id = $"{id}.checkmark";
            textBg.id = $"{id}.textBg";
            text.id = $"{id}.text";
        }

        public ToggleIdentity AddMethod(UnityAction<bool> call)
        {
            toggle.onValueChanged.AddListener(call);
            return this;
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

            //设置本体
            bg.SetSizeDelta(min, min);
            bg.SetAPos(bg.sd.x / 2, 0);
            checkmark.SetSizeDelta(min, min);
            checkmark.ap = Vector2.zero;

            //设置文本
            text.rt.anchorMin = new(0, 0);
            text.rt.anchorMax = new(1, 1);
            textBg.SetSizeDelta(-min, 0);
            textBg.SetAPos(min / 2, 0);
            text.sd = Vector2.zero;
            text.ap = Vector2.zero;
        }
    }
}
