using System.Collections;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using SP.Tools;
using System;
using System.Text;
using System.Reflection;
using GameCore.High;
using Cysharp.Threading.Tasks;
using System.Linq;
using GameCore.Network;
using static GameCore.EntityInit.SyncAttributeData;
using System.Linq.Expressions;
using Sirenix.Serialization;

namespace GameCore
{
    public sealed class EntityInit : NetworkBehaviour, IHasDestroyed
    {
        /* -------------------------------------------------------------------------- */
        /*                                    独留变量                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("独留变量"), LabelText("实体ID"), SyncVar] public string entityId;
        [BoxGroup("独留变量"), LabelText("服务器是否完全准备好"), SyncVar] public bool isServerCompletelyReady;
        [BoxGroup("独留变量"), LabelText("保存ID"), SyncVar] public string saveId;
        [BoxGroup("变量注册")] public string waitingRegisteringVar;
        public Entity entity;
        public bool hasGotEntityId => !entityId.IsNullOrWhiteSpace();
        public bool hasDestroyed { get; private set; }
        public bool hasRegisteredSyncVars;

        /* -------------------------------------------------------------------------- */
        /*                                    传出变量                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("生成属性"), LabelText("数据")] public EntityData data = null;
        public EntitySave save = null;



        private static readonly Dictionary<Type, SyncAttributeData[]> TotalSyncVarAttributeTemps = new();
        internal static bool isFirstEntityInit;


        public static readonly string regionIndexVarId = $"{typeof(Entity).FullName}.{nameof(Entity.regionIndex)}";
        public static readonly string healthVarId = $"{typeof(Entity).FullName}.{nameof(Entity.health)}";
        public static readonly string customDataVarId = $"{typeof(Entity).FullName}.{nameof(Entity.customData)}";

        public static readonly string manaVarId = $"{typeof(Player).FullName}.{nameof(Player.mana)}";
        public static readonly string coinVarId = $"{typeof(Player).FullName}.{nameof(Player.coin)}";
        public static readonly string inventoryVarId = $"{typeof(Player).FullName}.{nameof(Player.inventory)}";
        public static readonly string completedTasksVarId = $"{typeof(Player).FullName}.{nameof(Player.completedTasks)}";
        public static readonly string unlockedSkillsVarId = $"{typeof(Player).FullName}.{nameof(Player.unlockedSkills)}";
        public static readonly string skillPointsVarId = $"{typeof(Player).FullName}.{nameof(Player.skillPoints)}";
        public static readonly string playerNameVarId = $"{typeof(Player).FullName}.{nameof(Player.playerName)}";
        public static readonly string skinHeadVarId = $"{typeof(Player).FullName}.{nameof(Player.skinHead)}";
        public static readonly string skinBodyVarId = $"{typeof(Player).FullName}.{nameof(Player.skinBody)}";
        public static readonly string skinLeftArmVarId = $"{typeof(Player).FullName}.{nameof(Player.skinLeftArm)}";
        public static readonly string skinRightArmVarId = $"{typeof(Player).FullName}.{nameof(Player.skinRightArm)}";
        public static readonly string skinLeftLegVarId = $"{typeof(Player).FullName}.{nameof(Player.skinLeftLeg)}";
        public static readonly string skinRightLegVarId = $"{typeof(Player).FullName}.{nameof(Player.skinRightLeg)}";
        public static readonly string skinLeftFootVarId = $"{typeof(Player).FullName}.{nameof(Player.skinLeftFoot)}";
        public static readonly string skinRightFootVarId = $"{typeof(Player).FullName}.{nameof(Player.skinRightFoot)}";







        private void OnDestroy()
        {
            //调用生成绑定 (如果实体在生成时被瞬间销毁)
            if (entity)
                CallGenerationBindings();

            hasDestroyed = true;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();



            /* -------------------------------------------------------------------------- */
            /*                                  注销所有同步变量                                  */
            /* -------------------------------------------------------------------------- */
            if (!hasRegisteredSyncVars)
            {
                Debug.LogError("严重错误!! 该实体在服务器未注册好同步变量，就被销毁了!!!!!!!", this);
                return;
            }

            var syncVarTemps = ReadFromSyncAttributeTemps(data);

