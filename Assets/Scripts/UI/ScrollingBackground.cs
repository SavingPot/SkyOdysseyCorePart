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
        [LabelText("�����ٶ�"), Tooltip("��ֵ����, ��ֵ����")] public float scrollSpeed;
        [LabelText("������"), Tooltip("����Ϊ���һ��ͼƬ�� X �� * -1")] public float bound;
        [LabelText("Ĭ�ϵ�")] public Vector2 defaultPoint;

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
