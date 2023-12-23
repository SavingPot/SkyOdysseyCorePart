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

namespace GameCore
{
    /// <summary>
    /// Sync Var Packer
    /// </summary>
    //!     这个类的性能对游戏性能影响巨大!!!!!!!!!!!!!!!!! 一定要好好优化
    //TODO: 这个类的性能对游戏性能影响巨大!!!!!!!!!!!!!!!!! 一定要好好优化
    public static class SyncPacker
    {
        public delegate void OnVarValueChangeCallback(NMSyncVar nm, byte[] oldValue);

        public static OnVarValueChangeCallback OnVarValueChange = (_, _) => { };
        public static Action<NMRegisterSyncVar> OnRegisterVar = a => { };

        public static readonly Dictionary<string, NMSyncVar> vars = new();

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



        #region 注册变量
        /// <summary>
        /// 注册同步变量, 只能在服务端调用
        /// </summary>
        /// <param name="varId"></param>
        public static void RegisterVar(string varId, bool clientCanSet, byte[] defaultValue)
        => RegisterVar(new(varId, clientCanSet, defaultValue));

        /// <summary>
        /// 注册同步变量, 只能在服务端调用
        /// </summary>
        /// <param name="varId"></param>
        public static void RegisterVar(NMRegisterSyncVar var)
        {
            if (var.varId.IsNullOrWhiteSpace())
            {
                Debug.LogError("不可以注册空变量");
                return;
            }

            if (!Server.isServer)
            {
                Debug.LogError("非服务器不可以注册变量");
                return;
            }

            //保证服务器立马注册
            LocalRegisterSyncVar(var);

            Server.Send(var);
        }

        /// <summary>
        /// 取消注册同步变量, 只能在服务端调用
        /// </summary>
        /// <param name="varId"></param>
        public static void UnregisterVar(string varId) => UnregisterVar(new NMUnregisterSyncVar(varId));

        /// <summary>
        /// 取消注册同步变量, 只能在服务端调用
        /// </summary>
        /// <param name="var"></param>
        public static void UnregisterVar(NMUnregisterSyncVar var)
        {
            if (!Server.isServer)
            {
                Debug.LogWarning("非服务器无法注销同步变量");
                return;
            }

            Server.Send(var);
        }

        /// <summary>
        /// 取消注册同步变量, 只能在服务端调用
        /// </summary>
        /// <param name="var"></param>
        public static void UnregisterVarCore(NMUnregisterSyncVar var)
        {
            if (!vars.Remove(var.varId))
            {
                Debug.LogError($"取消注册同步变量 {var.varId} 失败");
            }
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

            List<string> sets = new();

            foreach (KeyValuePair<string, NMSyncVar> entry in vars)
            {
                //如果同步变量的值发生未改变就不同步
                if (Equals(entry.Value.valueLastSync, entry.Value.value))
                    continue;

                //将新值发送给所有客户端
                Server.Send(entry.Value);

                sets.Add(entry.Key);
            }

            foreach (var set in sets)
            {
                NMSyncVar temp = vars[set];
                temp.valueLastSync = temp.value;
                vars[set] = temp;
            }
        }

        public static NMSyncVar GetVar(string varId)
        {
#if DEBUG
            if (string.IsNullOrEmpty(varId))
            {
                Debug.LogWarning($"寻找的同步变量 ID 为空");
                return default;
            }
#endif

            if (vars.TryGetValue(varId, out var value))
            {
                return value;
            }

            Debug.LogWarning($"未找到同步变量 {varId}");
            return default;
        }

        public static bool HasVar(string varId)
        {
#if DEBUG
            if (string.IsNullOrEmpty(varId))
                return false;
#endif

            return vars.ContainsKey(varId);
        }

