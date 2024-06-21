using System;
using System.Collections.Generic;
using GameCore.High;
using GameCore.Network;
using Mirror;
using UnityEngine;

namespace GameCore
{
    public static class EntityCenter
    {
        public class EntityGenerationBinding
        {
            public float createdTime;
            public Action<Entity> action;

            public EntityGenerationBinding(float createdTime, Action<Entity> action)
            {
                this.createdTime = createdTime;
                this.action = action;
            }
        }
        public static readonly List<Entity> all = new();
        public static readonly Dictionary<string, EntityGenerationBinding> entityGenerationBindings = new();
        public static Action<Entity> OnAddEntity = _ => { };
        public static Action<Entity> OnRemoveEntity = _ => { };

        public static void BindGenerationEvent(string saveId, Action<Entity> action)
        {
            if (entityGenerationBindings.ContainsKey(saveId))
            {
                Debug.LogError($"EntityGenerationBinding for {saveId} already exists.");
                return;
            }

            entityGenerationBindings.Add(saveId, new(Tools.time, action));
        }

        public static void AddEntity(Entity entity)
        {
            all.Add(entity);
            OnAddEntity(entity);
        }

        public static void RemoveEntity(Entity entity)
        {
            all.Remove(entity);
            OnRemoveEntity(entity);
        }

        public static void Update()
        {
            if (Server.isServer)
            {
                float frameTime = Performance.frameTime;

                foreach (var entity in all)
                {
                    var invincibleTime = entity.invincibleTime;

                    if (invincibleTime > 0)
                        entity.invincibleTime = invincibleTime - Mathf.Min(frameTime, invincibleTime);
                }
            }

            //检查过时的实体生成绑定
            foreach (var binding in entityGenerationBindings)
            {
                if (Tools.time - binding.Value.createdTime > 30) //30秒后过期
                {
                    entityGenerationBindings.Remove(binding.Key);
                    Debug.LogError($"EntityGenerationBinding for {binding.Key} has expired.");
                }
            }
        }








        public static Component GetEntityByNetId(uint netIdToFind, Type type)
        {
#if DEBUG
            //uint.MaxValue 是我设定的无效值, 如果 netIdToFind 为 uint.MaxValue 是几乎不可能找到合适的 NetworkIdentity 的
            if (!NetworkClient.spawned.TryGetValue(netIdToFind, out NetworkIdentity identity))
            {
                if (netIdToFind == uint.MaxValue)
                    Debug.LogError($"无法找到无效实体 {type.FullName}");
                else
                    Debug.LogError($"无法找到实体 {type.FullName} {netIdToFind}");
                return null;
            }

            return identity.GetComponent(type);
#else
            return NetworkClient.spawned[netIdToFind].GetComponent(type);
#endif
        }

        public static T GetEntityByNetId<T>(uint netIdToFind) where T : Entity
        {
#if DEBUG
            //uint.MaxValue 是我设定的无效值, 如果 netIdToFind 为 uint.MaxValue 是几乎不可能找到合适的 NetworkIdentity 的
            if (!NetworkClient.spawned.TryGetValue(netIdToFind, out NetworkIdentity identity))
            {
                if (netIdToFind == uint.MaxValue)
                    Debug.LogError($"无法找到无效 {typeof(T).FullName}");
                else
                    Debug.LogError($"无法找到 {typeof(T).FullName} {netIdToFind}");
                return null;
            }

            return identity.GetComponent<T>();
#else
            return NetworkClient.spawned[netIdToFind].GetComponent<T>();
#endif
        }

        public static Entity GetEntityByNetIdWithInvalidCheck(uint netIdToFind)
        {
            if (netIdToFind != uint.MaxValue)
            {
#if DEBUG
                //uint.MaxValue 是我设定的无效值, 如果 netIdToFind 为 uint.MaxValue 是几乎不可能找到合适的 NetworkIdentity 的
                if (!NetworkClient.spawned.TryGetValue(netIdToFind, out NetworkIdentity identity))
                {
                    Debug.LogError($"无法找到 Entity {netIdToFind}");
                    return null;
                }

                return identity.GetComponent<Entity>();
#else
                return NetworkClient.spawned[netIdToFind].GetComponent<Entity>();
#endif
            }
            else
            {
                return null;
            }
        }

        public static bool TryGetEntityByNetId<T>(uint netIdToFind, out T result) where T : Entity
        {
            result = GetEntityByNetId<T>(netIdToFind);

            return result;
        }









        [RuntimeInitializeOnLoadMethod]
        private static void BindMethods()
        {
            GM.OnUpdate += Update;
        }
    }
}