using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameCore.High;

namespace GameCore
{
    public class ImageIdMessage : UIIdMessage<ImageIdMessage>
    {
        private Image _image;

        public Image image { get { if (!_image) _image = GetComponent<Image>(); return _image; } }


        protected override void Awake()
        {
            base.Awake();

            IdMessageCenter.imageMessages.Add(this);
        }
    }
}
