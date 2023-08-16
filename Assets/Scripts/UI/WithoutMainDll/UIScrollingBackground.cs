using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCore;
using GameCore.High;
using Sirenix.OdinInspector;

namespace GameCore.UI
{
    public class UIScrollingBackground : MonoBehaviour
    {
        [LabelText("�����ٶ�"), Tooltip("��ֵ����, ��ֵ����")] public float scrollSpeed;
        [LabelText("������"), Tooltip("����Ϊ���һ��ͼƬ�� X �� * -1")] public float bound;
        [LabelText("Ĭ�ϵ�")] public Vector2 defaultPoint;

        public List<SpriteRenderer> renderers = new();

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
