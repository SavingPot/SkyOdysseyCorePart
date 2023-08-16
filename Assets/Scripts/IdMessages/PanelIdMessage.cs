using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameCore.High;

namespace GameCore
{
    public class PanelIdMessage : UIIdMessage<PanelIdMessage>
    {
        private Image _panelImage;

        public Image panelImage { get { if (_panelImage == null) _panelImage = GetComponent<Image>(); return _panelImage; } }

        protected override void Awake()
        {
            base.Awake();

            IdMessageCenter.panelMessages.Add(this);
        }
    }
}