            foreach (SyncAttributeData pair in syncVarTemps)
            {
                SyncPacker.UnregisterVar(pair.fieldPath, netId);
            }
        }

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
                if (!isServer)
                    data = ModFactory.CompareEntity(entityId);
                if (entityId == EntityID.Player)
                    data.behaviourType = typeof(Player);
                if (data == null)
                    Debug.LogError($"严重错误!! 该实体的 {nameof(data)} 为空!!!!!!!", this);
                if (save == null)
                    Debug.LogError($"严重错误!! 该实体的存档数据为空!!!!!!!", this);

                AutoRegisterVars();
            }));
        }

        public IEnumerator IECallWhenGetGeneratingId(Action action)
        {
            while (!hasGotEntityId)
                yield return null;

            action();
        }



        public IEnumerator IEWaitForCondition(Func<bool> conditionToWait, Action action)
        {
            yield return new WaitWhile(conditionToWait);

            action();
        }





        public void AutoRegisterVars()
        {
            var syncVarTemps = ReadFromSyncAttributeTemps(data);

            //遍历每个属性
            if (isServer)
            {
                PlayerSave saveAsPlayer = save as PlayerSave;

                foreach (SyncAttributeData pair in syncVarTemps)
                {
                    var id = pair.fieldPath;

                    if (id == regionIndexVarId)
                    {
                        //注明：在 SummonEntity 中会调用 GameObject.Initialize 来初始化 EntityInit，此时 transform.position 也被赋值了
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(PosConvert.WorldPosToRegionIndex(transform.position)));
                    }
                    else if (id == healthVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(save.health == null ? Entity.GetDefaultMaxHealth(data) : save.health.Value));
                    }
                    else if (id == customDataVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(JsonUtils.LoadJObjectByString(save.customData)));
                    }
                    else if (id == manaVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.mana));
                    }
                    else if (id == coinVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.coin));
                    }
                    else if (id == skinHeadVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinHead);
                    }
                    else if (id == skinBodyVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinBody);
                    }
                    else if (id == skinLeftArmVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinLeftArm);
                    }
                    else if (id == skinRightArmVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinRightArm);
                    }
                    else if (id == skinLeftLegVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinLeftLeg);
                    }
                    else if (id == skinRightLegVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinRightLeg);
                    }
                    else if (id == skinLeftFootVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinLeftFoot);
                    }
                    else if (id == skinRightFootVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinRightFoot);
                    }
                    else if (id == inventoryVarId)
                    {
                        if (saveAsPlayer.inventory == null)
                        {
                            SyncPacker.RegisterVar(id, netId, null);
                        }
                        else
                        {
                            //恢复物品栏
                            //这一行代码的意义是如果物品栏栏位数更改了, 可以保证栏位数和预想的一致
                            saveAsPlayer.inventory.SetSlotCount(Player.inventorySlotCountConst);

                            SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.inventory));
                        }
                    }
                    else if (id == completedTasksVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.completedTasks ?? new()));
                    }
                    else if (id == unlockedSkillsVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.unlockedSkills ?? new()));
                    }
                    else if (id == skillPointsVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.skillPoints));
                    }
                    else if (id == playerNameVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(save.id));
                    }
                    else
                    {
                        if (!pair.includeDefaultValue)
                            SyncPacker.RegisterVar(id, netId, null);
                        else if (pair.defaultValueMethodCallType == DefaultValueMethodCallType.DontCall)
                            SyncPacker.RegisterVar(id, netId, pair.defaultValue);
                        else if (pair.defaultValueMethodCallType == DefaultValueMethodCallType.NoneParam)
                            SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(pair.defaultValueActionNoneParam()));
                        else if (pair.defaultValueMethodCallType == DefaultValueMethodCallType.EntityDataParam)
                            SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(pair.defaultValueActionEntityDataParam(data)));
                        else if (pair.defaultValueMethodCallType == DefaultValueMethodCallType.EntitySaveParam)
                            SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(pair.defaultValueActionEntitySaveParam(save)));
                        else if (pair.defaultValueMethodCallType == DefaultValueMethodCallType.EntityDataAndSaveParam)
                            SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(pair.defaultValueActionEntityDataAndSaveParam(data, save)));
                    }
                }
            }
            //* 如果不是服务器, 就需要等待服务器注册
            //! 如果不等待的话会疯狂报错
            else
            {
                //向服务器请求所有的实例同步变量
                if (isFirstEntityInit)
                {
                    isFirstEntityInit = false;

                    //等一帧是为了防止别的实体还没同步
                    MethodAgent.CallNextFrame(() =>
                    {
                        Client.Send(new NMRequestInstanceVars());
                    });
                }
            }

            //开始等待
            StartCoroutine(IEWaitRegistering(syncVarTemps));
        }

        IEnumerator IEWaitRegistering(SyncAttributeData[] syncVarTemps)
        {
            StringBuilder sb = new();

            /* --------------------------------- 等待变量注册 --------------------------------- */
            if (!isServer)
            {
                yield return new WaitUntil(() => isServerCompletelyReady);

                foreach (var pair in syncVarTemps)
                {
                    string id = pair.fieldPath;
                    waitingRegisteringVar = id;

                    //等待变量被注册并将其缓存
                    while (true)
                    {
                        foreach (var var in SyncPacker.instanceVars)
                        {
                            if (var.varId == id && var.instance == netId)
                            {
                                goto nextPair;
                            }
                        }

                        yield return null;
                    }

                //继续检测下一个变量
                nextPair:
                    continue;
                }
            }


            /* --------------------------------- 将所有变量缓存 (这些必须在一帧内完成) -------------------------------- */

            //必须先创建实体组件，才可以缓存
            CreateEntityComponent();

            foreach (var pair in syncVarTemps)
            {
                string id = pair.fieldPath;
                waitingRegisteringVar = id;

                foreach (var variant in SyncPacker.instanceVars)
                {
                    if (variant.varId != id || variant.instance != netId)
                        continue;

                    SyncPacker.FirstTempValue(id, entity, variant.value);
                    goto nextPair;
                }

                Debug.LogError($"实例同步变量 {id} 未被成功缓存");

            //继续缓存下一个变量
            nextPair: { }
            }

            /* --------------------------------- 注册完成 --------------------------------- */
            CompleteEntityComponentCreation();
        }

        void CreateEntityComponent()
        {
            entity = entityId == EntityID.Player ? gameObject.AddComponent<Player>() : (Entity)gameObject.AddComponent(data.behaviourType);
            entity.Init = this;
            entity.data = data;

            SyncPacker.EntitiesIDTable.Add(netId, entity);
        }

        void CompleteEntityComponentCreation()
        {
            if (isServer)
                isServerCompletelyReady = true;

            waitingRegisteringVar = string.Empty;
            hasRegisteredSyncVars = true;
            entity.Initialize();
            entity.AfterInitialization();

            //调用生成绑定
            CallGenerationBindings();
        }

        void CallGenerationBindings()
        {
            if (EntityCenter.entityGenerationBindings.TryGetValue(saveId, out var binding))
            {
                binding.action(entity);
                EntityCenter.entityGenerationBindings.Remove(saveId);
            }
        }





        public static EntityInit GetEntityInitByNetIdWithoutCheck(uint netIdToFind)
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

                return identity.GetComponent<EntityInit>();
