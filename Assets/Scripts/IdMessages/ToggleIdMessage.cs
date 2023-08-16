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
    public class ToggleIdMessage : UIIdMessage<ToggleIdMessage>
    {
        private Toggle _toggle;
        private ImageIdMessage _bg;
        private ImageIdMessage _checkmark;
        private ImageIdMessage _textBg;
        private TextIdMessage _text;

        public Toggle toggle { get { if (!_toggle) _toggle = GetComponent<Toggle>(); return _toggle; } }
        public ImageIdMessage bg { get { if (!_bg) _bg = this.FindComponent<ImageIdMessage>("Background"); return _bg; } }
        public ImageIdMessage checkmark { get { if (!_checkmark) _checkmark = bg.FindComponent<ImageIdMessage>("Checkmark"); return _checkmark; } }
        public ImageIdMessage textBg { get { if (!_textBg) _textBg = this.FindComponent<ImageIdMessage>("TextBackground"); return _textBg; } }
        public TextIdMessage text { get { if (!_text) _text = textBg.FindComponent<TextIdMessage>("Text"); return _text; } }



        protected override void Awake()
        {
            base.Awake();

            IdMessageCenter.toggleMessages.Add(this);
        }

        public override void SetID(string id)
        {
            base.SetID(id);

            bg.id = $"{id}.bg";
            checkmark.id = $"{id}.checkmark";
            textBg.id = $"{id}.textBg";
            text.id = $"{id}.text";
        }

        public ToggleIdMessage AddMethod(UnityAction<bool> call)
        {
            toggle.onValueChanged.AddListener(call);
            return this;
        }


        public void ResetStatusInScrollView(ScrollViewIdMessage scrollView)
        {
            SetScale(new(scrollView.gridLayoutGroup.cellSize.x, scrollView.gridLayoutGroup.cellSize.y));
        }

        [Sirenix.OdinInspector.Button("���óߴ�")]
        public void SetScale(Vector2 vec)
        {
            //��ȡ��Сֵ����������
            float min = Mathf.Min(vec.x, vec.y);
            rt.sizeDelta = vec;

            //���ñ���
            bg.SetSizeDelta(min, min);
            bg.SetAPos(bg.sd.x / 2, 0);
            checkmark.SetSizeDelta(min, min);
            checkmark.ap = Vector2.zero;

            //�����ı�
            text.rt.anchorMin = new(0, 0);
            text.rt.anchorMax = new(1, 1);
            textBg.SetSizeDelta(-min, 0);
            textBg.SetAPos(min / 2, 0);
            text.sd = Vector2.zero;
            text.ap = Vector2.zero;
        }
    }
}
