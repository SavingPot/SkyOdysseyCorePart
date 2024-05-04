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

namespace GameCore
{
    public sealed class EntityInit : NetworkBehaviour, IHasDestroyed
    {
        /* -------------------------------------------------------------------------- */
        /*                                    独留变量                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("独留变量"), LabelText("生成ID"), SyncVar] public string generationId;
        [BoxGroup("独留变量"), LabelText("服务器是否完全准备好"), SyncVar] public bool isServerCompletelyReady;
        [BoxGroup("变量注册")] public string waitingRegisteringVar;
        public Entity entity;
        public bool hasGotGeneratingId => !generationId.IsNullOrWhiteSpace();
        public bool hasDestroyed { get; private set; }
        public bool hasRegisteredSyncVars;

        /* -------------------------------------------------------------------------- */
        /*                                    传出变量                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("生成属性"), LabelText("数据")] public EntityData data = null;
        public EntitySave save = null;



        private static readonly Dictionary<Type, SyncAttributeData[]> TotalSyncVarAttributeTemps = new();
        internal static bool isFirstEntityInit;


        public static readonly string maxHealthVarId = $"{typeof(Entity).FullName}.{nameof(Entity.maxHealth)}";
        public static readonly string healthVarId = $"{typeof(Entity).FullName}.{nameof(Entity.health)}";
        public static readonly string customDataVarId = $"{typeof(Entity).FullName}.{nameof(Entity.customData)}";

        public static readonly string hungerValueVarId = $"{typeof(Player).FullName}.{nameof(Player.hungerValue)}";
        public static readonly string happinessValueVarId = $"{typeof(Player).FullName}.{nameof(Player.happinessValue)}";
        public static readonly string coinVarId = $"{typeof(Player).FullName}.{nameof(Player.coin)}";
        public static readonly string inventoryVarId = $"{typeof(Player).FullName}.{nameof(Player.inventory)}";
        public static readonly string completedTasksVarId = $"{typeof(Player).FullName}.{nameof(Player.completedTasks)}";
        public static readonly string unlockedSkillsVarId = $"{typeof(Player).FullName}.{nameof(Player.unlockedSkills)}";
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

            var syncVarTemps = ReadFromSyncAttributeTemps(data.behaviourType);

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
                    data = ModFactory.CompareEntity(generationId);
                if (generationId == EntityID.Player)
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
            while (!hasGotGeneratingId)
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
            var syncVarTemps = ReadFromSyncAttributeTemps(data.behaviourType);

            //遍历每个属性
            if (isServer)
            {
                foreach (SyncAttributeData pair in syncVarTemps)
                {
                    var id = pair.fieldPath;

                    if (pair.fieldPath == maxHealthVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(data.maxHealth));
                    }
                    else if (pair.fieldPath == healthVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(save.health == null ? data.maxHealth : save.health.Value));
                    }
                    else if (pair.fieldPath == customDataVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(JsonUtils.LoadJObjectByString(save.customData)));
                    }
                    else if (pair.fieldPath == hungerValueVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.hungerValue));
                    }
                    else if (pair.fieldPath == happinessValueVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.happinessValue));
                    }
                    else if (pair.fieldPath == coinVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.coin));
                    }
                    else if (pair.fieldPath == skinHeadVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinHead);
                    }
                    else if (pair.fieldPath == skinBodyVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinBody);
                    }
                    else if (pair.fieldPath == skinLeftArmVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinLeftArm);
                    }
                    else if (pair.fieldPath == skinRightArmVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinRightArm);
                    }
                    else if (pair.fieldPath == skinLeftLegVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinLeftLeg);
                    }
                    else if (pair.fieldPath == skinRightLegVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinRightLeg);
                    }
                    else if (pair.fieldPath == skinLeftFootVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinLeftFoot);
                    }
                    else if (pair.fieldPath == skinRightFootVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, saveAsPlayer.skinRightFoot);
                    }
                    else if (pair.fieldPath == inventoryVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

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
                    else if (pair.fieldPath == completedTasksVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.completedTasks ?? new()));
                    }
                    else if (pair.fieldPath == unlockedSkillsVarId)
                    {
                        PlayerSave saveAsPlayer = (PlayerSave)save;

                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(saveAsPlayer.unlockedSkills ?? new()));
                    }
                    else if (pair.fieldPath == playerNameVarId)
                    {
                        SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(save.id));
                    }
                    else
                    {
                        if (!pair.includeDefaultValue)
                            SyncPacker.RegisterVar(id, netId, null);
                        else if (pair.defaultValueMethod == null)
                            SyncPacker.RegisterVar(id, netId, pair.defaultValue);
                        else
                            SyncPacker.RegisterVar(id, netId, Rpc.ObjectToBytes(pair.defaultValueMethod.Invoke(null, null)));
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
                            if (var.Key != id)
                                continue;

                            foreach (var item in var.Value)
                            {
                                if (item.Key == netId)
                                {
                                    goto nextPair;
                                }
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

                foreach (var var in SyncPacker.instanceVars)
                {
                    if (var.Key != id)
                        continue;

                    foreach (var item in var.Value)
                    {
                        if (item.Key == netId)
                        {
                            SyncPacker.FirstTempValue(id, entity, item.Value.value);
                            goto nextPair;
                        }
                    }

                    Debug.LogError($"实例同步变量 {id} 未被成功缓存");
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
            entity = generationId == EntityID.Player ? gameObject.AddComponent<Player>() : (Entity)gameObject.AddComponent(data.behaviourType);
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
            public string fieldPath;
            public bool includeDefaultValue;
            public byte[] defaultValue;
            public MethodInfo defaultValueMethod;
            public string valueType;
        }

        internal static SyncAttributeData[] ReadFromSyncAttributeTemps(Type type)
        {
            if (TotalSyncVarAttributeTemps.TryGetValue(type, out SyncAttributeData[] value))
            {
                return value;
            }

            //如果没有就添加
            List<SyncAttributeData> ts = new();

            foreach (var field in type.GetFields())
            {
                //如果存在 SyncAttribute 就添加到列表
                if (AttributeGetter.TryGetAttribute<SyncAttribute>(field, out var att))
                {
                    string fieldPath = $"{field.DeclaringType.FullName}.{field.Name}";
                    bool includeDefaultValue = false;
                    byte[] defaultValue = null;
                    MethodInfo defaultValueMethod = null;

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
                    else if (AttributeGetter.TryGetAttribute<SyncDefaultValueFromMethodAttribute>(field, out var defaultValueFromMethodAtt))
                    {
                        defaultValueMethod = !defaultValueFromMethodAtt.methodName.Contains(".") ? type.GetMethodFromAllIncludingBases(defaultValueFromMethodAtt.methodName) : ModFactory.SearchUserMethod(defaultValueFromMethodAtt.methodName);

                        if (defaultValueMethod == null)
                        {
                            Debug.LogError($"无法找到同步变量 {fieldPath} 的默认值获取方法 {defaultValueFromMethodAtt.methodName}");
                            continue;
                        }

                        if (field.FieldType.FullName != defaultValueMethod.ReturnType.FullName)
                        {
                            Debug.LogError($"同步变量 {fieldPath} 错误: 返回值的类型为 {field.FieldType.FullName} , 但默认值的类型为 {defaultValueMethod.ReturnType.FullName}");
                            continue;
                        }

                        includeDefaultValue = true;

                        if (defaultValueFromMethodAtt.getValueUntilRegister)
                        {
                            //? 这会在注册变量时完成
                        }
                        else
                        {
                            byte[] temp = null;

                            //TODO: 检查方法是否实例, 参数列表
                            temp = Rpc.ObjectToBytes(defaultValueMethod.Invoke(null, null));
                            defaultValue = temp;
                        }
                    }

                    ts.Add(new()
                    {
                        fieldPath = fieldPath,
                        includeDefaultValue = includeDefaultValue,
                        defaultValue = defaultValue,
                        defaultValueMethod = defaultValueMethod,
                        valueType = field.FieldType.FullName,
                    });
                }
            }


            //将数据写入字典
            value = ts.ToArray();
            TotalSyncVarAttributeTemps.Add(type, value);

            return value;
        }
    }
}
