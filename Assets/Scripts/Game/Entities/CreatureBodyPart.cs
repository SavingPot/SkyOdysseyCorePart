using DG.Tweening;
using SP.Tools.Unity;
using System;
using UnityEngine;

namespace GameCore
{
    /// <typeparam name="T">主体的 T</typeparam>
    public interface IPart<T>
    {
        T mainBody { get; }
    }

    public enum BodyPartType
    {
        /// <summary>
        /// 身体, 当然如果想自己控制生成逻辑就用这个
        /// </summary>
        Body,

        Head,
        RightArm,
        LeftArm,
        RightLeg,
        LeftLeg,
        RightFoot,
        LeftFoot,
    }

    public class CreatureBodyPart : MonoBehaviour, IPart<Creature>, ISpriteRenderer
    {
        public Creature mainBody { get; internal set; }
        public Vector2 defaultPos;
        //public Vector2 rotatePos;
        private GameObject _child;
        public GameObject child
        {
            get
            {
                if (!_child)
                {
                    _child = new("child");
                    _child.transform.SetParent(armor.transform);
                    _child.transform.localPosition = Vector3.zero;
                }

                return _child;
            }
        }
        private GameObject _armor;
        public GameObject armor
        {
            get
            {
                if (!_armor)
                {
                    _armor = new("armor");
                    _armor.transform.SetParent(transform);
                    _armor.transform.localPosition = Vector3.zero;
                }

                return _armor;
            }
        }

        private SpriteRenderer _sr;
        public SpriteRenderer sr
        {
            get
            {
                if (!_sr)
                {
                    _sr = child.GetOrAddComponent<SpriteRenderer>();
                    _sr.material = GInit.instance.spriteLitMat;
                }

                return _sr;
            }
        }
        private SpriteRenderer _armorSr;
        public SpriteRenderer armorSr
        {
            get
            {
                if (!_armorSr)
                {
                    _armorSr = armor.GetOrAddComponent<SpriteRenderer>();
                    _armorSr.material = GInit.instance.spriteLitMat;
                }

                return _armorSr;
            }
        }
        private BoxCollider2D _boxCollider;
        public BoxCollider2D boxCollider { get { if (!_boxCollider) _boxCollider = child.GetOrAddComponent<BoxCollider2D>(); return _boxCollider; } }

        public void ResetPos() => transform.localPosition = defaultPos;
        public void ResetRot() => transform.rotation = Quaternion.identity;

        private void Start()
        {
            defaultPos = transform.localPosition;
        }

        public Func<Creature> GetMain => () => GetComponentInParent<Creature>(true);
    }
}