#else
                return NetworkClient.spawned[netIdToFind].GetComponent<EntityInit>();
#endif
            }
            else
            {
                return null;
            }
        }







        internal class SyncAttributeData
        {
            internal string fieldPath;
            internal bool includeDefaultValue;
            internal byte[] defaultValue;
            internal MethodInfo defaultValueMethod;
            internal Func<object> defaultValueActionNoneParam;
            internal Func<EntityData, object> defaultValueActionEntityDataParam;
            internal Func<EntitySave, object> defaultValueActionEntitySaveParam;
            internal Func<EntityData, EntitySave, object> defaultValueActionEntityDataAndSaveParam;
            internal DefaultValueMethodCallType defaultValueMethodCallType;
            internal string valueType;

            internal enum DefaultValueMethodCallType : byte
            {
                NoneParam,
                EntityDataParam,
                EntitySaveParam,
                EntityDataAndSaveParam,
                DontCall,
            }
        }

        internal static SyncAttributeData[] ReadFromSyncAttributeTemps(EntityData data)
        {
            if (TotalSyncVarAttributeTemps.TryGetValue(data.behaviourType, out SyncAttributeData[] value))
            {
                return value;
            }

            //如果没有就添加
            List<SyncAttributeData> ts = new();

            foreach (var field in data.behaviourType.GetFields())
            {
                //如果存在 SyncAttribute 就添加到列表
                if (AttributeGetter.TryGetAttribute<SyncAttribute>(field, out var att))
                {
                    string fieldPath = $"{field.DeclaringType.FullName}.{field.Name}";
                    bool includeDefaultValue = false;
                    byte[] defaultValue = null;
                    MethodInfo defaultValueMethod = null;
                    SyncDefaultValueFromMethodAttribute defaultValueFromMethodAtt = null;
                    DefaultValueMethodCallType defaultValueMethodCallType = DefaultValueMethodCallType.DontCall;
                    Func<object> defaultValueActionNoneParam = null;
                    Func<EntityData, object> defaultValueActionEntityDataParam = null;
                    Func<EntitySave, object> defaultValueActionEntitySaveParam = null;
                    Func<EntityData, EntitySave, object> defaultValueActionEntityDataAndSaveParam = null;

                    if (AttributeGetter.TryGetAttribute<SyncDefaultValueAttribute>(field, out var defaultValueAtt))
                    {
                        if (defaultValueAtt.defaultValue != null && field.FieldType.FullName != defaultValueAtt.defaultValue.GetType().FullName)
                        {
                            Debug.LogError($"同步变量 {fieldPath} 错误: 返回值的类型为 {field.FieldType.FullName} , 但默认值的类型为 {defaultValueAtt.defaultValue.GetType().FullName}");
                            continue;
                        }
                        else
                        {
                            byte[] temp = Rpc.ObjectToBytes(defaultValueAtt.defaultValue);

                            defaultValue = temp;
                            includeDefaultValue = true;
                        }
                    }
                    else if (AttributeGetter.TryGetAttribute(field, out defaultValueFromMethodAtt))
                    {
                        defaultValueMethod = !defaultValueFromMethodAtt.methodName.Contains(".") ? data.behaviourType.GetMethodFromAllIncludingBases(defaultValueFromMethodAtt.methodName) : ModFactory.SearchUserMethod(defaultValueFromMethodAtt.methodName);

                        //检查方法是否存在
                        if (defaultValueMethod == null)
                        {
                            Debug.LogError($"无法找到同步变量 {fieldPath} 的默认值获取方法 {defaultValueFromMethodAtt.methodName}");
                            continue;
                        }

                        //检查返回类型
                        if (field.FieldType.FullName != defaultValueMethod.ReturnType.FullName)
                        {
                            Debug.LogError($"同步变量 {fieldPath} 错误: 返回值的类型为 {field.FieldType.FullName} , 但默认值的类型为 {defaultValueMethod.ReturnType.FullName}");
                            continue;
                        }

                        //检查方法是否是静态方法
                        if (!defaultValueMethod.IsStatic)
                        {
                            Debug.LogError($"同步变量 {fieldPath} 的默认值获取方法 {defaultValueFromMethodAtt.methodName} 必须是静态方法");
                            continue;
                        }

                        //获取方法调用类型
                        switch (defaultValueMethod.GetParameters())
                        {
                            //无参数
                            case var parameters when parameters.Length == 0:
                                //只有临时获取才需要缓存
                                if (defaultValueFromMethodAtt.getValueUntilRegister)
                                {
                                    defaultValueActionNoneParam = Expression.Lambda<Func<object>>(Expression.Call(defaultValueMethod).Box()).Compile();
                                }
                                defaultValueMethodCallType = DefaultValueMethodCallType.NoneParam;
                                break;

                            //参数为 EntityData
                            case var parameters when parameters.Length == 1 && parameters[0].ParameterType.FullName == typeof(EntityData).FullName:
                                //只有临时获取才需要缓存
                                if (defaultValueFromMethodAtt.getValueUntilRegister)
                                {
                                    //缓存方法
                                    var arguments = new ParameterExpression[] { Expression.Parameter(typeof(EntityData)) };
                                    defaultValueActionEntityDataParam = Expression.Lambda<Func<EntityData, object>>(Expression.Call(defaultValueMethod, arguments).Box(), arguments).Compile();
                                }
                                defaultValueMethodCallType = DefaultValueMethodCallType.EntityDataParam;
                                break;

                            //参数为 EntitySave
                            case var parameters when parameters.Length == 1 && parameters[0].ParameterType.FullName == typeof(EntitySave).FullName:
                                if (defaultValueFromMethodAtt.getValueUntilRegister)
                                {
                                    //缓存方法
                                    var arguments = new ParameterExpression[] { Expression.Parameter(typeof(EntitySave)) };
                                    defaultValueActionEntitySaveParam = Expression.Lambda<Func<EntitySave, object>>(Expression.Call(defaultValueMethod, arguments).Box(), arguments).Compile();
                                    defaultValueMethodCallType = DefaultValueMethodCallType.EntitySaveParam;
                                }
                                else
                                {
                                    Debug.LogError($"同步变量 {fieldPath} 的默认值获取方法 {defaultValueFromMethodAtt.methodName} 不能包含 EntitySave 参数, 因为它的值在注册变量时才会确定");
                                    continue;
                                }
                                break;

                            //参数为 EntityData, EntitySave
                            case var parameters when parameters.Length == 2 && parameters[0].ParameterType.FullName == typeof(EntityData).FullName && parameters[1].ParameterType.FullName == typeof(EntitySave).FullName:
                                if (defaultValueFromMethodAtt.getValueUntilRegister)
                                {
                                    //缓存方法
                                    var arguments = new ParameterExpression[] { Expression.Parameter(typeof(EntityData)), Expression.Parameter(typeof(EntitySave)) };
                                    defaultValueActionEntityDataAndSaveParam = Expression.Lambda<Func<EntityData, EntitySave, object>>(Expression.Call(defaultValueMethod, arguments).Box(), arguments).Compile();
                                    defaultValueMethodCallType = DefaultValueMethodCallType.EntityDataAndSaveParam;
                                }
                                else
                                {
                                    Debug.LogError($"同步变量 {fieldPath} 的默认值获取方法 {defaultValueFromMethodAtt.methodName} 不能包含 EntitySave 参数, 因为它的值在注册变量时才会确定");
                                    continue;
                                }
                                break;

                            //不受支持的参数列表
                            default:
                                Debug.LogError($"同步变量 {fieldPath} 的默认值获取方法 {defaultValueFromMethodAtt.methodName} 的参数列表不受支持");
                                continue;
                        }

                        //获取成功
                        includeDefaultValue = true;

                        if (defaultValueFromMethodAtt.getValueUntilRegister)
                        {
                            //? 这会在注册变量时完成
                        }
                        else
                        {
                            //获取默认值
                            defaultValue = Rpc.ObjectToBytes(defaultValueMethod.Invoke(null, defaultValueMethodCallType switch
                            {
                                DefaultValueMethodCallType.NoneParam => null,
                                DefaultValueMethodCallType.EntityDataParam => new object[] { data },
                                _ => throw new NotImplementedException(defaultValueMethodCallType.ToString()),
                            }));

                            //防止二次调用
                            defaultValueMethodCallType = DefaultValueMethodCallType.DontCall;
                        }
                    }

                    ts.Add(new()
                    {
                        fieldPath = fieldPath,
                        includeDefaultValue = includeDefaultValue,
                        defaultValue = defaultValue,
                        defaultValueMethod = defaultValueMethod,
                        defaultValueMethodCallType = defaultValueMethodCallType,
                        defaultValueActionNoneParam = defaultValueActionNoneParam,
                        defaultValueActionEntityDataParam = defaultValueActionEntityDataParam,
                        defaultValueActionEntitySaveParam = defaultValueActionEntitySaveParam,
                        defaultValueActionEntityDataAndSaveParam = defaultValueActionEntityDataAndSaveParam,
                        valueType = field.FieldType.FullName,
                    });
                }
            }


            //将数据写入字典
            value = ts.ToArray();
            TotalSyncVarAttributeTemps.Add(data.behaviourType, value);

            return value;
        }
    }
}
