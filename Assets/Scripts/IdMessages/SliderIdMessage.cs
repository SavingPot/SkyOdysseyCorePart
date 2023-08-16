using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameCore.High;

namespace GameCore
{
    public class SliderIdMessage : UIIdMessage<SliderIdMessage>
    {
        private Slider _slider;
        private ImageIdMessage _bgImage;
        private RectTransform _fillArea;
        private ImageIdMessage _fillImage;
        private RectTransform _handleSlideArea;
        private ImageIdMessage _handleImage;
        private TextIdMessage _text;

        public Slider slider { get { if (!_slider) _slider = GetComponent<Slider>(); return _slider; } }
        public ImageIdMessage bgImage { get { if (!_bgImage) _bgImage = transform.Find("Background").GetComponent<ImageIdMessage>(); return _bgImage; } }
        public RectTransform fillAreaRt { get { if (!_fillArea) _fillArea = transform.Find("Fill Area").GetComponent<RectTransform>(); return _fillArea; } }
        public ImageIdMessage fillImage { get { if (!_fillImage) _fillImage = fillAreaRt.Find("Fill").GetComponent<ImageIdMessage>(); return _fillImage; } }
        public RectTransform handleSlideArea { get { if (!_handleSlideArea) _handleSlideArea = transform.Find("Handle Slide Area").GetComponent<RectTransform>(); return _handleSlideArea; } }
        public ImageIdMessage handleImage { get { if (!_handleImage) _handleImage = handleSlideArea.Find("Handle").GetComponent<ImageIdMessage>(); return _handleImage; } }
        public TextIdMessage text { get { if (!_text) _text = transform.Find("Text").GetComponent<TextIdMessage>(); return _text; } }

        protected override void Awake()
        {
            base.Awake();

            IdMessageCenter.sliderMessages.Add(this);
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
