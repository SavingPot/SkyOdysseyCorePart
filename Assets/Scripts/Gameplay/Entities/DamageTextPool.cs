using DG.Tweening;
using GameCore.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameCore
{
    public class DamageTextPool
    {
        public Stack<TMP_Text> stack = new();

        public TMP_Text Get(Entity entity, float damage)
        {
            TMP_Text text = null;
            if (stack.Count == 0)
            {
                text = GameObject.Instantiate(GInit.instance.DamageTextPrefab);
                text.transform.SetParent(GameUI.worldSpaceCanvasRT);
            }
            else
            {
                text = stack.Pop();
            }

            text.transform.position = entity.transform.position;
            text.text = damage.ToString();
            text.gameObject.SetActive(true);
            text.GetComponent<Rigidbody2D>().velocity = new(UnityEngine.Random.Range(-5, 5), 9);
            Tools.InvokeAfter(0.5f, () => text.transform.DOScale(new Vector3(0, 0, 1), 0.3f).OnStepComplete(() =>
            {
                text.transform.localScale = Vector3.one;
                Recover(text);
            }));

            //生成一个新的
            return text;
        }

        public void Recover(TMP_Text text)
        {
            stack.Push(text);

            text.gameObject.SetActive(false);
        }
    }
}