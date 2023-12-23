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

namespace GameCore
{
    public class EntityInit : NetworkBehaviour, IHasDestroyed
    {
        /* -------------------------------------------------------------------------- */
        /*                                    独留变量                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("变量注册"), LabelText("注册了网络变量")] public bool registeredSyncVars;
        [BoxGroup("独留变量"), LabelText("生成ID"), SyncVar] public string generationId;
        [BoxGroup("变量注册")] public string waitingRegisteringVar;
        public SyncPacker.OnVarValueChangeCallback[] varHooksToUnbind;
        public Entity entity;
        public bool hasGotGeneratingId => !generationId.IsNullOrWhiteSpace();
        public bool hasDestroyed { get; protected set; }

        /* -------------------------------------------------------------------------- */
        /*                                    传出变量                                    */
        /* -------------------------------------------------------------------------- */
        [BoxGroup("生成属性"), LabelText("自定义数据"), SyncVar] public JObject customData;
        [BoxGroup("生成属性"), LabelText("数据")] public EntityData data = null;
        [BoxGroup("生成属性"), LabelText("保存ID")] public string saveId;
        public float? health;



        private static readonly Dictionary<Type, SyncAttributeData[]> TotalSyncVarAttributeTemps = new();
        public static readonly string maxHealthVarId = $"{typeof(Entity).FullName}.{nameof(Entity.maxHealth)}";
        public static readonly string healthVarId = $"{typeof(Entity).FullName}.{nameof(Entity.health)}";
        public static readonly string inventoryVarId = $"{typeof(Player).FullName}.{nameof(Player.inventory)}";


        private void OnDestroy()
        {
            hasDestroyed = true;





            /* -------------------------------------------------------------------------- */
            /*                                  注销所有同步变量                                  */
            /* -------------------------------------------------------------------------- */
            if (!registeredSyncVars)
                return;


            //注销变量
            if (isServer)
            {
                var syncVarTemps = ReadFromSyncAttributeTemps(data.behaviourType);

                StringBuilder sb = new();

                foreach (SyncAttributeData pair in syncVarTemps)
                {
                    string id = SyncPacker.GetInstanceID(sb, pair.propertyPath, netId);
                    SyncPacker.UnregisterVar(id);
                }
            }


            //取消绑定变量的钩子
            foreach (var hook in varHooksToUnbind)
            {
                SyncPacker.OnVarValueChange -= hook;
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





        async void OnValueChangeHook(NMSyncVar nm, byte[] oldValue, MethodInfo hookMethod, string varId)
        {
            //* 这是为了防止服务器上进行了赋值, 但是客户端还没创建这个实体
            if (!entity)
                await UniTask.WaitUntil(() => entity);

            if (hasDestroyed)
                return;

            if (nm.varId == varId)
                hookMethod.Invoke(entity, null);
        }

        public void AutoRegisterVars()
        {
            var syncVarTemps = ReadFromSyncAttributeTemps(data.behaviourType);

            //遍历每个属性
            StringBuilder sb = new();
            List<SyncPacker.OnVarValueChangeCallback> varHooksToUnbindTemp = new();

            //TODO: Improve readability and performance step and step
            foreach (SyncAttributeData pair in syncVarTemps)
            {
                string id = SyncPacker.GetInstanceID(sb, pair.propertyPath, netId);

                if (pair.hook != null)
                {
                    SyncPacker.OnVarValueChangeCallback callback = (nm, oldValue) => OnValueChangeHook(nm, oldValue, pair.hook, id);
                    varHooksToUnbindTemp.Add(callback);
                    SyncPacker.OnVarValueChange += callback;
                }

                if (isServer)
                {
                    if (pair.propertyPath == healthVarId)
                    {
                        if (health == null)
                            SyncPacker.RegisterVar(id, true, Rpc.ObjectToBytes(data.maxHealth));
                        else
                            SyncPacker.RegisterVar(id, true, Rpc.ObjectToBytes((float)health));
                    }
                    else if (pair.propertyPath == maxHealthVarId)
                    {
                        SyncPacker.RegisterVar(id, true, Rpc.ObjectToBytes(data.maxHealth));
                    }
                    else if (pair.propertyPath == inventoryVarId)
                    {
                        SyncPacker.RegisterVar(id, true, Rpc.ObjectToBytes(new Inventory(Player.inventorySlotCount, null)));
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

            varHooksToUnbind = varHooksToUnbindTemp.ToArray();

            //开始等待注册完毕
            StartCoroutine(IEWaitRegistering(syncVarTemps));
        }

        IEnumerator IEWaitRegistering(SyncAttributeData[] syncVarTemps)
        {
            //* 如果不是服务器, 就需要等待服务器注册
            //! 如果不等待的话会疯狂报错
            if(!isServer)
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
            }

            waitingRegisteringVar = string.Empty;
            registeredSyncVars = true;


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