        //TODO: string varId -> long var;
        public static bool SetValue(string varId, byte[] value)
        {
#if DEBUG
            if (string.IsNullOrEmpty(varId))
            {
                Debug.LogError("设置的同步变量 Id 为空");
                return false;
            }
#endif

            if (vars.TryGetValue(varId, out var var))
            {
                //只有服务器拥有设置的权力
                if (!var.clientCanSet && !Server.isServer)
                {
                    Debug.LogWarning($"客户端不能设置同步变量 {varId}, 原因是其 {nameof(NMSyncVar.clientCanSet)} 值为 false, 如果想在客户端设置请将其设置为 true");
                    return false;
                }

                //设置值
                var oldValue = var.value;
                var.value = value;

                //服务器直接赋值
                if (Server.isServer)
                {
                    vars[varId] = var;
                    OnVarValueChange.Invoke(var, oldValue);
                }
                //客户端发送给服务器赋值
                else
                {
                    Client.Send(var);
                }

                return true;
            }

            Debug.LogWarning($"设置同步变量 {varId} 失败, 原因是没有找到, 也许还没有注册过这个变量或者是正在注册当中? 可以使用 {nameof(SyncPacker)}.{nameof(RegisterVar)}(string) 来注册同步变量\n注意! 也可能是您修改了同步变量的属性名称, 但没有修改其配套的读取方法和写入方法导致的!");

            return false;
        }







        public static Func<string, uint, string> InstanceGetId;
        public static Func<string, uint, string> InstanceSetId;

        public static Func<string, string> StaticGetId;
        public static Func<string, string> StaticSetId;

        public static Action StaticVarsRegister;
        public static Action<IVarInstanceID> InstanceVarsRegister;

