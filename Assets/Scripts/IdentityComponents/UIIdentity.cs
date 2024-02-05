using GameCore.High;
using SP.Tools.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCore
{
    [DisallowMultipleComponent]
    public class UIIdentity : MonoBehaviour, IStringId, IRectTransform
    {
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;

        public RectTransform rectTransform { get { if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>(); return _rectTransform; } }
        public RectTransform rt => rectTransform;
        public Vector2 ap { get => rectTransform.anchoredPosition; set => rectTransform.anchoredPosition = value; }
        public Vector2 sd { get => rectTransform.sizeDelta; set => rectTransform.sizeDelta = value; }
        public CanvasGroup canvasGroup { get { if (!_canvasGroup) _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>(); return _canvasGroup; } }

        public bool doRefresh = true;




        public string id { get; set; }



        ///<summary>
        ///string1 = type(方法种类), string2 = param(参数)
        ///</summary>
        public Action<string, string> CustomMethod = (_, _) => { };







        protected virtual void Awake()
        {
            IdentityCenter.uiIdentities.Add(this);
        }

        protected virtual void Start()
        {

        }

        public virtual void SetID(string id)
        {
            this.id = id;
            gameObject.name = id;
        }

        private void OnDestroy()
        {
            IdentityCenter.Remove(this);
        }






        protected virtual void InternalRefreshUI() { }
    }

    public class UIIdentity<T> : UIIdentity where T : UIIdentity<T>
    {
        public event Action<T> AfterRefreshing = t =>
        {
            t.InternalRefreshUI();
        };
        public event Action<T> OnUpdate;

        protected override void Start()
        {
            base.Start();

            RefreshUI();
        }

        protected virtual void Update()
        {
            MethodAgent.TryRun(() => OnUpdate?.Invoke((T)this), true);
        }

        public void RefreshUI()
        {
            if (!doRefresh)
                return;

            MethodAgent.TryRun(() => AfterRefreshing((T)this), true);
        }
    }
}
