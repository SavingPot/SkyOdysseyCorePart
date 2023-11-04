using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace GameCore
{
    public class Enemy : Creature, IHumanBodyParts<CreatureBodyPart>
    {
        [Tooltip("Ŀ��� Transform")]
        [LabelText("Ŀ��")]
        [BoxGroup("���")]
        public Transform targetTransform;


        public Func<Transform> FindTarget = () =>
        {
            if (!Server.isServer)
            {
                Debug.LogWarning("��Ӧ���ڿͻ���Ѱ��Ŀ��!");
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

            //����λ��
            if (model && model.transform.localPosition != Vector3.zero)
                model.transform.localPosition = Vector3.zero;

            if (!targetTransform && isServer)
                ReFindTarget();
        }

        public void ReFindTarget()
        {
            if (!isServer)
            {
                Debug.LogWarning("��Ӧ���ڿͻ���Ѱ��Ŀ��!");
                return;
            }

            var tempTransform = FindTarget();

            if (tempTransform)
                targetTransform = tempTransform;
        }
    }
}