        public static bool _InstanceSet(IVarInstanceID __instance, object value, MethodBase __originalMethod)
        {
#if DEBUG
            try
            {
                var path = $"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}";
                if (__instance == null)
                    Debug.LogError($"调用实例同步变量赋值器 {path} 时出错! 实例为空");
                var varInstanceId = __instance.varInstanceId;
                var bytes = Rpc.ObjectToBytes(value);
                SetValue(InstanceSetId(path, varInstanceId), bytes);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
#else
            SetValue(InstanceSetId($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", __instance.varInstanceId), Rpc.ObjectToBytes(value));
#endif
            return false;
        }

        public static bool _InstanceGet(IVarInstanceID __instance, ref object __result, MethodBase __originalMethod)
        {
#if DEBUG
            try
            {
                var path = $"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}";
                if (__instance == null)
                    Debug.LogError($"调用实例同步变量赋值器 {path} 时出错! 实例为空");
                var varInstanceId = __instance.varInstanceId;
                __result = Rpc.BytesToObject(GetVar(InstanceGetId(path, varInstanceId)).value);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
#else
            __result = Rpc.BytesToObject(GetVar(InstanceGetId($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", __instance.varInstanceId)).value);
#endif
            return false;
        }


        public static bool _StaticSet(object value, MethodBase __originalMethod)
        {
            SetValue(StaticSetId($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}"), Rpc.ObjectToBytes(value));
            return false;
        }

        public static bool _StaticGet(ref object __result, MethodBase __originalMethod)
        {
            __result = Rpc.BytesToObject(GetVar(StaticGetId($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}")).value);
            return false;
        }



        public static string _GetIDError(string id)
        {
            Debug.LogError($"获取 SyncVar {id} 失败!");
            return null;
        }

        public static string _GetReturnTypeError(string id)
        {
            Debug.LogError($"获取 返回值 {id} 失败!");
            return null;
        }

        public static void Init()
        {
            sendInterval = defaultSendInterval;

            /* --------------------------------- 生成反射参数 --------------------------------- */
            BindingFlags flags = ReflectionTools.BindingFlags_All;

            /* --------------------------------- 获取定义的方法 --------------------------------- */
            var _InstanceSet = typeof(SyncPacker).GetMethod($"{nameof(SyncPacker._InstanceSet)}");
            var _InstanceGet = typeof(SyncPacker).GetMethod($"{nameof(SyncPacker._InstanceGet)}");

            var _StaticSet = typeof(SyncPacker).GetMethod($"{nameof(SyncPacker._StaticSet)}");
            var _StaticGet = typeof(SyncPacker).GetMethod($"{nameof(SyncPacker._StaticGet)}");

            var GetInstanceID = typeof(SyncPacker).GetMethod("GetInstanceID", new Type[] { typeof(string), typeof(uint) });
            var RegisterVar = typeof(SyncPacker).GetMethod("RegisterVar", new Type[] { typeof(string), typeof(bool), typeof(byte[]) });

            ParameterExpression instanceGetIdParam_getter = Expression.Parameter(typeof(string), "id");
            ParameterExpression instanceGetIdParam_instance = Expression.Parameter(typeof(uint), "instanceId");
            ParameterExpression instanceSetIdParam_setter = Expression.Parameter(typeof(string), "id");
            ParameterExpression instanceSetIdParam_instance = Expression.Parameter(typeof(uint), "instanceId");

            ParameterExpression staticGetIdParam_getter = Expression.Parameter(typeof(string), "id");
            ParameterExpression staticSetIdParam_setter = Expression.Parameter(typeof(string), "id");
            ParameterExpression returnTypeParam_path = Expression.Parameter(typeof(string), "path");

            List<SwitchCase> instanceGetIdCases = new();
            List<SwitchCase> instanceSetIdCases = new();

            List<SwitchCase> staticGetIdCases = new();
            List<SwitchCase> staticSetIdCases = new();

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
                    if (type.Namespace == "System" || type.Namespace == "UnityEngine" || type.Namespace == "Mirror" || type.IsGenericType)
                        continue;

                    //获取所有可用方法
                    foreach (var method in type.GetAllMethods())
                    {
                        if (AttributeGetter.TryGetAttribute<SyncGetterAttribute>(method, out var GetterAttr))
                        {
                            string methodPath = $"{type.FullName}.{method.Name}";

                            if (method.ReturnType.FullName == typeof(void).FullName)
                            {
                                Debug.LogError($"同步变量获取器 {methodPath} 返回值必须不为空, 如 byte 的同步变量设置器应为 [SyncGetterAttribute] byte byte_get() => default;");
                                continue;
                            }


                            /* ---------------------------------- 编辑方法 ---------------------------------- */
                            Harmony harmony = new($"SyncPacker.harmony.getter.{methodPath}");
                            var splitted = methodPath.Split("_get");

                            if (splitted.Length != 2)
                            {
                                Debug.LogError($"同步变量获取器 {methodPath} 的格式必须为 *同步变量名_get*");
                                continue;
                            }

                            if (!method.IsStatic)
                            {
                                instanceGetIdCases.Add(Expression.SwitchCase(
                                    Expression.Call(null, GetInstanceID, Expression.Constant(splitted[0]), instanceGetIdParam_instance),
                                    Expression.Constant(methodPath)
                                ));
                                harmony.Patch(method, new HarmonyMethod(_InstanceGet));
                            }
                            else
                            {
                                staticGetIdCases.Add(Expression.SwitchCase(
                                    Expression.Constant(splitted[0]),
                                    Expression.Constant(methodPath)
                                ));
                                harmony.Patch(method, new HarmonyMethod(_StaticGet));
                            }
                        }
                        else if (AttributeGetter.TryGetAttribute<SyncSetterAttribute>(method, out var SetterAttr))
                        {
                            string methodPath = $"{type.FullName}.{method.Name}";

                            if (method.GetParameters().Length == 0)
                            {
                                Debug.LogError($"同步变量设置器 {methodPath} 必须包含一个参数, 如 byte 的同步变量设置器应为 [SyncSetterAttribute] void byte_set(byte value) " + "{ }");
                                continue;
                            }


                            /* ---------------------------------- 编辑方法 ---------------------------------- */
                            Harmony harmony = new($"SyncPacker.harmony.getter.{methodPath}");
                            var splitted = methodPath.Split("_set");

                            if (splitted.Length != 2)
                            {
                                Debug.LogError($"同步变量设置器 {methodPath} 的格式必须为 *同步变量名_set*");
                                continue;
                            }

                            if (!method.IsStatic)
                            {
                                instanceSetIdCases.Add(Expression.SwitchCase(
                                    Expression.Call(null, GetInstanceID, Expression.Constant(splitted[0]), instanceSetIdParam_instance),
                                    Expression.Constant(methodPath)
                                ));
                                harmony.Patch(method, new HarmonyMethod(_InstanceSet));
                            }
                            else
                            {
                                staticSetIdCases.Add(Expression.SwitchCase(
                                    Expression.Constant(splitted[0]),
                                    Expression.Constant(methodPath)
                                ));
                                harmony.Patch(method, new HarmonyMethod(_StaticSet));
                            }
                        }
                    }

                    foreach (var property in type.GetAllProperties())
                    {
                        if (AttributeGetter.TryGetAttribute<SyncAttribute>(property, out var att))
                        {
                            string propertyPath = $"{type.FullName}.{property.Name}";
                            var propertyPathConst = Expression.Constant(propertyPath);

                            /* ---------------------------------- 编辑方法 ---------------------------------- */
                            Harmony harmony = new($"SyncPacker.harmony.{propertyPath}");

                            //? 非静态的处理在 Entity 中
                            if (!property.GetMethod.IsStatic)
                            {

                            }
                            else
                            {
                                //* 绑定钩子
                                if (!att.hook.IsNullOrWhiteSpace())
                                {
                                    //获取钩子方法
                                    MethodInfo hookMethod = !att.hook.Contains(".") ? type.GetMethodFromAllIncludingBases(att.hook) : ModFactory.SearchUserMethod(att.hook);

                                    //检查是否找到钩子方法
                                    if (hookMethod == null)
                                    {
                                        Debug.LogError($"无法找到 {propertyPath} 的钩子: {att.hook}");
                                        continue;
                                    }

                                    //绑定钩子方法
                                    OnVarValueChange += (var, value) =>
                                    {
                                        if (var.varId == propertyPath)
                                        {
                                            hookMethod.Invoke(null, null);
                                        }
                                    };
                                }



                                //* 如果是 值形式 的默认值
                                if (AttributeGetter.TryGetAttribute<SyncDefaultValueAttribute>(property, out var defaultValueAttribute))
                                {
                                    //检查类型错误, 例如属性是 float 类型, 默认值却填写了 123 就会报错, 要写 123f
                                    if (defaultValueAttribute.defaultValue != null && property.PropertyType.FullName != defaultValueAttribute.defaultValue.GetType().FullName)
                                    {
                                        Debug.LogError($"同步变量 {propertyPath} 错误: 返回值为 {property.PropertyType.FullName} , 但默认值为 {defaultValueAttribute.defaultValue.GetType().FullName}");
                                        continue;
                                    }

                                    //把默认值转为 byte[] 然后保存
                                    var value = Rpc.ObjectToBytes(defaultValueAttribute.defaultValue);

                                    //绑定注册
                                    staticVarsRegisterMethods += () => SyncPacker.RegisterVar(propertyPath, true, value);
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
                                    if (property.PropertyType.FullName != defaultValueMethod.ReturnType.FullName)
                                    {
                                        Debug.LogError($"同步变量 {propertyPath} 错误: 返回值为 {property.PropertyType.FullName} , 但默认值为 {defaultValueMethod.ReturnType.FullName}");
                                        continue;
                                    }

                                    if (defaultValueFromMethodAttribute.getValueUntilRegister)
                                    {
                                        //在注册时获取默认值并转为 byte[]
                                        staticVarsRegisterMethods += () =>
                                        {
                                            SyncPacker.RegisterVar(propertyPath, true, Rpc.ObjectToBytes(defaultValueMethod.Invoke(null, null)));
                                        };
                                    }
                                    else
                                    {
                                        //获取默认值并转为 byte[] 然后保存
                                        var value = Rpc.ObjectToBytes(defaultValueMethod.Invoke(null, null));

                                        //绑定注册
                                        staticVarsRegisterMethods += () => SyncPacker.RegisterVar(propertyPath, true, value);
                                    }
                                }
                                //* 如果是无默认值
                                else
                                {
                                    staticVarsRegisterMethods += () => SyncPacker.RegisterVar(propertyPath, true, null);
                                }
                            }
                        }
                    }
                }
            }

            /* ----------------------------- 定义方法体 (Switch) ----------------------------- */
            var instanceGetIdBody = Expression.Switch(instanceGetIdParam_getter, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_GetIDError)), instanceGetIdParam_getter), instanceGetIdCases.ToArray());
            var instanceSetIdBody = Expression.Switch(instanceSetIdParam_setter, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_GetIDError)), instanceSetIdParam_setter), instanceSetIdCases.ToArray());

            var staticGetIdBody = Expression.Switch(staticGetIdParam_getter, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_GetIDError)), staticGetIdParam_getter), staticGetIdCases.ToArray());
            var staticSetIdBody = Expression.Switch(staticSetIdParam_setter, Expression.Call(typeof(SyncPacker).GetMethod(nameof(_GetIDError)), staticSetIdParam_setter), staticSetIdCases.ToArray());

