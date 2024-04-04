using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Sirenix.OdinInspector;
using SP.Tools;
using SP.Tools.Unity;

namespace GameCore
{
    [EntityBinding(EntityID.CoinEntity), NotSummonable]
    public sealed class CoinEntity : Entity
    {
        private SpriteRenderer _spriteRenderer;
        public SpriteRenderer spriteRenderer { get { if (!_spriteRenderer) _spriteRenderer = gameObject.GetOrAddComponent<SpriteRenderer>(); return _spriteRenderer; } }

        public int coinCount { get; private set; }


        protected override void Awake()
        {
            base.Awake();

            hurtable = false;

            spriteRenderer.sprite = ModFactory.CompareTexture("ori:coin").sprite;

            renderers.Add(spriteRenderer);
            spriteRenderers.Add(spriteRenderer);

            transform.localScale = new(0.5f, 0.5f);
            transform.SetLocalPosZ(-0.001f);
        }

        public override void Initialize()
        {
            base.Initialize();

            SummonSetup();
        }

        [Button("初始化")]
        public void SummonSetup()
        {
            var countJT = customData?["ori:coin_entity"]?["count"]?.ToString();

            /* ---------------------------------- 切割字符串 --------------------------------- */
            if (countJT != null)
            {
                coinCount = countJT.ToInt();
            }
            else
            {
                Debug.LogError($"金币实体的 customData 中没有 count");
            }
        }
    }
}

