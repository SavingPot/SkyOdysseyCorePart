using System.Collections;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using SP.Tools;
using System;

namespace GameCore
{
    public class EntityInit : NetworkBehaviour
    {
        /* -------------------------------------------------------------------------- */
        /*                                    独留变量                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("独留变量"), LabelText("生成ID"), SyncVar] public string generationId;
        public Entity entity;
        public bool hasGotGeneratingId => !generationId.IsNullOrWhiteSpace();

        /* -------------------------------------------------------------------------- */
        /*                                    传出变量                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("生成属性"), LabelText("自定义数据"), SyncVar] public JObject customData;
        [BoxGroup("生成属性"), LabelText("数据")] public EntityData data = null;
        [BoxGroup("生成属性"), LabelText("保存ID")] public string saveId;
        public float? health;

        public override void OnStartServer()
        {
            base.OnStartServer();

            //为客户端注册权限
            if (connectionToClient != null)
                netIdentity.AssignClientAuthority(connectionToClient);
        }

        public override void OnStartClient()
        {
            StartCoroutine(IECallWhenGetGeneratingId(() =>
            {
                //TODO: 修改代码, 使其同时适配 Player 和 普通实体
                //TODO: Also change the Drop? Have a look. so that we can combine the logics all into EntityInit
                //TODO: 使用 EntityInit 而非 Entity 来注册和销毁同步变量
                data ??= ModFactory.CompareEntity(generationId);

                entity = generationId == EntityID.Player ? gameObject.AddComponent<Player>() : (Entity)gameObject.AddComponent(data.behaviourType);
                entity.Init = this;
                entity.customData = customData;
                entity.data = data;

                if (health != null)
                    entity.WhenRegisteredSyncVars(() =>
                    {
                        entity.StartCoroutine(SetEntityHealth(entity, (float)health));
                    });
            }));
        }

        public IEnumerator IECallWhenGetGeneratingId(Action action)
        {
            while (!hasGotGeneratingId)
                yield return null;

            action();
        }

        static IEnumerator SetEntityHealth(Entity entity, float health)
        {
            yield return null;
            yield return null;

            entity.health = (float)health;
        }
    }
}