            /* ---------------------------------- 编译方法 ---------------------------------- */
            InstanceGetId = Expression.Lambda<Func<string, uint, string>>(instanceGetIdBody, instanceGetIdParam_getter, instanceGetIdParam_instance).Compile();
            InstanceSetId = Expression.Lambda<Func<string, uint, string>>(instanceSetIdBody, instanceSetIdParam_setter, instanceSetIdParam_instance).Compile();

            StaticGetId = Expression.Lambda<Func<string, string>>(staticGetIdBody, staticGetIdParam_getter).Compile();
            StaticSetId = Expression.Lambda<Func<string, string>>(staticSetIdBody, staticSetIdParam_setter).Compile();

            StaticVarsRegister = staticVarsRegisterMethods;





            NetworkCallbacks.OnTimeToServerCallback += async () =>
            {
                await UniTask.WaitUntil(() => Client.isClient);
                await UniTask.NextFrame();

                StaticVarsRegister();
            };
        }

        public static string GetInstanceID(string property, uint instance)
        {
            return $"{property}-{instance}";
        }

        public static string GetInstanceID(StringBuilder sb, string property, uint instance)
        {
            sb.Append(property).Append("-").Append(instance);

            //设置 varName 并注册变量
            string value = sb.ToString();
            sb.Clear();

            return value;
        }










