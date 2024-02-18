using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SP.Tools.Unity
{
    public static class UIEnumerableExtensions
	{
		[ChineseName("检查NULL")]
		public static void CheckNullUnity<TSource>(this IList<TSource> sources) where TSource : UnityEngine.Object
		{
			for (int i = 0; i < sources.Count; i++)
				if (!sources[i])
					sources.RemoveAt(i);
		}

		public static void SetColor(this IColor iColor, float num) => SetColor(iColor, new Color(num, num, num));

		public static void SetColor(this IColor iColor, Color newColor) => iColor.color = newColor;
	}
}
