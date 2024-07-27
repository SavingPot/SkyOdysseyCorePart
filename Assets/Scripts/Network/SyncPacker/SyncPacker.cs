using Cysharp.Threading.Tasks;
using GameCore.High;
using Mirror;
using SP.Tools;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace GameCore.Network
{
    /// <summary>
    /// Sync Var Packer
    /// </summary>
    //!     这个类的性能对游戏性能影响巨大!!!!!!!!!!!!!!!!! 一定要好好优化
    //TODO: 这个类的性能对游戏性能影响巨大!!!!!!!!!!!!!!!!! 一定要好好优化
    public static class SyncPacker
    {
        public delegate void FirstTempValueDelegate(string id, Entity instance, byte[] newValueBytes);
        public delegate void OnValueChangeCallback(string id, Entity instance, byte[] oldValueBytes, byte[] newValueBytes);

        public static FirstTempValueDelegate FirstTempValue;
        public static OnValueChangeCallback OnValueChange;

        public static readonly List<NMSyncVar> staticVars = new();
        public static readonly List<NMSyncVar> instanceVars = new();

        /// <summary>
        /// 发送同步变量的间隔
        /// </summary>
        private static int _sendInterval;
        public static int sendInterval
        {
            get => _sendInterval;
            set
            {
                if (value <= 0)
                {
                    //按 defaultSendInterval 毫秒来同步
                    Debug.LogError($"{nameof(sendInterval)} 值不应小于或等于 0, 否则程序将停止响应, 已按照 {nameof(defaultSendInterval)}:{defaultSendInterval} 处理");
                    value = defaultSendInterval;
                }

                _sendInterval = value;
            }
        }

        public const int defaultSendInterval = 100;
        public static bool initialized { get; private set; }
        public static readonly Dictionary<uint, Entity> EntitiesIDTable = new();



        #region 注册变量

        /// <summary>
        /// 注册同步变量, 只能在服务端调用
        /// </summary>
        /// <param name="varId"></param>
        public static void RegisterVar(string varId, uint instance, byte[] defaultValue)
        {
            if (varId.IsNullOrWhiteSpace())
            {
                Debug.LogError("不可以注册空变量");
                return;
            }

            if (!Server.isServer)
            {
                Debug.LogError("非服务器不可以注册变量");
                return;
            }

            NMRegisterSyncVar var = new(varId, instance, defaultValue, Tools.time);

            //保证服务器立马注册
#if DEBUG
            try
            {
#endif
                LocalRegisterSyncVar(var);
#if DEBUG
            }
            catch (Exception ex)
            {
                Debug.LogError($"注册变量 {varId} 失败: {ex}");
            }
#endif

            Server.Send(var);
        }

        /// <summary>
        /// 取消注册同步变量, 只能在服务端调用
        /// </summary>
        /// <param name="varId"></param>
        public static void UnregisterVar(string varId, uint instance)
        {
            if (!Server.isServer)
            {
                Debug.LogWarning("非服务器无法注销同步变量");
                return;
            }

            Server.Send(new NMUnregisterSyncVar(varId, instance));
        }

        #endregion

        static async void StartAutoSync()
        {
            //按 sendInterval 毫秒的间隔来同步
            await Task.Delay(sendInterval);

            //如果服务器关闭了就停止
            if (!Server.isServer)
                return;

            try
            {
                Sync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"自动同步变量失败: {ex}");
            }

            StartAutoSync();
        }

        static Dictionary<string, object> staticVarLastSyncValues = new();
        static Dictionary<string, Dictionary<uint, object>> instanceVarLastSyncValues = new();

        /// <summary>
        /// 立即同步所有改变了的变量
        /// </summary>
        public static void Sync()
        {
            if (!Server.isServer)
            {
                Debug.LogError("非服务器不可以同步变量");
                return;
            }


            Dictionary<string, object> newStaticVarLastSyncValues = new();
            Dictionary<string, Dictionary<uint, object>> newInstanceVarLastSyncValues = new();


            //遍历静态变量（时间复杂度为 O(n)）
            for (int i = 0; i < staticVars.Count; i++)
            {
                var variant = staticVars[i];
                var currentValue = GetStaticFieldValue(variant.varId);

                //记录下最新值供下次比较
                newStaticVarLastSyncValues.Add(variant.varId, currentValue);



                //* 如果值没有变化就跳过
                if (!staticVarLastSyncValues.TryGetValue(variant.varId, out var lastSyncValue) ||
                    Equals(currentValue, lastSyncValue))
                    continue;



                //调用值改变方法
                var currentValueBytes = Rpc.ObjectToBytes(currentValue);
                OnValueChange(variant.varId, null, Rpc.ObjectToBytes(variant.valueLastSync), currentValueBytes);

                //更改列表中的值
                var newVar = new NMSyncVar(variant.varId, uint.MaxValue, currentValueBytes, currentValueBytes, Tools.time);
                staticVars[i] = newVar;

                //将新值发送给所有客户端
                Server.Send(newVar);
            }





            //遍历实例变量（时间复杂度为 O(n)）
            for (int i = 0; i < instanceVars.Count; i++)
            {
                var variant = instanceVars[i];
                if (!EntitiesIDTable.TryGetValue(variant.instance, out var entity)) continue; //? 可能获取不到实例
                var currentValue = GetInstanceFieldValue(variant.varId, entity);

                //记录下最新值供下次比较
                if (newInstanceVarLastSyncValues.TryGetValue(variant.varId, out var instanceDict))
                    instanceDict.Add(variant.instance, currentValue);
                else
                    newInstanceVarLastSyncValues.Add(variant.varId, new() { { variant.instance, currentValue } });



                //* 如果值没有变化就跳过
                if (!instanceVarLastSyncValues.TryGetValue(variant.varId, out var lastSyncDict) ||
                    !lastSyncDict.TryGetValue(variant.instance, out var lastSyncValue) ||
                    Equals(lastSyncValue, currentValue))
                    continue;



                ////Debug.Log(variant.varId);
                //调用值改变方法
                var currentValueBytes = Rpc.ObjectToBytes(currentValue);
                OnValueChange(variant.varId, entity, Rpc.ObjectToBytes(variant.valueLastSync), currentValueBytes);

                //更改列表中的值
                var newVar = new NMSyncVar(variant.varId, variant.instance, currentValueBytes, currentValueBytes, Tools.time);
                instanceVars[i] = newVar;

                //将新值发送给所有客户端
                Server.Send(newVar);
            }


            staticVarLastSyncValues = newStaticVarLastSyncValues;
            instanceVarLastSyncValues = newInstanceVarLastSyncValues;
        }















        //TODO;合并这两个委托
        public static Func<string, string> InstanceSetterId;
        public static Func<string, string> StaticSetterId;
        public static Func<string, object> GetStaticFieldValue;
        public static Func<string, Entity, object> GetInstanceFieldValue;
        public static Action StaticVarsRegister;
        //public static Action<Entity> InstanceVarsRegister;//TODO

        private static readonly StringBuilder stringBuilder = new();





        public static void _OnVarValueChangeError(string id)
        {
            Debug.LogError($"调用 SyncVar {id} 的值绑定失败!");
        }

        public static string _GetIDError(string id)
        {
            Debug.LogError($"获取 SyncVar {id} 失败!");
            return null;
        }

        public static object _GetVarError(string id)
        {
            Debug.LogError($"获取 SyncVar {id} 失败!");
            return null;
        }


        public static void Init()
        {
            initialized = false;
            sendInterval = defaultSendInterval;

            /* --------------------------------- 生成反射参数 --------------------------------- */
            BindingFlags flags = ReflectionTools.BindingFlags_All;

            /* --------------------------------- 获取定义的方法 --------------------------------- */
            var RegisterVar = typeof(SyncPacker).GetMethod(nameof(SyncPacker.RegisterVar), new Type[] { typeof(string), typeof(bool), typeof(byte[]) });
            var RpcBytesToObject = typeof(Rpc).GetMethod(nameof(Rpc.BytesToObject), 0, new Type[] { typeof(byte[]) });

            ParameterExpression firstTempValue_id = Expression.Parameter(typeof(string), "id");
            ParameterExpression firstTempValue_instance = Expression.Parameter(typeof(Entity), "instance");
            ParameterExpression firstTempValue_newValueBytes = Expression.Parameter(typeof(byte[]), "newValueBytes");

            ParameterExpression onValueChangeParam_id = Expression.Parameter(typeof(string), "id");
            ParameterExpression onValueChangeParam_instance = Expression.Parameter(typeof(Entity), "instance");
            ParameterExpression onValueChangeParam_oldValueBytes = Expression.Parameter(typeof(byte[]), "oldValueBytes");
            ParameterExpression onValueChangeParam_newValueBytes = Expression.Parameter(typeof(byte[]), "newValueBytes");

            ParameterExpression instanceSetterIdParam_setter = Expression.Parameter(typeof(string), "id");
            ParameterExpression staticSetterIdParam_setter = Expression.Parameter(typeof(string), "id");

            List<SwitchCase> firstTempValueCases = new();
            List<SwitchCase> onValueChangeCases = new();
            List<SwitchCase> instanceSetterIdCases = new();
            List<SwitchCase> staticSetterIdCases = new();

            List<SwitchCase> getStaticFieldValueCases = new();
            List<SwitchCase> getInstanceFieldValueCases = new();
            ParameterExpression getStaticFieldValueParam_id = Expression.Parameter(typeof(string), "id");
            ParameterExpression getInstanceFieldValueParam_id = Expression.Parameter(typeof(string), "id");
            ParameterExpression getInstanceFieldValueParam_entity = Expression.Parameter(typeof(Entity), "entity");

            Action staticVarsRegisterMethods = () => { };

            static Expression CallRpcBytesToObject(Type type, Expression bytes)
            {
                return Expression.Call(
                    null,
                    typeof(Rpc).GetMethod(nameof(Rpc.BytesToObject), 1, new Type[] { typeof(byte[]) }).MakeGenericMethod(type),
                    bytes);
            }

            /* -------------------------------------------------------------------------- */
            //?                               更改方法内容                                 */
            /* -------------------------------------------------------------------------- */
            //遍历每个程序集中的方法
            foreach (var ass in ModFactory.assemblies)
            {
                foreach (var type in ass.GetTypes())
                {
                    //排除无用的程序集, 加快加载
                    if (!ModFactory.IsUserType(type) || type.IsGenericType)
                        continue;

                    //获取所有可用方法
                    foreach (var field in type.GetFields())
                    {
                        if (AttributeGetter.TryGetAttribute<SyncAttribute>(field, out var att))
                        {
                            var fieldPath = $"{type.FullName}.{field.Name}";
                            var fieldPathConst = Expression.Constant(fieldPath);
                            var valueType = field.FieldType;
                            var valueTypeName = valueType.FullName;
                            var valueTypeArray = new Type[] { valueType };
                            var isStaticVar = field.IsStatic;
                            var fieldConst = Expression.Constant(field, typeof(FieldInfo));





                            #region 绑定值获取
                            if (isStaticVar)
                            {
                                getStaticFieldValueCases.Add(Expression.SwitchCase(Expression.MakeMemberAccess(null, field).Box(), fieldPathConst));
                            }
                            else
                            {
                                getInstanceFieldValueCases.Add(Expression.SwitchCase(Expression.MakeMemberAccess(Expression.Convert(getInstanceFieldValueParam_entity, field.DeclaringType), field).Box(), fieldPathConst));
                            }
                            #endregion






                            #region 绑定 值改变事件

                            //处理钩子方法（线程安全）
                            MethodInfo hookMethod = null;
                            if (!att.hook.IsNullOrWhiteSpace())
                            {
                                //获取钩子方法
                                hookMethod = !att.hook.Contains(".") ? type.GetMethodFromAllIncludingBases(att.hook) : ModFactory.SearchUserMethod(att.hook);

                                //检查是否找到钩子方法
                                if (hookMethod == null)
                                {
                                    Debug.LogError($"无法找到同步变量 {fieldPath} 的钩子: {att.hook}");
                                    continue;
                                }
                                if (hookMethod.GetParameters().Length != 1)
                                {
                                    Debug.LogError($"同步变量 {fieldPath} 的钩子 {att.hook} 的参数列表必须为: byte[]");
                                    continue;
                                }
                                if (hookMethod.GetParameters()[0].ParameterType != typeof(byte[]))
                                {
                                    Debug.LogError($"同步变量 {fieldPath} 的钩子 {att.hook} 的参数列表必须为: byte[]");
                                    continue;
                                }
                            }


                            //绑定初始值设置
                            firstTempValueCases.Add(
                                Expression.SwitchCase(
                                        Expression.Block(
                                                typeof(void),
                                                new Expression[] {
                                                    isStaticVar ?
                                                        //* @field = Rpc.BytesToObject<T>(newValueBytes);;
                                                        Expression.Assign(Expression.Field(null, field), CallRpcBytesToObject(valueType, firstTempValue_newValueBytes))
                                                                :
                                                        //* @field = Rpc.BytesToObject<T>(newValueBytes);
                                                        Expression.Assign(Expression.Field(Expression.Convert(firstTempValue_instance, field.DeclaringType), field), CallRpcBytesToObject(valueType, firstTempValue_newValueBytes))
                                                }
                                        ),
                                        fieldPathConst
                                    )
                            );


                            //绑定值改变事件
                            onValueChangeCases.Add(
                                Expression.SwitchCase(
                                    Expression.Block(
                                        typeof(void),
                                        new Expression[] {
                                            (isStaticVar, hookMethod == null) switch
                                            {
                                                (true, true) =>
                                                                //* @field = Rpc.BytesToObject<T>(newValueBytes);
                                                                Expression.Assign(Expression.Field(null, field), CallRpcBytesToObject(valueType, onValueChangeParam_newValueBytes)),

                                                (true, false) =>
                                                                //* @field = Rpc.BytesToObject<T>(newValueBytes);
                                                                //* hookMethod(oldValueBytes);
                                                                Expression.Block(
                                                                    Expression.Assign(Expression.Field(null, field), CallRpcBytesToObject(valueType, onValueChangeParam_newValueBytes)),
                                                                    Expression.Call(null, hookMethod, onValueChangeParam_oldValueBytes)
                                                                ),

                                                (false, true) =>
                                                                //* @field = Rpc.BytesToObject<T>(newValueBytes);
                                                                Expression.Assign(Expression.Field(Expression.Convert(onValueChangeParam_instance, field.DeclaringType), field), CallRpcBytesToObject(valueType, onValueChangeParam_newValueBytes)),

                                                (false, false) =>
                                                                //* @field = Rpc.BytesToObject<T>(newValueBytes);
                                                                //* hookMethod(oldValueBytes);
                                                                Expression.Block(
                                                                    Expression.Assign(Expression.Field(Expression.Convert(onValueChangeParam_instance, field.DeclaringType), field), CallRpcBytesToObject(valueType, onValueChangeParam_newValueBytes)),
                                                                    Expression.Call(Expression.Convert(onValueChangeParam_instance, field.DeclaringType), hookMethod, onValueChangeParam_oldValueBytes)
                                                                )
                                            }
                                    }),
                                    fieldPathConst)
                            );

                            #endregion






                            #region 默认值处理 & 绑定注册
                            if (!isStaticVar)
                            {
                                //? 实例变量的默认值处理在 Entity 中
                                // ................
                            }
                            else
                            {
                                //* 如果是 值形式 的默认值
                                if (AttributeGetter.TryGetAttribute<SyncDefaultValueAttribute>(field, out var defaultValueAttribute))
                                {
                                    //检查类型错误, 例如属性是 float 类型, 默认值却填写了 123 就会报错, 要写 123f
                                    if (defaultValueAttribute.defaultValue != null && valueTypeName != defaultValueAttribute.defaultValue.GetType().FullName)
                                    {
                                        Debug.LogError($"同步变量 {fieldPath} 错误: 返回值为 {valueTypeName} , 但默认值为 {defaultValueAttribute.defaultValue.GetType().FullName}");
                                        continue;
                                    }

                                    //把默认值转为 byte[] 然后保存
                                    var value = Rpc.ObjectToBytes(defaultValueAttribute.defaultValue);

                                    //绑定注册
                                    staticVarsRegisterMethods += () => SyncPacker.RegisterVar(fieldPath, uint.MaxValue, value);
                                }
                                //* 如果是 方法形式 的默认值
                                else if (AttributeGetter.TryGetAttribute<SyncDefaultValueFromMethodAttribute>(field, out var defaultValueFromMethodAttribute))
                                {
                                    //获取默认值方法
                                    MethodInfo defaultValueMethod = ModFactory.SearchUserMethod(defaultValueFromMethodAttribute.methodName);

                                    //检查方法是否为空
                                    if (defaultValueMethod == null)
                                    {
                                        Debug.LogError($"无法找到同步变量 {fieldPath} 的默认值获取方法 {defaultValueFromMethodAttribute.methodName}");
                                        continue;
                                    }

                                    //检查类型错误, 例如属性是 float 类型, 默认值方法却返回 int 就会报错
                                    if (valueTypeName != defaultValueMethod.ReturnType.FullName)
                                    {
                                        Debug.LogError($"同步变量 {fieldPath} 错误: 返回值为 {valueTypeName} , 但默认值为 {defaultValueMethod.ReturnType.FullName}");
                                        continue;
                                    }

                                    if (defaultValueFromMethodAttribute.getValueUntilRegister)
                                    {
                                        //在注册时获取默认值并转为 byte[]
                                        staticVarsRegisterMethods += () =>
                                        {
                                            SyncPacker.RegisterVar(fieldPath, uint.MaxValue, Rpc.ObjectToBytes(defaultValueMethod.Invoke(null, null)));
                                        };
                                    }
                                    else
                                    {
                                        //获取默认值并转为 byte[] 然后保存
                                        var value = Rpc.ObjectToBytes(defaultValueMethod.Invoke(null, null));

                                        //绑定注册
                                        staticVarsRegisterMethods += () => SyncPacker.RegisterVar(fieldPath, uint.MaxValue, value);
                                    }
                                }
                                //* 如果是无默认值
                                else
                                {
                                    staticVarsRegisterMethods += () => SyncPacker.RegisterVar(fieldPath, uint.MaxValue, null);
                                }
                            }

                            #endregion
                        }
                    }
                }
            }

            /* ----------------------------- 定义方法体 (Switch) ----------------------------- */
            var firstTempValueBody = Expression.Switch(firstTempValue_id, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_OnVarValueChangeError)), firstTempValue_id), firstTempValueCases.ToArray());
            var onValueChangeBody = Expression.Switch(onValueChangeParam_id, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_OnVarValueChangeError)), onValueChangeParam_id), onValueChangeCases.ToArray());
            var instanceSetIdBody = Expression.Switch(instanceSetterIdParam_setter, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_GetIDError)), instanceSetterIdParam_setter), instanceSetterIdCases.ToArray());
            var staticSetIdBody = Expression.Switch(staticSetterIdParam_setter, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_GetIDError)), staticSetterIdParam_setter), staticSetterIdCases.ToArray());
            var getStaticFieldValueBody = Expression.Switch(getStaticFieldValueParam_id, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_GetVarError)), getStaticFieldValueParam_id), getStaticFieldValueCases.ToArray());
            var getInstanceFieldValueBody = Expression.Switch(getInstanceFieldValueParam_id, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_GetVarError)), getInstanceFieldValueParam_id), getInstanceFieldValueCases.ToArray());

            /* ---------------------------------- 编译方法 ---------------------------------- */
            FirstTempValue = Expression.Lambda<FirstTempValueDelegate>(firstTempValueBody, $"{nameof(FirstTempValue)}_Lambda", new[] { firstTempValue_id, firstTempValue_instance, firstTempValue_newValueBytes }).Compile();
            OnValueChange = Expression.Lambda<OnValueChangeCallback>(onValueChangeBody, $"{nameof(OnValueChange)}_Lambda", new[] { onValueChangeParam_id, onValueChangeParam_instance, onValueChangeParam_oldValueBytes, onValueChangeParam_newValueBytes }).Compile();
            InstanceSetterId = Expression.Lambda<Func<string, string>>(instanceSetIdBody, instanceSetterIdParam_setter).Compile();
            StaticSetterId = Expression.Lambda<Func<string, string>>(staticSetIdBody, staticSetterIdParam_setter).Compile();
            GetStaticFieldValue = Expression.Lambda<Func<string, object>>(getStaticFieldValueBody, getStaticFieldValueParam_id).Compile();
            GetInstanceFieldValue = Expression.Lambda<Func<string, Entity, object>>(getInstanceFieldValueBody, getInstanceFieldValueParam_id, getInstanceFieldValueParam_entity).Compile();

            StaticVarsRegister = staticVarsRegisterMethods;





            NetworkCallbacks.OnTimeToServerCallback += async () =>
            {
                await UniTask.WaitUntil(() => Client.isClient);
                await UniTask.NextFrame();

                StaticVarsRegister();
            };

            initialized = true;
        }











        [RuntimeInitializeOnLoadMethod]
        private static void BindMethods()
        {
            #region 

            //当新客户端进入时使其注册静态的同步变量
            NetworkCallbacks.OnClientReady += conn =>
            {
                //如果是自己就不反复注册
                if (conn == Server.localConnection)
                    return;

                foreach (var variant in staticVars)
                {
                    //让指定客户端重新注册同步变量
                    conn.Send<NMRegisterSyncVar>(new(variant.varId, variant.instance, variant.value, Tools.time));
                }
            };

            #endregion

            NetworkCallbacks.OnDisconnectFromServer += () =>
            {
                ClearVars();
            };

            NetworkCallbacks.OnStopClient += () =>
            {
                ClearVars();
            };

            NetworkCallbacks.OnStopServer += () =>
            {
                ClearVars();
            };

            NetworkCallbacks.OnStartServer += () =>
            {
                StartAutoSync();
            };

            static void ClearVars()
            {
                staticVars.Clear();
                instanceVars.Clear();
                EntitiesIDTable.Clear();
                EntityCenter.entityGenerationBindings.Clear();
                Debug.Log($"已清空 SyncPacker 同步变量");
            }

            static async void OnClientGetNMSyncVar(NMSyncVar nm)
            {
                //如果自己是服务器就不要同步
                if (Server.isServer)
                    return;

                //实例变量
                if (nm.instance != uint.MaxValue)
                {
                    for (int i = 0; i < instanceVars.Count; i++)
                    {
                        var variant = instanceVars[i];

                        //找到对应的实例变量，要求保留时间最新的
                        if (variant.varId == nm.varId && variant.instance == nm.instance && nm.serverSendTime > variant.serverSendTime)
                        {
                            //等待实体出现
                            //TODO 注: 这里可能会导致不同步问题，请使用队列Queue解决，将操作挂起
                            while (EntityCenter.GetEntityByNetIdWithInvalidCheck(variant.instance) == null)
                                await UniTask.NextFrame();

                            OnValueChange(variant.varId, EntityCenter.GetEntityByNetIdWithInvalidCheck(variant.instance), variant.value, nm.value);
                            instanceVars[i] = new(nm.varId, nm.instance, nm.value, nm.serverSendTime);

                            ////Debug.Log($"同步了变量 {var.varId} 为 {var.varValue}");
                            return;
                        }
                    }
                }
                //静态变量
                else
                {
                    for (int i = 0; i < staticVars.Count; i++)
                    {
                        var variant = staticVars[i];

                        //找到对应的静态变量，要求保留时间最新的
                        if (variant.varId == nm.varId && nm.serverSendTime > variant.serverSendTime)
                        {
                            OnValueChange(variant.varId, null, variant.value, nm.value);
                            staticVars[i] = new(nm.varId, nm.instance, nm.value, nm.serverSendTime);

                            ////Debug.Log($"同步了变量 {var.varId} 为 {var.varValue}");
                            return;
                        }
                    }
                }

                ////我们不在这里报错, 因为客户端可能还没从服务器同步到所有变量, 所以会收到一些同步变量, 但这些变量还没注册到本地, 所以会报错
                ////Debug.LogError($"从服务器接受同步变量 {nm.varId} 的值失败");
            }

            static void OnServerGetNMRequestInstanceVars(NetworkConnectionToClient conn, NMRequestInstanceVars _)
            {
                //实例变量要等进入游戏场景才可以注册
                foreach (var variant in instanceVars)
                {
                    //让指定客户端重新注册同步变量
                    conn.Send<NMRegisterSyncVar>(new(variant.varId, variant.instance, variant.value, Tools.time));
                }
            }

            static void OnServerGetNMSyncVar(NetworkConnectionToClient conn, NMSyncVar nm)
            {
                //// to-dO: 安全检查: if (!conn.owned)
                //// //排除服务器自己
                //// if (conn == Server.localConnection)
                ////     return;
                //
                //// //实例变量
                //// if (nm.instance != uint.MaxValue)
                //// {
                ////     if (instanceVars.TryGetValue(nm.varId, out var inner) && inner.TryGetValue(nm.instance, out var var))
                ////     {
                ////         var oldValue = var.value;
                ////         var.value = nm.value;
                ////         inner[nm.instance] = var;
                ////         OnValueChange(var.varId, Entity.GetEntityByNetIdWithCheckInvalid(var.instance), oldValue, var.value);
                ////         return;
                ////     }
                //// }
                //// //静态变量
                //// else
                //// {
                ////     if (staticVars.TryGetValue(nm.varId, out var var))
                ////     {
                ////         var oldValue = var.value;
                ////         var.value = nm.value;
                ////         staticVars[nm.varId] = var;
                ////         OnValueChange(var.varId, Entity.GetEntityByNetIdWithCheckInvalid(var.instance), oldValue, var.value);
                ////         return;
                ////     }
                //// }
                //
                //// //广播给其他客户端
                //// Server.Send(nm);
            }

            static void OnClientGetNMRegisterSyncVar(NMRegisterSyncVar register)
            {
                //服务器会提前进行注册, 不需要重复注册
                if (Server.isServer)
                    return;

                LocalRegisterSyncVar(register);
            }

            static void OnClientGetNMUnregisterVar(NMUnregisterSyncVar unregister)
            {
                //实例变量
                if (unregister.instance != uint.MaxValue)
                {
                    for (int i = 0; i < instanceVars.Count; i++)
                    {
                        var variant = instanceVars[i];
                        if (variant.varId == unregister.varId && variant.instance == unregister.instance)
                        {
                            instanceVars.RemoveAt(i);
                            return;
                        }
                    }

                    Debug.LogError($"取消注册实例同步变量 {unregister.varId}, {unregister.instance} 失败");
                }
                //静态变量
                else
                {
                    for (int i = 0; i < staticVars.Count; i++)
                    {
                        if (staticVars[i].varId == unregister.varId)
                        {
                            staticVars.RemoveAt(i);
                            return;
                        }
                    }

                    Debug.LogError($"取消注册静态同步变量 {unregister.varId} 失败");
                }
            }

            NetworkCallbacks.OnTimeToServerCallback += () =>
            {
                Server.Callback<NMRequestInstanceVars>(OnServerGetNMRequestInstanceVars);
                Server.Callback<NMSyncVar>(OnServerGetNMSyncVar);
            };
            NetworkCallbacks.OnTimeToClientCallback += () =>
            {
                Client.Callback<NMRegisterSyncVar>(OnClientGetNMRegisterSyncVar);
                Client.Callback<NMUnregisterSyncVar>(OnClientGetNMUnregisterVar);
                Client.Callback<NMSyncVar>(OnClientGetNMSyncVar);
            };
        }

        private static void LocalRegisterSyncVar(NMRegisterSyncVar var)
        {
            //Debug.Log($"注册了{var.varId}");

            NMSyncVar variant = new(var.varId, var.instance, var.defaultValue, var.serverSendTime);

            //实例变量
            if (var.instance != uint.MaxValue)
            {
                instanceVars.Add(variant);
                //? 实例变量的缓存由 EntityInit 执行......
            }
            //静态变量
            else
            {
                staticVars.Add(variant);
                FirstTempValue(var.varId, null, var.defaultValue);
            }
            //Debug.Log($"注册了同步变量 {var.varId}");
        }
    }
}
