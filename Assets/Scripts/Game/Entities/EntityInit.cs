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

namespace GameCore
{
    public class EntityInit : NetworkBehaviour, IHasDestroyed
    {
        /* -------------------------------------------------------------------------- */
        /*                                    独留变量                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("变量ID"), LabelText("注册了网络变量")] public bool registeredSyncVars;
        [BoxGroup("独留变量"), LabelText("生成ID"), SyncVar] public string generationId;
        public Entity entity;
        public bool hasGotGeneratingId => !generationId.IsNullOrWhiteSpace();
        public string waitingRegisteringVar;
        public bool hasDestroyed { get; protected set; }

        /* -------------------------------------------------------------------------- */
        /*                                    传出变量                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("生成属性"), LabelText("自定义数据"), SyncVar] public JObject customData;
        [BoxGroup("生成属性"), LabelText("数据")] public EntityData data = null;
        [BoxGroup("生成属性"), LabelText("保存ID")] public string saveId;
        public float? health;



        private static readonly Dictionary<Type, SyncAttributeData[]> TotalSyncVarAttributeTemps = new();
        public static readonly string healthVarId = $"{typeof(Entity).FullName}.{nameof(Entity.health)}";


        private void OnDestroy()
        {
            hasDestroyed = true;
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
                //TODO: 修改代码, 使其同时适配 Player 和 普通实体
                //TODO: Also change the Drop? Have a look. so that we can combine the logics all into EntityInit
                //TODO: 使用 EntityInit 而非 Entity 来注册和销毁同步变量
                if (!isServer)
                    data = ModFactory.CompareEntity(generationId);
                if (generationId == EntityID.Player)
                    data.behaviourType = typeof(Player);

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
            StringBuilder sb = new();

            //TODO: Improve readability and performance step and step
            foreach (SyncAttributeData pair in syncVarTemps)
            {
                string id = SyncPacker.GetInstanceID(sb, pair.propertyPath, netId);

                if (pair.hook != null)
                {
                    SyncPacker.OnVarValueChange += OnValueChangeBoth;

                    void OnValueChangeBoth(NMSyncVar i, byte[] v)
                    {
                        if (i.varId == id)
                            pair.hook.Invoke(entity, null);

                        if (hasDestroyed)
                        {
                            SyncPacker.OnVarValueChange -= OnValueChangeBoth;
                            return;
                        }
                    }
                }

                if (isServer)
                {
                    //TODO: 检查是否会被劫持
                    if (pair.propertyPath == healthVarId && health != null)
                    {
                        SyncPacker.RegisterVar(id, true, Rpc.ObjectToBytes((float)health));
                    }
                    else
                    {
                        if (!pair.includeDefaultValue)
                            SyncPacker.RegisterVar(id, true, null);
                        else if (pair.defaultValueMethod == null)
                            SyncPacker.RegisterVar(id, true, pair.defaultValue);
                        else
                            SyncPacker.RegisterVar(id, true, Rpc.ObjectToBytes(pair.defaultValueMethod.Invoke(null, null)));
                    }
                }
            }

            //开始等待注册完毕
            StartCoroutine(IEWaitRegistering(syncVarTemps));
        }

        IEnumerator IEWaitRegistering(SyncAttributeData[] syncVarTemps)
        {
            StringBuilder sb = new();

            //等待注册
            foreach (var pair in syncVarTemps)
            {
                string vn = SyncPacker.GetInstanceID(sb, pair.propertyPath, netId);

                //等待数值正确
                waitingRegisteringVar = vn;
                yield return new WaitUntil(() => SyncPacker.HasVar(vn));
            }

            waitingRegisteringVar = string.Empty;
            registeredSyncVars = true;





            //!TODO: 等待 -------=-==-=--==-r1290-349120-490-12940-
            entity = generationId == EntityID.Player ? gameObject.AddComponent<Player>() : (Entity)gameObject.AddComponent(data.behaviourType);
            entity.Init = this;
            entity.customData = customData;
            entity.data = data;
        }
        internal class SyncAttributeData
        {
            public string propertyPath;
            public MethodInfo hook;
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

            foreach (var property in type.GetAllProperties())
            {
                //如果存在 SyncAttribute 就添加到列表
                if (AttributeGetter.TryGetAttribute<SyncAttribute>(property, out var att))
                {
                    string propertyPath = $"{property.DeclaringType.FullName}.{property.Name}";
                    bool includeDefaultValue = false;
                    MethodInfo hookMethod = null;
                    byte[] defaultValue = null;
                    MethodInfo defaultValueMethod = null;

                    if (!att.hook.IsNullOrWhiteSpace())
                    {
                        hookMethod = !att.hook.Contains(".") ? type.GetMethodFromAllIncludingBases(att.hook) : ModFactory.SearchUserMethod(att.hook);

                        if (hookMethod == null)
                        {
                            Debug.LogError($"无法找到同步变量 {propertyPath} 的钩子: {att.hook}");
                            continue;
                        }
                    }

                    if (AttributeGetter.TryGetAttribute<SyncDefaultValueAttribute>(property, out var defaultValueAtt))
                    {
                        if (defaultValueAtt.defaultValue != null && property.PropertyType.FullName != defaultValueAtt.defaultValue.GetType().FullName)
                        {
                            Debug.LogError($"同步变量 {propertyPath} 错误: 返回值为 {property.PropertyType.FullName} , 但默认值为 {defaultValueAtt.defaultValue.GetType().FullName}");
                            continue;
                        }
                        else
                        {
                            byte[] temp = Rpc.ObjectToBytes(defaultValueAtt.defaultValue);

                            defaultValue = temp;
                            includeDefaultValue = true;
                        }
                    }
                    else if (AttributeGetter.TryGetAttribute<SyncDefaultValueFromMethodAttribute>(property, out var defaultValueFromMethodAtt))
                    {
                        defaultValueMethod = !defaultValueFromMethodAtt.methodName.Contains(".") ? type.GetMethodFromAllIncludingBases(defaultValueFromMethodAtt.methodName) : ModFactory.SearchUserMethod(defaultValueFromMethodAtt.methodName);

                        if (defaultValueMethod == null)
                        {
                            Debug.LogError($"无法找到同步变量 {propertyPath} 的默认值获取方法 {defaultValueFromMethodAtt.methodName}");
                            continue;
                        }

                        if (property.PropertyType.FullName != defaultValueMethod.ReturnType.FullName)
                        {
                            Debug.LogError($"同步变量 {propertyPath} 错误: 返回值为 {property.PropertyType.FullName} , 但默认值为 {defaultValueMethod.ReturnType.FullName}");
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
                        propertyPath = propertyPath,
                        hook = hookMethod,
                        includeDefaultValue = includeDefaultValue,
                        defaultValue = defaultValue,
                        defaultValueMethod = defaultValueMethod,
                        valueType = property.PropertyType.FullName,
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
