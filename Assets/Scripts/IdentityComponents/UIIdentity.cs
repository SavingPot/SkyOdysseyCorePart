using GameCore.High;
using SP.Tools.Unity;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCore
{
    [DisallowMultipleComponent]
    public class UIIdentity : IdentityComponent, IRectTransform
    {
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;

        public RectTransform rectTransform { get { if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>(); return _rectTransform; } }
        public RectTransform rt => rectTransform;
        public Vector2 ap { get => rectTransform.anchoredPosition; set => rectTransform.anchoredPosition = value; }
        public Vector2 sd { get => rectTransform.sizeDelta; set => rectTransform.sizeDelta = value; }
        public CanvasGroup canvasGroup { get { if (!_canvasGroup) _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>(); return _canvasGroup; } }

        public bool doRefresh = true;


        protected virtual void Start()
        {

        }

        public UIIdentity SetRefresh(bool value)
        {
            doRefresh = value;
            return this;
        }

        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.uiIdentities.Add(this);
        }

        protected virtual void InternalRefreshUI() { }
    }

    public class UIIdentity<T> : UIIdentity where T : UIIdentity<T>
    {
        public event Action<T> AfterRefreshing = t =>
        {
            t.InternalRefreshUI();
        };
        public event Action<T> OnUpdate = _ => { };

        protected override void Start()
        {
            base.Start();

            RefreshUI();
        }

        protected virtual void Update()
        {
            MethodAgent.TryRun(() => OnUpdate((T)this), true);
        }

        public void RefreshUI()
        {
            if (doRefresh)
            {
                MethodAgent.TryRun(() => AfterRefreshing((T)this), true);
            }
        }
    }
}
