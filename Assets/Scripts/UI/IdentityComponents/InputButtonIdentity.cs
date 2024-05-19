using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using SP.Tools.Unity;
using GameCore.High;

namespace GameCore.UI
{
    public class InputButtonIdentity : UIIdentity<InputButtonIdentity>
    {
        private ButtonIdentity _button;
        private InputFieldIdentity _field;

        public ButtonIdentity button { get { if (!_button) _button = transform.Find("ButtonPrefab").GetComponent<ButtonIdentity>(); return _button; } }
        public InputFieldIdentity field { get { if (!_field) _field = transform.Find("InputFieldPrefab").GetComponent<InputFieldIdentity>(); return _field; } }

        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.inputButtonIdentities.Add(this);
        }

        public override void SetID(string id)
        {
            base.SetID(id);

            field.id = $"{id}.inputField";
            button.id = $"{id}.button";
        }

        public InputButtonIdentity OnClickBind(UnityAction call)
        {
            button.OnClickBind(call);
            return this;
        }

        public void ResetStatusInScrollView(ScrollViewIdentity scrollView)
        {
            SetSize(new(scrollView.gridLayoutGroup.cellSize.x, scrollView.gridLayoutGroup.cellSize.y));
        }

        [Sirenix.OdinInspector.Button("���óߴ�")]
        public void SetSize(Vector2 vec)
        {
            //��ȡ��Сֵ����������
            float min = Mathf.Min(vec.x, vec.y);
            rt.sizeDelta = vec;

            //���ð�ť
            button.SetSizeDelta(min, min);
            button.SetAPos(-button.rt.sizeDelta.x / 2, 0);
            button.buttonText.SetSizeDelta(min, min);

            //���������
            field.SetSizeDelta(vec.x - button.rt.sizeDelta.x, button.rt.sizeDelta.y);
            field.SetAPos(-button.rt.sizeDelta.x / 2, 0);
        }
    }
}
