using SP.Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore
{
    public class ScrollViewIdentity : UIIdentity<ScrollViewIdentity>
    {
        private Image _scrollViewImage;
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private Image _viewportImage;
        private Mask _viewportMask;
        private GridLayoutGroup _gridLayoutGroup;

        public Image scrollViewImage { get { if (!_scrollViewImage) _scrollViewImage = GetComponent<Image>(); return _scrollViewImage; } }
        public ScrollRect scrollRect { get { if (!_scrollRect) _scrollRect = GetComponent<ScrollRect>(); return _scrollRect; } }
        public RectTransform content { get { if (!_content) _content = rectTransform.Find("Viewport/Content").GetComponent<RectTransform>(); return _content; } }
        public Image viewportImage { get { if (!_viewportImage) _viewportImage = rectTransform.Find("Viewport").GetComponent<Image>(); return _viewportImage; } }
        public Mask viewportMask { get { if (!_viewportMask) _viewportMask = viewportImage.GetComponent<Mask>(); return _viewportMask; } }
        public GridLayoutGroup gridLayoutGroup { get { if (!_gridLayoutGroup) _gridLayoutGroup = content.GetComponent<GridLayoutGroup>(); return _gridLayoutGroup; } }

        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.scrollViewIdentities.Add(this);
        }

        public void Clear() => content.DestroyChildren();

        public void AddChild(IRectTransform irt) => AddChild(irt.rectTransform);

        public void AddChild(RectTransform rt)
        {
            rt.SetParentForUI(content);
        }
    }
}
