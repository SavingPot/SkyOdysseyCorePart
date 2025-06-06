using HarmonyLib;
using Mirror;
using MonoMod.Utils;
using SP.Tools;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System;
using UnityEngine;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace GameCore.Network
{
    //TODO: 性能优化
    public static class Rpc
    {
        public static bool initialized { get; private set; }
        public static BinaryFormatter binaryFormatter;
        public static SurrogateSelector surrogateSelector;
        public static Func<string, NetworkConnection, byte[], byte[], byte[], byte[], byte[], Entity, bool> Remote;
        public static Action<string, NetworkConnection, byte[], byte[], byte[], byte[], byte[], uint> LocalCall;


        private static bool TryGetRpcInfo(MethodInfo mtd, string mtdPath, Type voidType, Type connType, out RpcType type)
        {
            RpcAttribute att = null;
            type = RpcType.ServerRpc;

            if (AttributeGetter.TryGetAttribute<ServerRpcAttribute>(mtd, out var attTemp1))
            {
                type = RpcType.ServerRpc;
                att = attTemp1;
            }
            else if (AttributeGetter.TryGetAttribute<ClientRpcAttribute>(mtd, out var attTemp2))
            {
                type = RpcType.ClientRpc;
                att = attTemp2;
            }
            else if (AttributeGetter.TryGetAttribute<ConnectionRpc>(mtd, out var attTemp3))
            {
                type = RpcType.ConnectionRpc;
                att = attTemp3;
            }

            if (att == null)
            {
                return false;
            }



            if (mtd.ReturnType != voidType)
            {
                Debug.LogError($"{mtdPath} 使用了 {nameof(RpcAttribute)} 特性, 必须无返回值");
                return false;
            }

            var parameters = mtd.GetParameters();
            if (parameters.Length == 0 || parameters[^1].ParameterType != connType || parameters[^1].Name != "caller")
            {
                Debug.LogError($"{mtdPath} 使用了 {nameof(RpcAttribute)} 特性, 最后一个参数必须为 NetworkConnection caller");
                return false;
            }
            for (int i = 0; i < parameters.Length - 1; i++)
            {
                var parameter = parameters[i];
                if (parameter.HasDefaultValue)
                {
                    Debug.LogError($"{mtdPath} 使用了 {nameof(RpcAttribute)} 特性, 参数 {i} 不能有默认值");
                    return false;
                }
            }

            return true;
        }





        /* -------------------------------------------------------------------------- */
        /*                                   内部反射方法                                   */
        /* -------------------------------------------------------------------------- */
        private static bool _RemoteCall(NMRpc temp, NetworkConnection caller, byte[] parameter0, byte[] parameter1, byte[] parameter2, byte[] parameter3, byte[] parameter4, Entity instance)
        {
            temp.parameter0 = parameter0;
            temp.parameter1 = parameter1;
            temp.parameter2 = parameter2;
            temp.parameter3 = parameter3;
            temp.parameter4 = parameter4;
            temp.instance = instance != null ? instance.netId : uint.MaxValue;

            //以下 Send 的回调绑定在 ManagerNetwork 中
            switch (temp.callType)
            {
                case RpcType.ServerRpc:
                    {
                        //如果是服务器就直接调用
                        if (Server.isServer)
                            LocalCall(temp.methodPath, Server.localConnection, temp.parameter0, temp.parameter1, temp.parameter2, temp.parameter3, temp.parameter4, temp.instance);
                        //如果是客户端就发给服务器
                        else
                            Client.Send(temp);

                        break;
                    }

                case RpcType.ClientRpc:
                    {
                        //如果是服务器就直接发给所有客户端
                        if (Server.isServer)
                            Server.Send(temp);
                        //如果是客户端就发给服务器, 让服务器发给所有客户端
                        else
                            Client.Send(temp);
                        break;
                    }

                case RpcType.ConnectionRpc:
                    {
                        if (caller == null)
                        {
                            Debug.LogError($"调用类型为 {nameof(ConnectionRpc)} 的方法 {temp.methodPath} 时 {nameof(caller)} 不能为空, 必须提供参数!");
                            break;
                        }

                        /* //* 这通常不会触发报错, 在遇到问题时可以取消注释, 以便调试
                        if (!Server.isServer)
                        {
                            Debug.LogError($"{nameof(NC_Type)}.{nameof(NC_Type.ConnectionRpc)} 方法只能在服务器被调用");
                            return;
                        }
                        */

                        //向发送者发送回去
                        caller.Send(temp);
                        break;
                    }
            }

            return false;
        }

        private static bool _StaticRemote0(NetworkConnection caller, MethodBase __originalMethod)
        {
            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, null, null, null, null, null, null);
        }

        private static bool _StaticRemote1(NetworkConnection caller, object[] __args, MethodBase __originalMethod)
        {
            byte[] parameter0 = ObjectToBytes(__args[0]);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, parameter0, null, null, null, null, null);
        }

        private static bool _StaticRemote2(NetworkConnection caller, object[] __args, MethodBase __originalMethod)
        {
            byte[] parameter0 = ObjectToBytes(__args[0]);
            byte[] parameter1 = ObjectToBytes(__args[1]);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, parameter0, parameter1, null, null, null, null);
        }

        private static bool _StaticRemote3(NetworkConnection caller, object[] __args, MethodBase __originalMethod)
        {
            byte[] parameter0 = ObjectToBytes(__args[0]);
            byte[] parameter1 = ObjectToBytes(__args[1]);
            byte[] parameter2 = ObjectToBytes(__args[2]);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, parameter0, parameter1, parameter2, null, null, null);
        }

        private static bool _StaticRemote4(NetworkConnection caller, object[] __args, MethodBase __originalMethod)
        {
            byte[] parameter0 = ObjectToBytes(__args[0]);
            byte[] parameter1 = ObjectToBytes(__args[1]);
            byte[] parameter2 = ObjectToBytes(__args[2]);
            byte[] parameter3 = ObjectToBytes(__args[3]);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, parameter0, parameter1, parameter2, parameter3, null, null);
        }

        private static bool _StaticRemote5(NetworkConnection caller, object[] __args, MethodBase __originalMethod)
        {
            byte[] parameter0 = ObjectToBytes(__args[0]);
            byte[] parameter1 = ObjectToBytes(__args[1]);
            byte[] parameter2 = ObjectToBytes(__args[2]);
            byte[] parameter3 = ObjectToBytes(__args[3]);
            byte[] parameter4 = ObjectToBytes(__args[4]);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, parameter0, parameter1, parameter2, parameter3, parameter4, null);
        }

        private static bool _InstanceRemote0(NetworkConnection caller, Entity __instance, MethodBase __originalMethod)
        {
            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, null, null, null, null, null, __instance);
        }

        private static bool _InstanceRemote1(NetworkConnection caller, object[] __args, Entity __instance, MethodBase __originalMethod)
        {
            byte[] parameter0 = ObjectToBytes(__args[0]);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, parameter0, null, null, null, null, __instance);
        }

        private static bool _InstanceRemote2(NetworkConnection caller, object[] __args, Entity __instance, MethodBase __originalMethod)
        {
            byte[] parameter0 = ObjectToBytes(__args[0]);
            byte[] parameter1 = ObjectToBytes(__args[1]);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, parameter0, parameter1, null, null, null, __instance);
        }

        private static bool _InstanceRemote3(NetworkConnection caller, object[] __args, Entity __instance, MethodBase __originalMethod)
        {
            byte[] parameter0 = ObjectToBytes(__args[0]);
            byte[] parameter1 = ObjectToBytes(__args[1]);
            byte[] parameter2 = ObjectToBytes(__args[2]);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, parameter0, parameter1, parameter2, null, null, __instance);
        }

        private static bool _InstanceRemote4(NetworkConnection caller, object[] __args, Entity __instance, MethodBase __originalMethod)
        {
            byte[] parameter0 = ObjectToBytes(__args[0]);
            byte[] parameter1 = ObjectToBytes(__args[1]);
            byte[] parameter2 = ObjectToBytes(__args[2]);
            byte[] parameter3 = ObjectToBytes(__args[3]);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, parameter0, parameter1, parameter2, parameter3, null, __instance);
        }

        private static bool _InstanceRemote5(NetworkConnection caller, object[] __args, Entity __instance, MethodBase __originalMethod)
        {
            byte[] parameter0 = ObjectToBytes(__args[0]);
            byte[] parameter1 = ObjectToBytes(__args[1]);
            byte[] parameter2 = ObjectToBytes(__args[2]);
            byte[] parameter3 = ObjectToBytes(__args[3]);
            byte[] parameter4 = ObjectToBytes(__args[4]);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, parameter0, parameter1, parameter2, parameter3, parameter4, __instance);
        }



        public static byte[] ObjectToBytes(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            using MemoryStream ms = new();
            binaryFormatter.Serialize(ms, obj);
            return ms.ToArray();
        }

        public static T BytesToObject<T>(byte[] data)
        {
            if (data == null)
            {
                return default;
            }

            using MemoryStream ms = new(data);
            var obj = binaryFormatter.Deserialize(ms);
            return obj == null ? default : (T)obj;
        }

        public static object BytesToObject(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            using MemoryStream ms = new(data);
            return binaryFormatter.Deserialize(ms);
        }









        internal static MethodInfo CopyMethod(MethodBase mtd)
        {
            using var dynamicMethod = new DynamicMethodDefinition(mtd);
            MethodInfo after = dynamicMethod.Generate();

            return after;
        }




        public static void Init()
        {
            initialized = false;
            int delayDuration = 0;

            //TODO: 有一个重要的问题: 若是一个方法有多个重载, 会不会出现问题？ 还是说不同的重载名字不一样？
            /* -------------------------------------------------------------------------- */
            /*                              //Step 1: 定义反射参数
            /* -------------------------------------------------------------------------- */
            //Substep1: 获取类型和标记
            Type voidType = typeof(void);
            Type connType = typeof(NetworkConnection);
            BindingFlags flags = ReflectionTools.BindingFlags_All;

            //Substep2: 获取定义的方法
            var BytesToObject = typeof(Rpc).GetMethod(nameof(Rpc.BytesToObject), 0, new Type[] { typeof(byte[]) });

            var _RemoteCall = typeof(Rpc).GetMethod($"{nameof(Rpc._RemoteCall)}", flags);

            var _StaticRemote0 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticRemote0)}", flags);
            var _StaticRemote1 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticRemote1)}", flags);
            var _StaticRemote2 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticRemote2)}", flags);
            var _StaticRemote3 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticRemote3)}", flags);
            var _StaticRemote4 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticRemote4)}", flags);
            var _StaticRemote5 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticRemote5)}", flags);

            var _InstanceRemote0 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceRemote0)}", flags);
            var _InstanceRemote1 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceRemote1)}", flags);
            var _InstanceRemote2 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceRemote2)}", flags);
            var _InstanceRemote3 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceRemote3)}", flags);
            var _InstanceRemote4 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceRemote4)}", flags);
            var _InstanceRemote5 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceRemote5)}", flags);





            /* -------------------------------------------------------------------------- */
            /*                         //Step 2: 定义 Expression 参数
            /* -------------------------------------------------------------------------- */
            //Substep 1: 定义传入方法的参数
            ParameterExpression remoteParam_id = Expression.Parameter(typeof(string), "id");
            ParameterExpression remoteParam_caller = Expression.Parameter(typeof(NetworkConnection), "caller");
            ParameterExpression remoteParam_parameter0 = Expression.Parameter(typeof(byte[]), "parameter0");
            ParameterExpression remoteParam_parameter1 = Expression.Parameter(typeof(byte[]), "parameter1");
            ParameterExpression remoteParam_parameter2 = Expression.Parameter(typeof(byte[]), "parameter2");
            ParameterExpression remoteParam_parameter3 = Expression.Parameter(typeof(byte[]), "parameter3");
            ParameterExpression remoteParam_parameter4 = Expression.Parameter(typeof(byte[]), "parameter4");
            ParameterExpression remoteParam_instance = Expression.Parameter(typeof(Entity), "instance");

            ParameterExpression localParam_exception = Expression.Parameter(typeof(Exception), "exception");
            ParameterExpression localParam_bytesToObjectTemp = Expression.Parameter(typeof(object), "bytesToObjectTemp");
            ParameterExpression localParam_id = Expression.Parameter(typeof(string), "id");
            ParameterExpression localParam_caller = Expression.Parameter(typeof(NetworkConnection), "caller");
            ParameterExpression localParam_parameter0 = Expression.Parameter(typeof(byte[]), "parameter0");
            ParameterExpression localParam_parameter1 = Expression.Parameter(typeof(byte[]), "parameter1");
            ParameterExpression localParam_parameter2 = Expression.Parameter(typeof(byte[]), "parameter2");
            ParameterExpression localParam_parameter3 = Expression.Parameter(typeof(byte[]), "parameter3");
            ParameterExpression localParam_parameter4 = Expression.Parameter(typeof(byte[]), "parameter4");
            ParameterExpression localParam_instance = Expression.Parameter(typeof(uint), "instance");

            //Substep 2: 定义 Switch Cases
            List<SwitchCase> remoteCases = new();
            List<SwitchCase> localCases = new();





            /* -------------------------------------------------------------------------- */
            /*                              //Step 3: 为 BinaryFormatter 匹配正确的转化器
            /* -------------------------------------------------------------------------- */
            surrogateSelector = new();


            //? 为所有 Entity 指定代理, 避免反复编写代理
            //? 先指定实体代理, 再设定通用代理, 是为了方便定制
            //? 例如: 我有一个 Player, 需要传输 int value 这个值, 就需要把设定通用代理放在后面, 并写一个 PlayerSurrogate
            ModFactory.assemblies.Foreach(ass =>
            {
                Parallel.ForEach(ass.GetTypes(), type =>
                {
                    if (type.IsSubclassOf(typeof(Entity)))
                    {
                        surrogateSelector.AddSurrogate(
                                type,
                                new StreamingContext(StreamingContextStates.All),
                                (ISerializationSurrogate)Activator.CreateInstance(typeof(SerializationSurrogates.EntitySurrogate))
                            );
                    }
                });
            });

            ModFactory.assemblies.Foreach(ass =>
            {
                Parallel.ForEach(ass.GetTypes(), type =>
                {
                    if (AttributeGetter.TryGetAttribute<SerializationSurrogatesClassAttribute>(type, out _))
                    {
                        type.GetNestedTypes(flags).Foreach(nested =>
                        {
                            var surrogateInterface = nested.GetInterface(typeof(ISerializationSurrogate<>).FullName);

                            if (surrogateInterface != null)
                            {
                                var typePointedTo = surrogateInterface.GetGenericArguments()[0];

                                //删去通用的实体代理
                                if (typePointedTo != typeof(Entity) && typePointedTo.IsSubclassOf(typeof(Entity)))
                                    surrogateSelector.RemoveSurrogate(typePointedTo, new StreamingContext(StreamingContextStates.All));

                                surrogateSelector.AddSurrogate(
                                    typePointedTo,
                                    new StreamingContext(StreamingContextStates.All),
                                    (ISerializationSurrogate)Activator.CreateInstance(nested)
                                );
                            }
                        });
                    }
                });
            });

            binaryFormatter = new()
            {
                SurrogateSelector = surrogateSelector
            };





            /* -------------------------------------------------------------------------- */
            /*                              //Step 6: 更改带有 RpcBinder 的方法的内容
            /* -------------------------------------------------------------------------- */

            //获取所有可用方法
            ModFactory.EachUserMethod(async (ass, type, mtd) =>
            {
                string mtdPath = $"{type.FullName}.{mtd.Name}";

                //获取呼叫类型
                if (TryGetRpcInfo(mtd, mtdPath, voidType, connType, out RpcType callType))
                {
                    /* ---------------------------------- 生成常量 ---------------------------------- */
                    var mtdPathConst = Expression.Constant(mtdPath);
                    var NMRpcTemp = Expression.Constant(new NMRpc(mtdPath, callType));

                    /* ---------------------------------- 获取参数 ---------------------------------- */
                    var totalParameters = mtd.GetParameters();   //参数分为真参数和 NetworkConnection
                    List<Type> trueParameters = new();
                    for (int i = 0; i < totalParameters.Length - 1; i++)
                    {
                        trueParameters.Add(totalParameters[i].ParameterType);
                    }
                    var parameterListConst = Expression.Constant(trueParameters.ToArray());

                    /* ---------------------------------- 编辑方法 ---------------------------------- */
                    MethodInfo mtdCopy = CopyMethod(mtd);
                    Harmony harmony = new($"Rpc.harmony.{mtdPath}");

                    void PatchMethod(MethodInfo patch)
                    {
                        try
                        {
                            harmony.Patch(mtd, new HarmonyMethod(patch));
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"为 Rpc 方法 {mtdPath} 打补丁时抛出异常!\n\n{ex.GetType().FullName}: {ex.Message}\n{Tools.HighlightedStackTrace()}");
                        }
                    }

                    BlockExpression LocalCaseParameterGeneration(int index)
                    {
                        var parameterToSelect = index switch
                        {
                            0 => localParam_parameter0,
                            1 => localParam_parameter1,
                            2 => localParam_parameter2,
                            3 => localParam_parameter3,
                            4 => localParam_parameter4,
                            _ => throw new()
                        };

                        return Expression.Block(
                                    trueParameters[index],
                                    new ParameterExpression[] { localParam_bytesToObjectTemp },
                                    new Expression[]
                                    {
                                        Expression.Assign(
                                            localParam_bytesToObjectTemp,
                                            Expression.Call(
                                                BytesToObject,
                                                parameterToSelect
                                            )
                                        ),
                                        Expression.Condition(
                                            Expression.Equal(
                                                localParam_bytesToObjectTemp,
                                                Expression.Constant(null)
                                            ),
                                            //* if (objectTemp == null) return default(T);
                                            Expression.Default(trueParameters[index]),//TODO: Maybe can't be Default
                                            //* else return (T)objectTemp;
                                            Expression.Convert(
                                                localParam_bytesToObjectTemp,
                                                trueParameters[index]
                                            )
                                        )
                                    }
                                );
                    }

                    void AddLocalCase(Expression[] arguments, bool isInstanceMethod)
                    {
                        List<Expression> actualArgument = new();

                        if (isInstanceMethod)
                            actualArgument.Add(
                                Expression.Convert(
                                        Expression.Call(
                                            typeof(EntityCenter).GetMethod(nameof(EntityCenter.GetEntityByNetId), new Type[] { typeof(uint), typeof(Type) }),
                                            localParam_instance,
                                            Expression.Constant(mtdCopy.GetParameters()[0].ParameterType)
                                        ),
                                        mtdCopy.GetParameters()[0].ParameterType));

                        actualArgument.AddRange(arguments);
                        actualArgument.Add(localParam_caller);

                        localCases.Add(
                            Expression.SwitchCase(
                                Expression.TryCatch(
                                    Expression.Call(
                                        mtdCopy,
                                        actualArgument.ToArray()
                                    ),
                                    Expression.Catch(
                                        localParam_exception,
                                        Expression.Call(
                                            typeof(Debug).GetMethod("LogError", new Type[] { typeof(object) }),
                                            Expression.Call(
                                                null,
                                                typeof(string).GetMethod(nameof(string.Format), new Type[] { typeof(string), typeof(object) }),
                                                Expression.Constant("调用本地方法 <color=#e5c072>" + mtdPath + "</color> 时抛出了异常!!!\n<color=#e5c072>异常源于本地方法或字节转换器的代码! 请根据第一行堆栈判断是否是本地方法</color>\n如果是本地方法, 第一行通常包括 LocalMethod_Lambda, 请尝试在本地方法中括上一个 MethodAgent.DebugRun(action) 或者是 try-catch 块来捕获异常等\n如果不是, 请检查字节转换器\n异常将被抛出以防止破坏客户端和服务器, 具体异常如下!\n{0}"),
                                                Expression.Call(
                                                    typeof(Tools).GetMethod(nameof(Tools.HighlightedStackTrace), new Type[] { typeof(Exception) }),
                                                        localParam_exception
                                                    )
                                                )
                                            )
                                        )
                                    ),
                                    mtdPathConst
                                )
                            );
                    }

                    if (mtd.IsStatic)
                    {
                        PatchMethod(trueParameters.Count switch
                        {
                            0 => _StaticRemote0,
                            1 => _StaticRemote1,
                            2 => _StaticRemote2,
                            3 => _StaticRemote3,
                            4 => _StaticRemote4,
                            5 => _StaticRemote5,
                            _ => null
                        });

                        /* --------------------------------- 绑定 Case -------------------------------- */
                        //? 以下为重难点
                        //? == 表示等效于
                        //? 认真看其实很好理解


                        //*== case mtdPath:
                        //*==   _RemoteCall(mtdInfo, caller, writer);
                        //*==   break;
                        remoteCases.Add(Expression.SwitchCase(
                                            Expression.Call(
                                                null,
                                                _RemoteCall,
                                                NMRpcTemp,
                                                        remoteParam_caller,
                                                        remoteParam_parameter0,
                                                        remoteParam_parameter1,
                                                        remoteParam_parameter2,
                                                        remoteParam_parameter3,
                                                        remoteParam_parameter4,
                                                        Expression.Constant(null, typeof(Entity))
                                                    ),
                                            mtdPathConst));

                        switch (trueParameters.Count)
                        {
                            case 0:
                                AddLocalCase(new Expression[0], false);

                                break;

                            case 1:
                                AddLocalCase(new Expression[]
                                {
                                    LocalCaseParameterGeneration(0),
                                }, false);

                                break;

                            case 2:
                                AddLocalCase(new Expression[]
                                {
                                    LocalCaseParameterGeneration(0),
                                    LocalCaseParameterGeneration(1),
                                }, false);

                                break;

                            case 3:
                                AddLocalCase(new Expression[]
                                {
                                    LocalCaseParameterGeneration(0),
                                    LocalCaseParameterGeneration(1),
                                    LocalCaseParameterGeneration(2),
                                }, false);

                                break;

                            case 4:
                                AddLocalCase(new Expression[]
                                {
                                    LocalCaseParameterGeneration(0),
                                    LocalCaseParameterGeneration(1),
                                    LocalCaseParameterGeneration(2),
                                    LocalCaseParameterGeneration(3),
                                }, false);

                                break;

                            case 5:
                                AddLocalCase(new Expression[]
                                {
                                    LocalCaseParameterGeneration(0),
                                    LocalCaseParameterGeneration(1),
                                    LocalCaseParameterGeneration(2),
                                    LocalCaseParameterGeneration(3),
                                    LocalCaseParameterGeneration(4),
                                }, false);

                                break;

                            default:
                                Debug.LogError($"方法 {mtdPath} 包含不支持的参数数量");
                                break;
                        }
                    }
                    else
                    {
                        PatchMethod(trueParameters.Count switch
                        {
                            0 => _InstanceRemote0,
                            1 => _InstanceRemote1,
                            2 => _InstanceRemote2,
                            3 => _InstanceRemote3,
                            4 => _InstanceRemote4,
                            5 => _InstanceRemote5,
                            _ => null
                        });

                        /* --------------------------------- 绑定 Case -------------------------------- */
                        //? 以下为重难点
                        //? == 表示等效于
                        //? 认真看其实很好理解


                        //*== case mtdPath:
                        //*==   _RemoteCall(mtdInfo, caller, writer);
                        //*==   break;
                        remoteCases.Add(Expression.SwitchCase(
                                            Expression.Call(
                                                null,
                                                _RemoteCall,
                                                NMRpcTemp,
                                                        remoteParam_caller,
                                                        remoteParam_parameter0,
                                                        remoteParam_parameter1,
                                                        remoteParam_parameter2,
                                                        remoteParam_parameter3,
                                                        remoteParam_parameter4,
                                                        remoteParam_instance
                                                    ),
                                            mtdPathConst));

                        switch (trueParameters.Count)
                        {
                            case 0:
                                AddLocalCase(new Expression[0], true);

                                break;

                            case 1:
                                AddLocalCase(new Expression[]
                                {
                                    LocalCaseParameterGeneration(0),
                                }, true);

                                break;

                            case 2:
                                AddLocalCase(new Expression[]
                                {
                                    LocalCaseParameterGeneration(0),
                                    LocalCaseParameterGeneration(1),
                                }, true);

                                break;

                            case 3:
                                AddLocalCase(new Expression[]
                                {
                                    LocalCaseParameterGeneration(0),
                                    LocalCaseParameterGeneration(1),
                                    LocalCaseParameterGeneration(2),
                                }, true);

                                break;

                            case 4:
                                AddLocalCase(new Expression[]
                                {
                                    LocalCaseParameterGeneration(0),
                                    LocalCaseParameterGeneration(1),
                                    LocalCaseParameterGeneration(2),
                                    LocalCaseParameterGeneration(3),
                                }, true);

                                break;

                            case 5:
                                AddLocalCase(new Expression[]
                                {
                                    LocalCaseParameterGeneration(0),
                                    LocalCaseParameterGeneration(1),
                                    LocalCaseParameterGeneration(2),
                                    LocalCaseParameterGeneration(3),
                                    LocalCaseParameterGeneration(4),
                                }, true);

                                break;

                            default:
                                Debug.LogError($"方法 {mtdPath} 包含不支持的参数数量");
                                break;
                        }
                    }



                    //每修改 10 个方法就等一帧以防止游戏卡死
                    delayDuration++;
                    if (delayDuration == 10)
                        await UniTask.NextFrame();
                }
            }, ReflectionTools.BindingFlags_All | BindingFlags.DeclaredOnly);










            /* -------------------------------------------------------------------------- */
            /*                  //Step 7: 编译 Remote & LocalMethod 用于直接调用
            /* -------------------------------------------------------------------------- */

            //SubStep 1: 定义未找到方法时的报错
            //* Debug.LogError($"远程调用方法失败: 未找到方法 {path}");
            //*     return true;
            var remoteNotFoundError = Expression.Block(
                                                        Expression.Call(
                                                            null,
                                                            typeof(Debug).GetMethod("LogError", new Type[] { typeof(object) }),
                                                            Expression.Call(typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }),
                                                                Expression.Constant("远程调用方法失败: 未找到方法 ", typeof(string)),
                                                                remoteParam_id
                                                            )),
                                                        Expression.Constant(true, typeof(bool)
                                                        ));

            //* Debug.LogError($"直接调用方法失败: 未找到方法 {path}");
            var localNotFoundError = Expression.Call(
                                        null, typeof(Debug).GetMethod("LogError", new Type[] { typeof(object) }),
                                        Expression.Call(typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }),
                                            Expression.Constant("直接调用方法失败: 未找到方法 ", typeof(string)),
                                            localParam_id
                                        ));

            //SubStep 2: 定义方法体 (Switch)
            var remoteBody = Expression.Switch(remoteParam_id, remoteNotFoundError, remoteCases.ToArray());
            var localBody = Expression.Switch(localParam_id, localNotFoundError, localCases.ToArray());

            //SubStep 3: 编译
            Remote = Expression.Lambda<Func<string, NetworkConnection, byte[], byte[], byte[], byte[], byte[], Entity, bool>>(remoteBody, "Remote_Lambda", new ParameterExpression[] { remoteParam_id, remoteParam_caller, remoteParam_parameter0, remoteParam_parameter1, remoteParam_parameter2, remoteParam_parameter3, remoteParam_parameter4, remoteParam_instance }).Compile();
            LocalCall = Expression.Lambda<Action<string, NetworkConnection, byte[], byte[], byte[], byte[], byte[], uint>>(localBody, "LocalMethod_Lambda", new ParameterExpression[] { localParam_id, localParam_caller, localParam_parameter0, localParam_parameter1, localParam_parameter2, localParam_parameter3, localParam_parameter4, localParam_instance }).Compile();



            initialized = true;
            SyncPacker.Init();
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class NonNetworkAttribute : Attribute
    {

    }
}