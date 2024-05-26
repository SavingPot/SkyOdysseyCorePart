using SP.Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
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
			trans.anchorMin = UIA.Middle;
			trans.anchorMax = UIA.Middle;
		}





		public static void ClearColorEffects(this Selectable s)
		{
			s.colors = new()
			{
				normalColor = s.colors.normalColor,
				highlightedColor = s.colors.normalColor,
				pressedColor = s.colors.normalColor,
				selectedColor = s.colors.normalColor,
				fadeDuration = s.colors.fadeDuration,
				disabledColor = s.colors.disabledColor,
				colorMultiplier = s.colors.colorMultiplier
			};
		}
	}
}
