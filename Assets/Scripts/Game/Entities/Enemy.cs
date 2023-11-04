using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace GameCore
{
    public class Enemy : Creature, IHumanBodyParts<CreatureBodyPart>
    {
        [Tooltip("目标的 Transform")]
        [LabelText("目标")]
        [BoxGroup("组件")]
        public Transform targetTransform;


        public Func<Transform> FindTarget = () =>
        {
            if (!Server.isServer)
            {
                Debug.LogWarning("不应该在客户端寻找目标!");
                return null;
            }
            
            var pl = FindObjectOfType<Player>();
            return (pl && !pl.isDead) ? pl.transform : null;
        };





        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();

            //修正位置
            if (model && model.transform.localPosition != Vector3.zero)
                model.transform.localPosition = Vector3.zero;

            if (!targetTransform && isServer)
                ReFindTarget();
        }

        public void ReFindTarget()
        {
            if (!isServer)
            {
                Debug.LogWarning("不应该在客户端寻找目标!");
                return;
            }

            var tempTransform = FindTarget();

            if (tempTransform)
                targetTransform = tempTransform;
        }
    }
}
