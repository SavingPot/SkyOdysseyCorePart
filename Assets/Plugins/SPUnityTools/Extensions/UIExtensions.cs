using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SP.Tools.Unity
{
    public static class UIExtensions
	{
		public static void SetParentForUI(this IRectTransform c, IRectTransform p) => SetParentForUI(c.rectTransform, p.rectTransform);

		public static void SetParentForUI(this IRectTransform c, Transform p) => SetParentForUI(c.rectTransform, p);

		public static void SetParentForUI(this RectTransform trans, IRectTransform p) => SetParentForUI(trans, p.rectTransform);

		/// <summary>
		/// 设置 UI 的父对象 (锚点和锚位都会重置! [不要为 Panel 使用此方法!])
		/// </summary>
		/// <param name="trans"></param>
		/// <param name="p"></param>
		public static void SetParentForUI(this RectTransform trans, Transform p)
		{
			trans.SetParent(p);
			trans.anchoredPosition = Vector2.zero;
			trans.anchorMin = UPC.Middle;
			trans.anchorMax = UPC.Middle;
		}
	}
}
