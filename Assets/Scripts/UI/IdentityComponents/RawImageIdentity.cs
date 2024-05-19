using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class RawImageIdentity : UIIdentity<RawImageIdentity>
    {
        private RawImage _image;

        public RawImage image { get { if (!_image) _image = GetComponent<RawImage>(); return _image; } }


        protected override void Awake()
        {
            base.Awake();

            IdentityCenter.rawImageIdentities.Add(this);
        }
    }
}