        [RuntimeInitializeOnLoadMethod]
        private static void BindMethods()
        {
            NetworkCallbacks.OnClientReady += conn =>
            {
                //如果是自己就不注册及同步
                if (conn == Server.localConnection)
                    return;

                //当新客户端进入时使其注册同步变量
                foreach (KeyValuePair<string, NMSyncVar> entry in vars)
                {
                    //让指定客户端重新注册同步变量
                    conn.Send<NMRegisterSyncVar>(new(entry.Value.varId, entry.Value.clientCanSet, entry.Value.value));
                }
            };

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

            static void ClearVars()
            {
                vars.Clear();
                Debug.Log($"退出或关闭了服务器, {nameof(vars)} 被清空");
            }

            NetworkCallbacks.OnStartServer += () =>
            {
                StartAutoSync();
            };

            void OnClientGetNMSyncVar(NMSyncVar nm)
            {
                //如果自己是服务器就不要同步
                if (Server.isServer)
                    return;

                if (vars.TryGetValue(nm.varId, out var var))
                {
                    var oldValue = var.value;
                    var.value = nm.value;
                    vars[nm.varId] = var;
                    OnVarValueChange.Invoke(var, oldValue);

                    //Debug.Log($"同步了变量 {var.varId} 为 {var.varValue}");
                    return;
                }

                //Debug.LogError($"同步变量 {var.varId} 的值为 {var.varValue} 失败");
            }

            void OnServerGetNMSyncVar(NetworkConnectionToClient conn, NMSyncVar nm)
            {
                if (vars.TryGetValue(nm.varId, out var var))
                {
                    //TODO: 安全检查: if (!nm.clientCanSet&&conn.owned)
                    var oldValue = var.value;
                    var.value = nm.value;
                    vars[nm.varId] = var;

                    OnVarValueChange.Invoke(var, oldValue);
                }
            }

            void OnClientGetNMRegisterSyncVar(NMRegisterSyncVar nm)
            {
                //服务器会提前进行注册, 不需要重复注册
                if (Server.isServer)
                    return;

                LocalRegisterSyncVar(nm);
            }

            NetworkCallbacks.OnTimeToServerCallback += () =>
            {
                Server.Callback<NMSyncVar>(OnServerGetNMSyncVar);
            };
            NetworkCallbacks.OnTimeToClientCallback += () =>
            {
                Client.Callback<NMRegisterSyncVar>(OnClientGetNMRegisterSyncVar);
                Client.Callback<NMUnregisterSyncVar>(UnregisterVarCore);
                Client.Callback<NMSyncVar>(OnClientGetNMSyncVar);
            };
        }

        private static void LocalRegisterSyncVar(NMRegisterSyncVar var)
        {
            //Debug.Log($"注册了{var.varId}");

            if (!vars.TryAdd(var.varId, new(var.varId, var.defaultValue, var.clientCanSet)))
            {
                Debug.LogError($"注册 {var.varId} 失败, 其已存在");
            }
            //Debug.Log($"注册了同步变量 {var.varId}");
            OnRegisterVar(var);
        }
    }
}
