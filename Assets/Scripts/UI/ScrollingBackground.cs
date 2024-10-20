using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCore;
using Sirenix.OdinInspector;
using GameCore.High;
using SP.Tools.Unity;

namespace GameCore.UI
{
    public class ScrollingBackground : InfiniteBackground
    {
        [LabelText("滚动速度"), Tooltip("负值向左, 正值向右")] public float scrollSpeed;
        [LabelText("滚动到"), Tooltip("设置为最后一张图片的 X 轴 * -1")] public float bound;
        [LabelText("默认点")] public Vector2 defaultPoint;

        private void Update()
        {
            transform.Translate(scrollSpeed * Performance.frameTime, 0, 0);

            if (transform.position.x < bound)
            {
                transform.position = defaultPoint;
            }
        }
    }
}
