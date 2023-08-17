using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameCore.High;

namespace GameCore
{
    public class SliderIdentity : UIIdentity<SliderIdentity>
    {
        private Slider _slider;
        private ImageIdentity _bgImage;
        private RectTransform _fillArea;
        private ImageIdentity _fillImage;
        private RectTransform _handleSlideArea;
        private ImageIdentity _handleImage;
        private TextIdentity _text;

        public Slider slider { get { if (!_slider) _slider = GetComponent<Slider>(); return _slider; } }
        public ImageIdentity bgImage { get { if (!_bgImage) _bgImage = transform.Find("Background").GetComponent<ImageIdentity>(); return _bgImage; } }
        public RectTransform fillAreaRt { get { if (!_fillArea) _fillArea = transform.Find("Fill Area").GetComponent<RectTransform>(); return _fillArea; } }
        public ImageIdentity fillImage { get { if (!_fillImage) _fillImage = fillAreaRt.Find("Fill").GetComponent<ImageIdentity>(); return _fillImage; } }
        public RectTransform handleSlideArea { get { if (!_handleSlideArea) _handleSlideArea = transform.Find("Handle Slide Area").GetComponent<RectTransform>(); return _handleSlideArea; } }
        public ImageIdentity handleImage { get { if (!_handleImage) _handleImage = handleSlideArea.Find("Handle").GetComponent<ImageIdentity>(); return _handleImage; } }
        public TextIdentity text { get { if (!_text) _text = transform.Find("Text").GetComponent<TextIdentity>(); return _text; } }

        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.sliderMessages.Add(this);
        }

        public override void SetID(string id)
        {
            base.SetID(id);

            bgImage.id = $"{id}.bgImage";
            fillImage.id = $"{id}.fillImage";
            handleImage.id = $"{id}.handleImage";
            text.id = $"{id}.text";
        }
    }
}
