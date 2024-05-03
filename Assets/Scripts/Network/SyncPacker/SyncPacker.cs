using Cysharp.Threading.Tasks;
using GameCore.High;
using HarmonyLib;
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

namespace GameCore
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

        public static readonly Dictionary<string, NMSyncVar> staticVars = new();
        public static readonly Dictionary<string, Dictionary<uint, NMSyncVar>> instanceVars = new();

        /// <summary>
        /// 发送同步变量的间隔
        /// </summary>
        private static float _sendInterval;
        public static float sendInterval
        {
            get => _sendInterval;
            set
            {
                if (value <= 0)
                {
                    //按 defaultSendInterval 秒来同步
                    Debug.LogError($"{nameof(sendInterval)} 值不应小于或等于 0, 否则程序将停止响应, 已按照 {nameof(defaultSendInterval)}:{defaultSendInterval} 处理");
                    value = defaultSendInterval;
                }

                _sendInterval = value;
            }
        }

        public const float defaultSendInterval = 0.1f;
        public static bool initialized { get; private set; }



        #region 注册变量

        /// <summary>
        /// 注册同步变量, 只能在服务端调用
        /// </summary>
        /// <param name="varId"></param>
        public static void RegisterVar(string varId, uint instance, bool clientCanSet, byte[] defaultValue)
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

            NMRegisterSyncVar var = new(varId, instance, clientCanSet, defaultValue);

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

        public static async void StartAutoSync()
        {
            //按 sendInterval 秒的间隔来同步
            await sendInterval;

            if (!Server.isServer)
                return;

            Sync();

            StartAutoSync();
        }

        /// <summary>
        /// 立即同步所有变量
        /// </summary>
        public static void Sync()
        {
            if (!Server.isServer)
            {
                Debug.LogError("非服务器不可以同步变量");
                return;
            }



            List<string> staticSets = new();
            List<(string id, uint instance)> instanceSets = new();



            static bool ShouldNotBeSend(NMSyncVar var)
            {
                //如果同步变量的值发生未改变就不同步
                return var.valueLastSync == var.value;
            }



            //遍历静态变量
            foreach (var entry in staticVars)
            {
                if (ShouldNotBeSend(entry.Value))
                    continue;

                //将新值发送给所有客户端
                Server.Send(entry.Value);
                staticSets.Add(entry.Key);
            }

            //遍历实例变量
            foreach (var entry in instanceVars)
            {
                foreach (var item in entry.Value)
                {
                    if (ShouldNotBeSend(item.Value))
                        continue;

                    //将新值发送给所有客户端
                    Server.Send(item.Value);
                    instanceSets.Add((entry.Key, item.Key));
                }
            }



            //设置同步变量的旧值
            foreach (var set in staticSets)
            {
                NMSyncVar temp = staticVars[set];
                temp.valueLastSync = temp.value;
                staticVars[set] = temp;
            }

            foreach (var (id, instance) in instanceSets)
            {
                NMSyncVar temp = instanceVars[id][instance];
                temp.valueLastSync = temp.value;
                instanceVars[id][instance] = temp;
            }
        }

        //TODO: string varId -> long var;
        public static bool SetValue(string varId, Entity instance, byte[] value)
        {
            NMSyncVar var;

#if DEBUG
            if (string.IsNullOrEmpty(varId))
            {
                Debug.LogError("设置的同步变量 Id 为空");
                return false;
            }

            if (instance)
            {
                if (!instanceVars.TryGetValue(varId, out var table))
                {
                    Debug.LogWarning($"设置同步变量 {varId} 失败, 原因是没有找到, 也许还没有注册过这个变量或者是正在注册当中? 可以使用 {nameof(SyncPacker)}.{nameof(RegisterVar)}(string) 来注册同步变量\n注意! 也可能是您修改了同步变量的属性名称, 但没有修改其配套的读取方法和写入方法导致的!");
                    return false;
                }
                else if (!table.TryGetValue(instance.netId, out var))
                {
                    Debug.LogWarning($"设置同步变量 {varId} 失败, 原因是没有找到, 也许还没有注册过这个变量或者是正在注册当中? 可以使用 {nameof(SyncPacker)}.{nameof(RegisterVar)}(string) 来注册同步变量\n注意! 也可能是您修改了同步变量的属性名称, 但没有修改其配套的读取方法和写入方法导致的!");
                    return false;
                }
            }
            else
            {
                if (!staticVars.TryGetValue(varId, out var))
                {
                    Debug.LogWarning($"设置同步变量 {varId} 失败, 原因是没有找到, 也许还没有注册过这个变量或者是正在注册当中? 可以使用 {nameof(SyncPacker)}.{nameof(RegisterVar)}(string) 来注册同步变量\n注意! 也可能是您修改了同步变量的属性名称, 但没有修改其配套的读取方法和写入方法导致的!");
                    return false;
                }
            }
#else
            if (instance)
                var = instanceVars[varId][instance.netId];
            else
                var = staticVars[varId];
#endif

            //只有服务器拥有设置的权力
            if (!var.clientCanSet && !Server.isServer)
            {
                Debug.LogWarning($"客户端不能设置同步变量 {varId}, 原因是其 {nameof(NMSyncVar.clientCanSet)} 值为 false, 如果想在客户端设置, 请将其设置为 true");
                return false;
            }

            //设置值
            var oldValue = var.value;
            var.value = value;

            //服务器直接赋值
            if (Server.isServer)
            {
                if (instance)
                    instanceVars[varId][instance.netId] = var;
                else
                    staticVars[varId] = var;

                OnValueChange(var.varId, instance, oldValue, value);
            }
            //客户端发送给服务器赋值
            else
            {
                Client.Send(var);
            }

            return true;
        }










        static void StaticOnValueChangeBind<T>(byte[] newValueBytes, FieldInfo tempField)
        {
            T newValue = Rpc.BytesToObject<T>(newValueBytes);
            tempField.SetValue(null, newValue);
        }

        static void StaticOnValueChangeBindWithHook<T>(byte[] oldValueBytes, byte[] newValueBytes, FieldInfo tempField, MethodInfo hookMethod)
        {
            T newValue = Rpc.BytesToObject<T>(newValueBytes);
            tempField.SetValue(null, newValue);

            hookMethod.Invoke(null, new object[] { oldValueBytes });
        }


        static void InstanceOnValueChangeBind<T>(byte[] newValueBytes, FieldInfo tempField, Entity entity)
        {
            T newValue = Rpc.BytesToObject<T>(newValueBytes);
            tempField.SetValue(entity, newValue);
        }

        static void InstanceOnValueChangeBindWithHook<T>(byte[] oldValueBytes, byte[] newValueBytes, FieldInfo tempField, MethodInfo hookMethod, Entity entity)
        {
            T newValue = Rpc.BytesToObject<T>(newValueBytes);
            tempField.SetValue(entity, newValue);

            hookMethod.Invoke(entity, new object[] { oldValueBytes });
        }











        //TODO;合并这两个委托
        public static Func<string, string> InstanceSetterId;
        public static Func<string, string> StaticSetterId;

        public static Action StaticVarsRegister;
        //public static Action<Entity> InstanceVarsRegister;//TODO

        private static readonly StringBuilder stringBuilder = new();


        public static bool _InstanceSet(Entity __instance, object value, MethodBase __originalMethod)
        {
            stringBuilder.Clear();
            stringBuilder.Append(__originalMethod.DeclaringType.FullName).Append('.').Append(__originalMethod.Name);

#if DEBUG
            try
            {
                var path = stringBuilder.ToString();
                if (__instance == null)
                    Debug.LogError($"调用实例同步变量赋值器 {path} 时出错! 实例为空");
                var varInstanceId = __instance.varInstanceId;
                var bytes = Rpc.ObjectToBytes(value);
                SetValue(InstanceSetterId(path), __instance, bytes);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
#else
            SetValue(InstanceSetterId(stringBuilder.ToString()), __instance, Rpc.ObjectToBytes(value));
#endif

            return false;
        }


        public static bool _StaticSet(object value, MethodBase __originalMethod)
        {
            stringBuilder.Clear();
            stringBuilder.Append(__originalMethod.DeclaringType.FullName).Append('.').Append(__originalMethod.Name);

            //TODO: 直接编译 StaticSetValue, 一步到位, 而不再是 StaticSetterId
            SetValue(StaticSetterId(stringBuilder.ToString()), null, Rpc.ObjectToBytes(value));

            return false;
        }



        public static void _OnVarValueChangeError(string id)
        {
            Debug.LogError($"调用 SyncVar {id} 的值绑定失败!");
        }

        public static string _GetIDError(string id)
        {
            Debug.LogError($"获取 SyncVar {id} 失败!");
            return null;
        }


        public static async void Init()
        {
            initialized = false;
            sendInterval = defaultSendInterval;

            /* --------------------------------- 生成反射参数 --------------------------------- */
            BindingFlags flags = ReflectionTools.BindingFlags_All;

            /* --------------------------------- 获取定义的方法 --------------------------------- */
            var _InstanceSet = typeof(SyncPacker).GetMethod($"{nameof(SyncPacker._InstanceSet)}");
            var _StaticSet = typeof(SyncPacker).GetMethod($"{nameof(SyncPacker._StaticSet)}");
            var RegisterVar = typeof(SyncPacker).GetMethod(nameof(SyncPacker.RegisterVar), new Type[] { typeof(string), typeof(bool), typeof(byte[]) });
            var RpcBytesToObject = typeof(Rpc).GetMethod(nameof(Rpc.BytesToObject), 0, new Type[] { typeof(byte[]) });
            var _StaticOnValueChangeBind = typeof(SyncPacker).GetMethodFromAll(nameof(StaticOnValueChangeBind));
            var _StaticOnValueChangeBindWithHook = typeof(SyncPacker).GetMethodFromAll(nameof(StaticOnValueChangeBindWithHook));
            var _InstanceOnValueChangeBind = typeof(SyncPacker).GetMethodFromAll(nameof(InstanceOnValueChangeBind));
            var _InstanceOnValueChangeBindWithHook = typeof(SyncPacker).GetMethodFromAll(nameof(InstanceOnValueChangeBindWithHook));

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

            Action staticVarsRegisterMethods = () => { };

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

                    //TODO: 加油，没那么难
                    //TODO: 要做的是加载完后保存好所有同步变量的类型，
                    //TODO: 现在oldValue改为object,而非byte[], SetValue(以后接受参数object而非byte[])
                    //TODO: 在注册同步变量后就开始在AutoRegister中记录同步变量的值（检测object而非byte[]），然后检测有无变化
                    //获取所有可用方法
                    foreach (var property in type.GetAllProperties())
                    {
                        if (AttributeGetter.TryGetAttribute<SyncAttribute>(property, out var att))
                        {
                            var propertyPath = $"{type.FullName}.{property.Name}";
                            var propertyPathConst = Expression.Constant(propertyPath);
                            var valueType = property.PropertyType;
                            var valueTypeName = valueType.FullName;
                            var valueTypeArray = new Type[] { valueType };
                            var tempFieldName = $"_{property.Name}";
                            var tempField = type.GetFieldFromAllIncludingBases(tempFieldName);
                            var tempFieldConst = Expression.Constant(tempField, typeof(FieldInfo));
                            var setterMethodPath = $"_{propertyPath}_set";
                            var setterMethod = type.GetMethodFromAllIncludingBases($"_{property.Name}_set");
                            var isStaticVar = property.GetSetMethod().IsStatic;
                            Harmony harmony = new($"SyncPacker.harmony.{propertyPath}");





                            #region 检查
#if DEBUG

                            if (propertyPath.Contains("_set"))
                            {
                                Debug.LogError($"同步变量 {propertyPath} 的名称包括 \"_set\", 这不被允许, 请重新创建");
                                continue;
                            }


                            if (tempField == null)
                            {
                                Debug.LogError($"同步变量 {propertyPath} 不包含字段 {tempFieldName}, 请重新创建");
                                continue;
                            }
                            if (tempField.IsStatic != isStaticVar)
                            {
                                Debug.LogError($"同步变量的缓存字段 {tempFieldName} 与其对应属性的静态/实例不符, 请重新创建");
                                continue;
                            }
                            if (tempField.FieldType != valueType)
                            {
                                Debug.LogError($"同步变量的缓存字段 {tempFieldName} 的类型与其对应属性不符合, 请重新创建, 如 float health 的缓存字段应为 float health_temp;");
                                continue;
                            }



                            if (setterMethod == null)
                            {
                                Debug.LogError($"同步变量 {propertyPath} 不包含方法 {setterMethodPath}, 请重新创建");
                                continue;
                            }
                            if (setterMethod.IsStatic != isStaticVar)
                            {
                                Debug.LogError($"同步变量设置器 {tempFieldName} 与其对应属性的静态/实例不符, 请重新创建");
                                continue;
                            }
                            if (setterMethod.GetParameters().Length != 1)
                            {
                                Debug.LogError($"同步变量设置器 {setterMethodPath} 必须有且仅有一个参数, 请重新创建, 如 float health 的同步变量设置器应为 void health_set(float value) " + "{ }");
                                continue;
                            }
                            if (setterMethod.GetParameters()[0].ParameterType != valueType)
                            {
                                Debug.LogError($"同步变量设置器 {setterMethodPath} 的参数类型与其对应属性不符合, 请重新创建, 如 float health 的同步变量设置器应为 void health_set(float value) " + "{ }");
                                continue;
                            }

#endif
                            #endregion







                            #region 修改 _set 方法（这个地方 Harmony 的耗时很高，所以我们用了多线程）


                            /* ---------------------------------- 编辑方法 ---------------------------------- */

                            if (!setterMethod.IsStatic)
                            {
                                //* 通过 setterMethod 的名字获取变量名
                                lock (instanceSetterIdCases)
                                    instanceSetterIdCases.Add(Expression.SwitchCase(
                                        propertyPathConst,
                                        Expression.Constant(setterMethodPath)
                                    ));

                                //修改方法
                                harmony.Patch(setterMethod, new HarmonyMethod(_InstanceSet));
                            }
                            else
                            {
                                lock (staticSetterIdCases)
                                    //* 通过 setterMethod 的名字获取变量名
                                    staticSetterIdCases.Add(Expression.SwitchCase(
                                        propertyPathConst,
                                        Expression.Constant(setterMethodPath)
                                    ));

                                //修改方法
                                harmony.Patch(setterMethod, new HarmonyMethod(_StaticSet));
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
                                    Debug.LogError($"无法找到同步变量 {propertyPath} 的钩子: {att.hook}");
                                    continue;
                                }
                                if (hookMethod.GetParameters().Length != 1)
                                {
                                    Debug.LogError($"同步变量 {propertyPath} 的钩子 {att.hook} 的参数列表必须为: byte[]");
                                    continue;
                                }
                                if (hookMethod.GetParameters()[0].ParameterType != typeof(byte[]))
                                {
                                    Debug.LogError($"同步变量 {propertyPath} 的钩子 {att.hook} 的参数列表必须为: byte[]");
                                    continue;
                                }
                            }



                            //绑定初始值设置
                            lock (firstTempValueCases)
                                firstTempValueCases.Add(
                                    Expression.SwitchCase(
                                            isStaticVar ?
                                                Expression.Call(
                                                        null,
                                                        _StaticOnValueChangeBind.MakeGenericMethod(valueTypeArray),
                                                        firstTempValue_newValueBytes,
                                                        tempFieldConst
                                                    ) :
                                                Expression.Call(
                                                        null,
                                                        _InstanceOnValueChangeBind.MakeGenericMethod(valueTypeArray),
                                                        firstTempValue_newValueBytes,
                                                        tempFieldConst,
                                                        firstTempValue_instance
                                                    ),
                                            propertyPathConst
                                        )
                                );


                            //绑定值改变事件
                            lock (onValueChangeCases)
                                onValueChangeCases.Add((isStaticVar, hookMethod == null) switch
                                {
                                    (true, true) => Expression.SwitchCase(
                                                    Expression.Call(
                                                            null,
                                                            _StaticOnValueChangeBind.MakeGenericMethod(valueTypeArray),
                                                            onValueChangeParam_newValueBytes,
                                                            tempFieldConst),
                                                    propertyPathConst),
                                    (true, false) => Expression.SwitchCase(
                                                    Expression.Call(
                                                            null,
                                                            _StaticOnValueChangeBindWithHook.MakeGenericMethod(valueTypeArray),
                                                            onValueChangeParam_oldValueBytes,
                                                            onValueChangeParam_newValueBytes,
                                                            tempFieldConst,
                                                            Expression.Constant(hookMethod)),
                                                    propertyPathConst),
                                    (false, true) => Expression.SwitchCase(
                                                    Expression.Call(
                                                            null,
                                                            _InstanceOnValueChangeBind.MakeGenericMethod(valueTypeArray),
                                                            onValueChangeParam_newValueBytes,
                                                            tempFieldConst,
                                                            onValueChangeParam_instance),
                                                    propertyPathConst),
                                    (false, false) => Expression.SwitchCase(
                                                    Expression.Call(
                                                            null,
                                                            _InstanceOnValueChangeBindWithHook.MakeGenericMethod(valueTypeArray),
                                                            onValueChangeParam_oldValueBytes,
                                                            onValueChangeParam_newValueBytes,
                                                            tempFieldConst,
                                                            Expression.Constant(hookMethod),
                                                            onValueChangeParam_instance),
                                                    propertyPathConst)
                                });

                            #endregion






                            #region 默认值处理 & 绑定注册
                            if (!property.GetMethod.IsStatic)
                            {
                                //? 实例变量的默认值处理在 Entity 中
                                // ................
                            }
                            else
                            {
                                //* 如果是 值形式 的默认值
                                if (AttributeGetter.TryGetAttribute<SyncDefaultValueAttribute>(property, out var defaultValueAttribute))
                                {
                                    //检查类型错误, 例如属性是 float 类型, 默认值却填写了 123 就会报错, 要写 123f
                                    if (defaultValueAttribute.defaultValue != null && valueTypeName != defaultValueAttribute.defaultValue.GetType().FullName)
                                    {
                                        Debug.LogError($"同步变量 {propertyPath} 错误: 返回值为 {valueTypeName} , 但默认值为 {defaultValueAttribute.defaultValue.GetType().FullName}");
                                        continue;
                                    }

                                    //把默认值转为 byte[] 然后保存
                                    var value = Rpc.ObjectToBytes(defaultValueAttribute.defaultValue);

                                    //绑定注册
                                    staticVarsRegisterMethods += () => SyncPacker.RegisterVar(propertyPath, uint.MaxValue, true, value);
                                }
                                //* 如果是 方法形式 的默认值
                                else if (AttributeGetter.TryGetAttribute<SyncDefaultValueFromMethodAttribute>(property, out var defaultValueFromMethodAttribute))
                                {
                                    //获取默认值方法
                                    MethodInfo defaultValueMethod = ModFactory.SearchUserMethod(defaultValueFromMethodAttribute.methodName);

                                    //检查方法是否为空
                                    if (defaultValueMethod == null)
                                    {
                                        Debug.LogError($"无法找到同步变量 {propertyPath} 的默认值获取方法 {defaultValueFromMethodAttribute.methodName}");
                                        continue;
                                    }

                                    //检查类型错误, 例如属性是 float 类型, 默认值方法却返回 int 就会报错
                                    if (valueTypeName != defaultValueMethod.ReturnType.FullName)
                                    {
                                        Debug.LogError($"同步变量 {propertyPath} 错误: 返回值为 {valueTypeName} , 但默认值为 {defaultValueMethod.ReturnType.FullName}");
                                        continue;
                                    }

                                    if (defaultValueFromMethodAttribute.getValueUntilRegister)
                                    {
                                        //在注册时获取默认值并转为 byte[]
                                        staticVarsRegisterMethods += () =>
                                        {
                                            SyncPacker.RegisterVar(propertyPath, uint.MaxValue, true, Rpc.ObjectToBytes(defaultValueMethod.Invoke(null, null)));
                                        };
                                    }
                                    else
                                    {
                                        //获取默认值并转为 byte[] 然后保存
                                        var value = Rpc.ObjectToBytes(defaultValueMethod.Invoke(null, null));

                                        //绑定注册
                                        staticVarsRegisterMethods += () => SyncPacker.RegisterVar(propertyPath, uint.MaxValue, true, value);
                                    }
                                }
                                //* 如果是无默认值
                                else
                                {
                                    staticVarsRegisterMethods += () => SyncPacker.RegisterVar(propertyPath, uint.MaxValue, true, null);
                                }
                            }

                            #endregion
                        }
                    }


                    //要等一帧以防止游戏卡死
                    await UniTask.NextFrame();
                }
            }

            /* ----------------------------- 定义方法体 (Switch) ----------------------------- */
            var firstTempValueBody = Expression.Switch(firstTempValue_id, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_OnVarValueChangeError)), firstTempValue_id), firstTempValueCases.ToArray());
            var onValueChangeBody = Expression.Switch(onValueChangeParam_id, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_OnVarValueChangeError)), onValueChangeParam_id), onValueChangeCases.ToArray());
            var instanceSetIdBody = Expression.Switch(instanceSetterIdParam_setter, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_GetIDError)), instanceSetterIdParam_setter), instanceSetterIdCases.ToArray());
            var staticSetIdBody = Expression.Switch(staticSetterIdParam_setter, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_GetIDError)), staticSetterIdParam_setter), staticSetterIdCases.ToArray());

            /* ---------------------------------- 编译方法 ---------------------------------- */
            FirstTempValue = Expression.Lambda<FirstTempValueDelegate>(firstTempValueBody, $"{nameof(SyncPacker.FirstTempValue)}_Lambda", new[] { firstTempValue_id, firstTempValue_instance, firstTempValue_newValueBytes }).Compile();
            OnValueChange = Expression.Lambda<OnValueChangeCallback>(onValueChangeBody, $"{nameof(SyncPacker.OnValueChange)}_Lambda", new[] { onValueChangeParam_id, onValueChangeParam_instance, onValueChangeParam_oldValueBytes, onValueChangeParam_newValueBytes }).Compile();
            InstanceSetterId = Expression.Lambda<Func<string, string>>(instanceSetIdBody, instanceSetterIdParam_setter).Compile();
            StaticSetterId = Expression.Lambda<Func<string, string>>(staticSetIdBody, staticSetterIdParam_setter).Compile();

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

                foreach (KeyValuePair<string, NMSyncVar> entry in staticVars)
                {
                    var var = entry.Value;

                    //让指定客户端重新注册同步变量
                    conn.Send<NMRegisterSyncVar>(new(var.varId, var.instance, var.clientCanSet, var.value));
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
                    if (instanceVars.TryGetValue(nm.varId, out var inner) && inner.TryGetValue(nm.instance, out var var))
                    {
                        //等待实体出现
                        //TODO 注: 这里可能会导致不同步问题，请使用先入先出队列解决，将操作挂起
                        while (Entity.GetEntityByNetIdWithCheckInvalid(var.instance) == null)
                            await UniTask.NextFrame();

                        var oldValue = var.value;
                        var.value = nm.value;
                        inner[nm.instance] = var;
                        OnValueChange(var.varId, Entity.GetEntityByNetIdWithCheckInvalid(var.instance), oldValue, var.value);

                        ////Debug.Log($"同步了变量 {var.varId} 为 {var.varValue}");
                        return;
                    }
                }
                //静态变量
                else
                {
                    if (staticVars.TryGetValue(nm.varId, out var var))
                    {
                        var oldValue = var.value;
                        var.value = nm.value;
                        staticVars[nm.varId] = var;
                        OnValueChange(var.varId, null, oldValue, var.value);

                        ////Debug.Log($"同步了变量 {var.varId} 为 {var.varValue}");
                        return;
                    }
                }

                ////Debug.LogError($"同步变量 {var.varId} 的值为 {var.varValue} 失败");
            }

            static void OnServerGetNMRequestInstanceVars(NetworkConnectionToClient conn, NMRequestInstanceVars _)
            {
                //实例变量要等进入游戏场景才可以注册
                foreach (KeyValuePair<string, Dictionary<uint, NMSyncVar>> entry in instanceVars)
                {
                    foreach (var instanceEntry in entry.Value)
                    {
                        var var = instanceEntry.Value;

                        //让指定客户端重新注册同步变量
                        conn.Send<NMRegisterSyncVar>(new(var.varId, var.instance, var.clientCanSet, var.value));
                    }
                }
            }

            static void OnServerGetNMSyncVar(NetworkConnectionToClient conn, NMSyncVar nm)
            {
                //TODO: 安全检查: if (!nm.clientCanSet&&conn.owned)
                //排除服务器自己
                if (conn == Server.localConnection)
                    return;

                //实例变量
                if (nm.instance != uint.MaxValue)
                {
                    if (instanceVars.TryGetValue(nm.varId, out var inner) && inner.TryGetValue(nm.instance, out var var))
                    {
                        var oldValue = var.value;
                        var.value = nm.value;
                        inner[nm.instance] = var;
                        OnValueChange(var.varId, Entity.GetEntityByNetIdWithCheckInvalid(var.instance), oldValue, var.value);
                        return;
                    }
                }
                //静态变量
                else
                {
                    if (staticVars.TryGetValue(nm.varId, out var var))
                    {
                        var oldValue = var.value;
                        var.value = nm.value;
                        staticVars[nm.varId] = var;
                        OnValueChange(var.varId, Entity.GetEntityByNetIdWithCheckInvalid(var.instance), oldValue, var.value);
                        return;
                    }
                }

                //广播给其他客户端
                Server.Send(nm);
            }

            static void OnClientGetNMRegisterSyncVar(NMRegisterSyncVar nm)
            {
                //服务器会提前进行注册, 不需要重复注册
                if (Server.isServer)
                    return;

                LocalRegisterSyncVar(nm);
            }

            static void OnClientGetNMUnregisterVar(NMUnregisterSyncVar var)
            {
                //实例变量
                if (var.instance != uint.MaxValue)
                {
                    if (!instanceVars.TryGetValue(var.varId, out var instanceVar) || !instanceVar.Remove(var.instance))
                    {
                        Debug.LogError($"取消注册同步变量 {var.varId}, {var.instance} 失败");
                    }
                }
                //静态变量
                else
                {
                    if (!staticVars.Remove(var.varId))
                    {
                        Debug.LogError($"取消注册同步变量 {var.varId} 失败");
                    }
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

            //实例变量
            if (var.instance != uint.MaxValue)
            {
                Dictionary<uint, NMSyncVar> table;

                if (!instanceVars.ContainsKey(var.varId))
                {
                    table = new();
                    instanceVars.Add(var.varId, table);
                }
                else
                {
                    table = instanceVars[var.varId];
                }

                if (table.TryAdd(var.instance, new NMSyncVar(var.varId, var.instance, var.defaultValue, var.clientCanSet)))
                {
                    ////FirstTempValue(var.varId, Entity.GetEntityByNetIdWithCheck(var.instance), var.defaultValue);
                    //? 实例变量的缓存由 EntityInit 执行
                }
                else
                {
                    Debug.LogError($"注册 {var.varId}, {var.instance} 失败, 其已存在");
                }
            }
            //静态变量
            else
            {
                if (staticVars.TryAdd(var.varId, new(var.varId, var.instance, var.defaultValue, var.clientCanSet)))
                {
                    FirstTempValue(var.varId, null, var.defaultValue);
                }
                else
                {
                    Debug.LogError($"注册 {var.varId} 失败, 其已存在");
                }
            }
            //Debug.Log($"注册了同步变量 {var.varId}");
        }
    }
}
