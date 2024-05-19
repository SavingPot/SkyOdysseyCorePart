using System;
using System.Collections.Generic;
using GameCore.High;
using Mirror;
using UnityEngine;

namespace GameCore
{
    public static class EntityCenter
    {
        public static readonly List<Entity> all = new();
        public static Action<Entity> OnAddEntity = _ => { };
        public static Action<Entity> OnRemoveEntity = _ => { };

        public static void BindEventOnEntitySummoned(string targetEntitySaveId, Action<Entity> action)
        {
            void Event(Entity entity)
            {
                if (entity.Init.save.saveId == targetEntitySaveId)
                {
                    action(entity);
                    OnAddEntity -= Event;
                }
            }

            OnAddEntity += Event;
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
        }







        /* -------------------------------------------------------------------------- */
        /*                                  Static 方法                                 */
        /* -------------------------------------------------------------------------- */

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