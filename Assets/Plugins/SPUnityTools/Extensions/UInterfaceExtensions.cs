using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SP.Tools.Unity
{
    public static class UInterfaceExtensions
    {
        public static Vector2 GetScaleToNormal(this ISpriteRenderer iSr) => new Vector2(GetWidthToNormal(iSr), GetHeightToNormal(iSr));

        public static float GetWidthToNormal(this ISpriteRenderer iSr) => iSr.sr.sprite.texture.width / iSr.sr.sprite.pixelsPerUnit * iSr.sr.transform.lossyScale.x;

        public static float GetHeightToNormal(this ISpriteRenderer iSr) => iSr.sr.sprite.texture.height / iSr.sr.sprite.pixelsPerUnit * iSr.sr.transform.lossyScale.y;

        public static void SetAPos(this IRectTransform irt, Vector2 pos) => irt.rectTransform.anchoredPosition = pos;
        public static void SetAPos(this IRectTransform irt, float x, float y) => irt.rectTransform.anchoredPosition = new Vector2(x, y);

        public static void SetAPosX(this IRectTransform irt, float x) => irt.rectTransform.anchoredPosition = new Vector2(x, irt.rectTransform.anchoredPosition.y);

        public static void SetAPosY(this IRectTransform irt, float y) => irt.rectTransform.anchoredPosition = new Vector2(irt.rectTransform.anchoredPosition.x, y);

        public static void AddAPos(this IRectTransform irt, float x, float y) => irt.rectTransform.anchoredPosition = new Vector2(irt.rectTransform.anchoredPosition.x + x, irt.rectTransform.anchoredPosition.y + y);

        public static void AddAPosX(this IRectTransform irt, float x) => irt.rectTransform.anchoredPosition = new Vector2(irt.rectTransform.anchoredPosition.x + x, irt.rectTransform.anchoredPosition.y);

        public static void AddAPosY(this IRectTransform irt, float y) => irt.rectTransform.anchoredPosition = new Vector2(irt.rectTransform.anchoredPosition.x, irt.rectTransform.anchoredPosition.y + y);

        #region SetAPosOn
        public static void SetAPosOn(this IRectTransform self, IRectTransform bas, float x, float y)
        {
            self.rectTransform.anchoredPosition = new Vector2(bas.rectTransform.anchoredPosition.x + x, bas.rectTransform.anchoredPosition.y + y);
        }

        public static void SetAPosXOn(this IRectTransform self, IRectTransform bas, float x)
        {
            self.rectTransform.anchoredPosition = new Vector2(bas.rectTransform.anchoredPosition.x + x, bas.rectTransform.anchoredPosition.y);
        }

        public static void SetAPosYOn(this IRectTransform self, IRectTransform bas, float y)
        {
            self.rectTransform.anchoredPosition = new Vector2(bas.rectTransform.anchoredPosition.x, bas.rectTransform.anchoredPosition.y + y);
        }


        public static void SetAPosOnBySize(this IRectTransform self, IRectTransform bas, float x, float y)
        {
            self.rectTransform.anchoredPosition = new Vector2(bas.rectTransform.anchoredPosition.x + bas.rectTransform.sizeDelta.x / 2 + self.rectTransform.sizeDelta.x / 2 + x, bas.rectTransform.anchoredPosition.y + bas.rectTransform.sizeDelta.y / 2 + self.rectTransform.sizeDelta.y / 2 + y);
        }

        public static void SetAPosOnBySizeRight(this IRectTransform self, IRectTransform bas, float x)
        {
            self.rectTransform.anchoredPosition = new Vector2(bas.rectTransform.anchoredPosition.x + bas.rectTransform.sizeDelta.x / 2 + self.rectTransform.sizeDelta.x / 2 + x, bas.rectTransform.anchoredPosition.y);
        }

        public static void SetAPosOnBySizeLeft(this IRectTransform self, IRectTransform bas, float x)
        {
            self.rectTransform.anchoredPosition = new Vector2(bas.rectTransform.anchoredPosition.x - bas.rectTransform.sizeDelta.x / 2 - self.rectTransform.sizeDelta.x / 2 - x, bas.rectTransform.anchoredPosition.y);
        }

        public static void SetAPosOnBySizeUp(this IRectTransform self, IRectTransform bas, float y)
        {
            self.rectTransform.anchoredPosition = new Vector2(bas.rectTransform.anchoredPosition.x, bas.rectTransform.anchoredPosition.y + bas.rectTransform.sizeDelta.y / 2 + self.rectTransform.sizeDelta.y / 2 + y);
        }

        public static void SetAPosOnBySizeDown(this IRectTransform self, IRectTransform bas, float y)
        {
            self.rectTransform.anchoredPosition = new Vector2(bas.rectTransform.anchoredPosition.x, bas.rectTransform.anchoredPosition.y - bas.rectTransform.sizeDelta.y / 2 - self.rectTransform.sizeDelta.y / 2 - y);
        }
        #endregion

        public static void SetSizeDelta(this IRectTransform irt, Vector2 vector) => irt.rectTransform.sizeDelta = vector;
        public static void SetSizeDelta(this IRectTransform irt, float x, float y) => irt.rectTransform.sizeDelta = new Vector2(x, y);

        public static void SetSizeDeltaX(this IRectTransform irt, float x) => irt.rectTransform.sizeDelta = new Vector2(x, irt.rectTransform.sizeDelta.y);

        public static void SetSizeDeltaY(this IRectTransform irt, float y) => irt.rectTransform.sizeDelta = new Vector2(irt.rectTransform.sizeDelta.x, y);

        public static void SetAnchorMin(this IRectTransform irt, float x, float y) => irt.rectTransform.anchorMin = new Vector2(x, y);

        public static void SetAnchorMax(this IRectTransform irt, float x, float y) => irt.rectTransform.anchorMax = new Vector2(x, y);

        public static void SetAnchorMinMax(this IRectTransform irt, float x, float y)
        {
            SetAnchorMin(irt, x, y);
            SetAnchorMax(irt, x, y);
        }

        public static void SetAnchorMinMax(this IRectTransform irt, float minX, float minY, float maxX, float maxY)
        {
            SetAnchorMin(irt, minX, minY);
            SetAnchorMax(irt, maxX, maxY);
        }

        public static void SetAnchorMinMax(this IRectTransform irt, Vector2 min, Vector2 max)
        {
            irt.rectTransform.anchorMin = min;
            irt.rectTransform.anchorMax = max;
        }

        public static void SetAnchorMinMax(this IRectTransform irt, Vector4 vec)
        {
            SetAnchorMin(irt, vec.x, vec.y);
            SetAnchorMax(irt, vec.z, vec.w);
        }



        public static void AddForceX(this IRigidbody2D rb, float force) => URigibodyExtensions.AddForceX(rb.rb, force);

        public static void AddForceY(this IRigidbody2D rb, float force) => URigibodyExtensions.AddForceY(rb.rb, force);

        public static void SetVelocity(this IRigidbody2D rb, float x, float y) => URigibodyExtensions.SetVelocity(rb.rb, x, y);

        public static void SetVelocityX(this IRigidbody2D rb, float x) => URigibodyExtensions.SetVelocityX(rb.rb, x);

        public static void SetVelocityY(this IRigidbody2D rb, float y) => URigibodyExtensions.SetVelocityY(rb.rb, y);

        public static void AddVelocity(this IRigidbody2D rb, float x, float y) => URigibodyExtensions.AddVelocity(rb.rb, x, y);

        public static void AddVelocity(this IRigidbody2D rb, Vector2 vec) => URigibodyExtensions.AddVelocity(rb.rb, vec);

        public static void AddVelocityX(this IRigidbody2D rb, float x) => URigibodyExtensions.AddVelocityX(rb.rb, x);

        public static void AddVelocityY(this IRigidbody2D rb, float y) => URigibodyExtensions.AddVelocityY(rb.rb, y);

        public static void SetVelocity(this IRigidbody2D rb, Vector2 newVelo) => URigibodyExtensions.SetVelocity(rb.rb, newVelo);

        public static void SetVelocityNormalized(this IRigidbody2D rb, float x, float y) => URigibodyExtensions.SetVelocityNormalized(rb.rb, x, y);

        public static void SetVelocityNormalized(this IRigidbody2D rb, Vector2 newVelo) => URigibodyExtensions.SetVelocityNormalized(rb.rb, newVelo);
    }
}
