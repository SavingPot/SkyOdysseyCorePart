using HarmonyLib;
using Mirror;
using MonoMod.Utils;
using SP.Tools;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System;
using UnityEngine;
using System.Linq;
using System.Reflection.Emit;
using Mono.Cecil.Cil;
using System.ComponentModel;
using GameCore.Converters;

namespace GameCore
{
    //TODO: 性能优化
    //TODO: 自动写入Enum, 支持 sbyte, byte, short, ushort, int, uint, long, ulong
    public static class Rpc
    {
        public static Func<string, NetworkConnection, ByteWriter, Entity, bool> Remote;
        public static Action<string, NetworkConnection, ByteWriter, Entity> LocalMethod;
        public static Func<
            Type, FieldInfo, MemberExpression, IndexExpression, Type[], ParameterExpression,
            (bool isSupported, Expression writer, MemberAssignment memberBinding)>
            GenericTypeSupport =
                (fieldType, fieldInfo, fieldInstance, writerToRead, genericArguments, writerToWrite) =>
        {
            //检测调用时想要的是 是否支持 还是 读写方法
            bool wantedResultIsSupported = fieldInfo == null;
            Debug.Log($"{fieldType.FullName} {writerToRead == null}");
            Expression chunksToRead = Expression.Field(writerToRead, typeof(ByteWriter).GetField(nameof(ByteWriter.chunks))); //*== writerToRead.chunks


            if (fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (wantedResultIsSupported)
                    return (true, null, null);

                var write_LoopIndex = Expression.Variable(typeof(int), "i");
                var write_BreakLabel = Expression.Label("break");

                var read_NewListExpression = Expression.Variable(fieldType, "list");
                var read_LoopIndex = Expression.Variable(typeof(int), "i");
                var read_BreakLabel = Expression.Label("break");

                var genericArg = genericArguments[0];
                var writerMethod = ByteWriter.GetWriter(genericArg.FullName);
                var readerMethod = ByteReader.GetReader(genericArg.FullName);

                return (
                    true,
                    //*== for (int i = 0; i < values.Length ; i++)               values is List<T>
                    //*== {
                    //*==   method.Invoke(null, new object[] { values[i], writer });
                    //*== }
                    Expression.Block(
                        typeof(void),
                        new ParameterExpression[]
                        {
                            write_LoopIndex
                        },
                        new Expression[]
                        {
                            //定义一个类似 for 语句的循环
                            Expression.Loop(
                                Expression.IfThenElse(
                                    //检测是否遍历完成
                                    Expression.LessThan(
                                        write_LoopIndex,
                                        fieldInstance.ListCount()
                                    ),
                                    //遍历时执行
                                    Expression.Block(
                                        //调用字节写入器
                                        ByteWriter.GetExpressionOfWriting(writerToWrite, fieldInstance.ListItem(write_LoopIndex), genericArg),
                                        //等效于 i++
                                        Expression.PostIncrementAssign(write_LoopIndex)
                                    ),
                                    //如果遍历完了就 break
                                    Expression.Break(write_BreakLabel)
                                ),
                                write_BreakLabel
                            )
                        }
                    ),
                    //*== 在读取 field 时执行: 
                    //*== List<T> values = new();
                    //*==
                    //*== for (int i = 0; i < writer.chunks.Count; i++)
                    //*== {
                    //*==   values.Add(reader(writer.chunks[i]));
                    //*== }
                    //*==
                    //*== return values;
                    Expression.Bind(
                        fieldInfo,
                        Expression.Block(
                            fieldType,
                            new ParameterExpression[]
                            {
                                read_LoopIndex,
                                read_NewListExpression
                            },
                            new Expression[]
                            {
                                Expression.Assign(read_NewListExpression, Expression.New(fieldType)),
                                //定义一个类似 for 语句的循环
                                Expression.Loop(
                                    Expression.IfThenElse(
                                        //检测是否遍历完成
                                        Expression.LessThan(
                                            read_LoopIndex,
                                            chunksToRead.ListCount()
                                        ),
                                        //遍历时执行
                                        Expression.Block(
                                            //为列表添加元素
                                            read_NewListExpression.ListAdd(
                                                //提供列表的类型以获取对应 Add 方法
                                                fieldType,
                                                //把写入器对应的区块投给 T 的字节读取器
                                                ByteReader.GetExpressionOfReading(chunksToRead.ListItem(read_LoopIndex), genericArg)
                                            ),
                                            //等效于 i++
                                            Expression.PostIncrementAssign(read_LoopIndex)
                                        ),
                                        //如果遍历完了就 break
                                        Expression.Break(read_BreakLabel)
                                    ),
                                    read_BreakLabel
                                ),
                                read_NewListExpression
                            }
                        )
                    )
                );
            }
            else if (fieldType.IsArray)
            {
                if (wantedResultIsSupported)
                    return (true, null, null);

                var write_LoopIndex = Expression.Variable(typeof(int), "i");
                var write_BreakLabel = Expression.Label("break");

                var read_NewArrayExpression = Expression.Variable(fieldType, "array");
                var read_LoopIndex = Expression.Variable(typeof(int), "i");
                var read_BreakLabel = Expression.Label("break");

                var genericArg = genericArguments[0];
                var writerMethod = ByteWriter.GetWriter(genericArg.FullName);
                var readerMethod = ByteReader.GetReader(genericArg.FullName);

                return (true,
                    //*== for (int i = 0; i < values.Length ; i++)               values is T[]
                    //*== {
                    //*==   method.Invoke(null, new object[] { values[i], writer });
                    //*== }
                    Expression.Block(
                        typeof(void),
                        new ParameterExpression[]
                        {
                            write_LoopIndex
                        },
                        new Expression[]
                        {
                            //定义一个类似 for 语句的循环
                            Expression.Loop(
                                Expression.IfThenElse(
                                    //检测是否遍历完成
                                    Expression.LessThan(
                                        write_LoopIndex,
                                        fieldInstance.ArrayLength()
                                    ),
                                    //遍历时执行
                                    Expression.Block(
                                        //调用字节写入器
                                        ByteWriter.GetExpressionOfWriting(writerToWrite, fieldInstance.ArrayItem(write_LoopIndex), genericArg),
                                        //等效于 i++
                                        Expression.PostIncrementAssign(write_LoopIndex)
                                    ),
                                    //如果遍历完了就 break
                                    Expression.Break(write_BreakLabel)
                                ),
                                write_BreakLabel
                            )
                        }
                    ),
                    //*== 在读取 field 时执行: 
                    //*== T[] values = new T[];
                    //*==
                    //*== for (int i = 0; i < writer.chunks.Count; i++)
                    //*== {
                    //*==   values[i] = reader(writer.chunks[i]);
                    //*== }
                    //*==
                    //*== return values;
                    //TODO: Don't bind it here
                    Expression.Bind(
                        fieldInfo,
                        Expression.Block(
                            fieldType,
                            new ParameterExpression[]
                            {
                                read_LoopIndex,
                                read_NewArrayExpression
                            },
                            new Expression[]
                            {
                                Expression.Assign(read_NewArrayExpression, Expression.NewArrayBounds(genericArg, fieldInstance)),
                                //定义一个类似 for 语句的循环
                                Expression.Loop(
                                    Expression.IfThenElse(
                                        //检测是否遍历完成
                                        Expression.LessThan(
                                            read_LoopIndex,
                                            chunksToRead.ListCount()
                                        ),
                                        //遍历时执行
                                        Expression.Block(
                                            //被赋值的是数组的元素
                                            Expression.Assign(
                                                //访问创建的数组
                                                read_NewArrayExpression.ArrayItem(read_LoopIndex),
                                                //把写入器对应的区块投给 T 的字节读取器
                                                ByteReader.GetExpressionOfReading(chunksToRead.ListItem(read_LoopIndex), genericArg)
                                            ),
                                            //等效于 i++
                                            Expression.PostIncrementAssign(read_LoopIndex)
                                        ),
                                        //如果遍历完了就 break
                                        Expression.Break(read_BreakLabel)
                                    ),
                                    read_BreakLabel
                                ),
                                read_NewArrayExpression
                            }
                        )
                    )
                );
            }
            else if (fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (wantedResultIsSupported)
                    return (true, null, null);

                var read_Temp = Expression.Variable(fieldType);

                var genericArg = genericArguments[0];
                var chunkZero = chunksToRead.ListItem(0);
                var writerMethod = ByteWriter.GetWriter(genericArg.FullName);
                var readerMethod = ByteReader.GetReader(genericArg.FullName);
                var doNotReadIdentity = Expression.Constant(Converters.ByteConverter.ToBytes(1025943687), typeof(byte[]));   //要写入随机数据, 检测 writer.bytes 而不是 writer.chunks[0].bytes 是因为如果 genericArg 的写入器在某种情况下直接 WriteNull, 会导致判断失误

                return (true,
                    //*== if (nullable == null)     nullable is Nullable<T>
                    //*==   writer.bytes = Converters.ByteConverter.ToBytes(1025943687);
                    //*== else
                    //*==   writerMethod((T)nullable);
                    Expression.IfThenElse(
                        Expression.Equal(fieldInstance, Expression.Constant(null)),
                        Expression.Assign(Expression.Field(writerToWrite, typeof(ByteWriter).GetField(nameof(ByteWriter.bytes))), doNotReadIdentity),
                        ByteWriter.GetExpressionOfWriting(writerToWrite, fieldInstance, genericArg)
                    ),
                    //*== 在读取 field 时执行: 
                    //*== if (writer.bytes != null)
                    //*==   return new T?;
                    //*== else
                    //*==   return reader(writer.chunks[0]);
                    //TODO: Don't bind it here
                    Expression.Bind(
                        fieldInfo,
                        Expression.Block(
                            fieldType,
                            new ParameterExpression[]
                            {
                                read_Temp
                            },
                            new Expression[]
                            {
                                Expression.IfThenElse(
                                    Expression.NotEqual(Expression.Field(writerToRead, typeof(ByteWriter).GetField(nameof(ByteWriter.bytes))), Expression.Constant(null)),
                                    Expression.Assign(read_Temp, Expression.New(fieldType)),
                                    Expression.Assign(read_Temp, Expression.Convert(ByteReader.GetExpressionOfReading(chunkZero, genericArg), fieldType))
                                ),
                                read_Temp
                            }
                        )
                    )
                );
            }

            return (false, null, null);
        };



        static bool GetNetCallType(MethodInfo mtd, string mtdPath, Type voidType, Type connType, out RpcType type)
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
            if (parameters.Length == 0 || parameters[^1].ParameterType != connType)
            {
                Debug.LogError($"{mtdPath} 使用了 {nameof(RpcAttribute)} 特性, 最后一个参数必须为 NetworkConnection");
                return false;
            }

            return true;
        }





        /* -------------------------------------------------------------------------- */
        /*                                   内部反射方法                                   */
        /* -------------------------------------------------------------------------- */
        public static bool _RemoteCall(NMRpc temp, ref NetworkConnection caller, ByteWriter writer, Entity instance)
        {
            temp.parameters = writer;
            temp.instance = instance ? instance.netId : uint.MaxValue;

            //以下 Send 的回调绑定在 ManagerNetwork 中
            switch (temp.callType)
            {
                case RpcType.ServerRpc:
                    {
                        //如果是服务器就直接调用
                        if (Server.isServer)
                            LocalCall(temp.methodPath, caller ?? Server.localConnection, temp.parameters, temp.instance);
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
                        /* //* 这通常不会触发报错, 在遇到问题时可以取消注释, 以便调试
                        if (caller == null)
                        {
                            Debug.LogError("调用 ConnectionRpc 时 conn 不能为空, 必须提供参数!");
                            return;
                        }

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

        public static bool _StaticWrite0(NetworkConnection caller, MethodBase __originalMethod)
        {
            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, ByteWriter.CreateNull(), null);
        }

        public static bool _StaticWrite1(NetworkConnection caller, object[] __args, MethodBase __originalMethod)
        {
            ByteWriter writer = ByteWriter.Create();
            var parameters = __originalMethod.GetParameters();

            ByteWriter.TypeWrite(parameters[0].ParameterType.FullName, __args[0], writer);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, writer, null);
        }

        public static bool _StaticWrite2(NetworkConnection caller, object[] __args, MethodBase __originalMethod)
        {
            ByteWriter writer = ByteWriter.Create();
            var parameters = __originalMethod.GetParameters();

            ByteWriter.TypeWrite(parameters[0].ParameterType.FullName, __args[0], writer);
            ByteWriter.TypeWrite(parameters[1].ParameterType.FullName, __args[1], writer);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, writer, null);
        }

        public static bool _StaticWrite3(NetworkConnection caller, object[] __args, MethodBase __originalMethod)
        {
            ByteWriter writer = ByteWriter.Create();
            var parameters = __originalMethod.GetParameters();

            ByteWriter.TypeWrite(parameters[0].ParameterType.FullName, __args[0], writer);
            ByteWriter.TypeWrite(parameters[1].ParameterType.FullName, __args[1], writer);
            ByteWriter.TypeWrite(parameters[2].ParameterType.FullName, __args[2], writer);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, writer, null);
        }

        public static bool _StaticWrite4(NetworkConnection caller, object[] __args, MethodBase __originalMethod)
        {
            ByteWriter writer = ByteWriter.Create();
            var parameters = __originalMethod.GetParameters();

            ByteWriter.TypeWrite(parameters[0].ParameterType.FullName, __args[0], writer);
            ByteWriter.TypeWrite(parameters[1].ParameterType.FullName, __args[1], writer);
            ByteWriter.TypeWrite(parameters[2].ParameterType.FullName, __args[2], writer);
            ByteWriter.TypeWrite(parameters[3].ParameterType.FullName, __args[3], writer);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, writer, null);
        }

        public static bool _StaticWrite5(NetworkConnection caller, object[] __args, MethodBase __originalMethod)
        {
            ByteWriter writer = ByteWriter.Create();
            var parameters = __originalMethod.GetParameters();

            ByteWriter.TypeWrite(parameters[0].ParameterType.FullName, __args[0], writer);
            ByteWriter.TypeWrite(parameters[1].ParameterType.FullName, __args[1], writer);
            ByteWriter.TypeWrite(parameters[2].ParameterType.FullName, __args[2], writer);
            ByteWriter.TypeWrite(parameters[3].ParameterType.FullName, __args[3], writer);
            ByteWriter.TypeWrite(parameters[4].ParameterType.FullName, __args[4], writer);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, writer, null);
        }

        public static bool _InstanceWrite0(NetworkConnection caller, Entity __instance, MethodBase __originalMethod)
        {
            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, ByteWriter.CreateNull(), __instance);
        }

        public static bool _InstanceWrite1(NetworkConnection caller, object[] __args, Entity __instance, MethodBase __originalMethod)
        {
            ByteWriter writer = ByteWriter.Create();
            var parameters = __originalMethod.GetParameters();

            ByteWriter.TypeWrite(parameters[0].ParameterType.FullName, __args[0], writer);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, writer, __instance);
        }

        public static bool _InstanceWrite2(NetworkConnection caller, object[] __args, Entity __instance, MethodBase __originalMethod)
        {
            ByteWriter writer = ByteWriter.Create();
            var parameters = __originalMethod.GetParameters();

            ByteWriter.TypeWrite(parameters[0].ParameterType.FullName, __args[0], writer);
            ByteWriter.TypeWrite(parameters[1].ParameterType.FullName, __args[1], writer);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, writer, __instance);
        }

        public static bool _InstanceWrite3(NetworkConnection caller, object[] __args, Entity __instance, MethodBase __originalMethod)
        {
            ByteWriter writer = ByteWriter.Create();
            var parameters = __originalMethod.GetParameters();

            ByteWriter.TypeWrite(parameters[0].ParameterType.FullName, __args[0], writer);
            ByteWriter.TypeWrite(parameters[1].ParameterType.FullName, __args[1], writer);
            ByteWriter.TypeWrite(parameters[2].ParameterType.FullName, __args[2], writer);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, writer, __instance);
        }

        public static bool _InstanceWrite4(NetworkConnection caller, object[] __args, Entity __instance, MethodBase __originalMethod)
        {
            ByteWriter writer = ByteWriter.Create();
            var parameters = __originalMethod.GetParameters();

            ByteWriter.TypeWrite(parameters[0].ParameterType.FullName, __args[0], writer);
            ByteWriter.TypeWrite(parameters[1].ParameterType.FullName, __args[1], writer);
            ByteWriter.TypeWrite(parameters[2].ParameterType.FullName, __args[2], writer);
            ByteWriter.TypeWrite(parameters[3].ParameterType.FullName, __args[3], writer);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, writer, __instance);
        }

        public static bool _InstanceWrite5(NetworkConnection caller, object[] __args, Entity __instance, MethodBase __originalMethod)
        {
            ByteWriter writer = ByteWriter.Create();
            var parameters = __originalMethod.GetParameters();

            ByteWriter.TypeWrite(parameters[0].ParameterType.FullName, __args[0], writer);
            ByteWriter.TypeWrite(parameters[1].ParameterType.FullName, __args[1], writer);
            ByteWriter.TypeWrite(parameters[2].ParameterType.FullName, __args[2], writer);
            ByteWriter.TypeWrite(parameters[3].ParameterType.FullName, __args[3], writer);
            ByteWriter.TypeWrite(parameters[4].ParameterType.FullName, __args[4], writer);

            return Remote($"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}", caller, writer, __instance);
        }












        //? 最多支持五个参数 (不包含NetworkConnection)
        public static object _Read0(Type[] parameterTypes, ByteWriter parameters)
        {
            return ByteReader.TypeRead(parameterTypes[0].FullName, parameters.chunks[0]);
        }

        public static object _Read1(Type[] parameterTypes, ByteWriter parameters)
        {
            return ByteReader.TypeRead(parameterTypes[1].FullName, parameters.chunks[1]);
        }

        public static object _Read2(Type[] parameterTypes, ByteWriter parameters)
        {
            return ByteReader.TypeRead(parameterTypes[2].FullName, parameters.chunks[2]);
        }

        public static object _Read3(Type[] parameterTypes, ByteWriter parameters)
        {
            return ByteReader.TypeRead(parameterTypes[3].FullName, parameters.chunks[3]);
        }

        public static object _Read4(Type[] parameterTypes, ByteWriter parameters)
        {
            return ByteReader.TypeRead(parameterTypes[4].FullName, parameters.chunks[4]);
        }









        internal static MethodInfo CopyMethod(MethodBase mtd)
        {
            using var dynamicMethod = new DynamicMethodDefinition(mtd);
            MethodInfo after = dynamicMethod.Generate();

            return after;
        }




        public static void Init()
        {
            /* -------------------------------------------------------------------------- */
            /*                              //Step 1: 定义反射参数
            /* -------------------------------------------------------------------------- */
            //Substep1: 获取类型和标记
            Type voidType = typeof(void);
            Type connType = typeof(NetworkConnection);
            BindingFlags flags = ReflectionTools.BindingFlags_All;

            //Substep2: 获取定义的方法
            var _RemoteCall = typeof(Rpc).GetMethod($"{nameof(Rpc._RemoteCall)}");

            var _StaticWrite0 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticWrite0)}");
            var _StaticWrite1 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticWrite1)}");
            var _StaticWrite2 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticWrite2)}");
            var _StaticWrite3 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticWrite3)}");
            var _StaticWrite4 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticWrite4)}");
            var _StaticWrite5 = typeof(Rpc).GetMethod($"{nameof(Rpc._StaticWrite5)}");

            var _InstanceWrite0 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceWrite0)}");
            var _InstanceWrite1 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceWrite1)}");
            var _InstanceWrite2 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceWrite2)}");
            var _InstanceWrite3 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceWrite3)}");
            var _InstanceWrite4 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceWrite4)}");
            var _InstanceWrite5 = typeof(Rpc).GetMethod($"{nameof(Rpc._InstanceWrite5)}");

            var _Read0 = typeof(Rpc).GetMethod($"{nameof(Rpc._Read0)}");
            var _Read1 = typeof(Rpc).GetMethod($"{nameof(Rpc._Read1)}");
            var _Read2 = typeof(Rpc).GetMethod($"{nameof(Rpc._Read2)}");
            var _Read3 = typeof(Rpc).GetMethod($"{nameof(Rpc._Read3)}");
            var _Read4 = typeof(Rpc).GetMethod($"{nameof(Rpc._Read4)}");


            /* -------------------------------------------------------------------------- */
            /*                         //Step 2: 定义 Expression 参数
            /* -------------------------------------------------------------------------- */
            //Substep 1: 定义传入方法的参数
            ParameterExpression getWriterParam_type = Expression.Parameter(typeof(string), "type");

            ParameterExpression getReaderParam_type = Expression.Parameter(typeof(string), "type");

            ParameterExpression writeParam_type = Expression.Parameter(typeof(string), "type");
            ParameterExpression writeParam_obj = Expression.Parameter(typeof(object), "obj");
            ParameterExpression writeParam_writer = Expression.Parameter(typeof(ByteWriter), "writer");

            ParameterExpression readParam_type = Expression.Parameter(typeof(string), "type");
            ParameterExpression readParam_parameters = Expression.Parameter(typeof(ByteWriter), "parameters");

            ParameterExpression remoteParam_id = Expression.Parameter(typeof(string), "id");
            ParameterExpression remoteParam_caller = Expression.Parameter(typeof(NetworkConnection), "caller");
            ParameterExpression remoteParam_writer = Expression.Parameter(typeof(ByteWriter), "writer");
            ParameterExpression remoteParam_instance = Expression.Parameter(typeof(Entity), "instance");

            ParameterExpression localParam_id = Expression.Parameter(typeof(string), "id");
            ParameterExpression localParam_caller = Expression.Parameter(typeof(NetworkConnection), "caller");
            ParameterExpression localParam_parameters = Expression.Parameter(typeof(ByteWriter), "parameters");
            ParameterExpression localParam_instance = Expression.Parameter(typeof(Entity), "instance");

            //Substep 2: 定义 Switch Cases
            List<SwitchCase> getWriterCases = new();
            List<SwitchCase> getReaderCases = new();
            List<SwitchCase> writeCases = new();
            List<SwitchCase> readCases = new();
            List<SwitchCase> remoteCases = new();
            List<SwitchCase> localCases = new();


            Dictionary<string, MethodInfo> genericWriterMethodTable = new();
            Dictionary<string, MethodInfo> genericReaderMethodTable = new();



            /* -------------------------------------------------------------------------- */
            /*                            //Step 3: 生成明确指定的 ByteConverter
            /* -------------------------------------------------------------------------- */

            Expression WriteParamExpression(Type type, Expression ifFalse)
            {
                return Expression.IfThenElse(
                    Expression.Equal(writeParam_obj, Expression.Constant(null)),                                        //* if (obj == null)
                    Expression.Empty(),                                                                                 //* { }
                    ifFalse                                                                                             //* else
                );
            }

            Expression ReadParamExpression(Expression ifFalse)
            {
                //TODO: Recover;
                return ifFalse;
                // return Expression.Block(
                //     typeof(object), //指定返回类型
                //     Expression.IfThenElse(
                //         Expression.Equal(                                                                                                                    //* if (   
                //             Expression.Field(readParam_parameters, typeof(ByteWriter).GetField(nameof(ByteWriter.bytes))),                                   //*        parameters.bytes ==
                //             Expression.Constant(null)),                                                                                                      //*        null)
                //         Expression.Constant(null),                                                                                                           //*    return null;
                //         ifFalse                                                                                                                              //* else
                //     ),
                //     Expression.Constant(null) // return null
                // );
            }

            //Substep 1: 原生字节转换器: 遍历每个程序集中的方法
            ModFactory.EachUserMethod((ass, type, mtd) =>
            {
                string mtdPath = $"{type.FullName}.{mtd.Name}";

                //TODO: 将检测 [ByteWriterAttribute]method 改为检测 [ByteConvertersAttribute]type
                if (AttributeGetter.TryGetAttribute<ByteWriterAttribute>(mtd, out var writerAttr))
                {
                    foreach (var p in getWriterCases)
                    {
                        if (p.TestValues[0].ToString() == writerAttr.targetType)
                        {
                            //TODO: 添加权重设置 (byte)
                            Debug.LogError($"类型 {p.TestValues[0]} 的 ByteWriter 重复, 将忽略 {mtdPath}");
                            return;
                        }
                    }

                    var tempParameters = mtd.GetParameters();

                    if (tempParameters.Length != 2 || tempParameters[1].ParameterType != typeof(ByteWriter))
                    {
                        Debug.LogError($"ByteWriter {mtdPath} 必须含两种参数: xxx, ByteWriter");
                        return;
                    }

                    var typeToWrite = tempParameters[0].ParameterType;

                    if (typeToWrite.IsGenericType)
                    {
                        genericWriterMethodTable.Add(writerAttr.targetType, mtd);
                    }
                    else
                    {
                        writeCases.Add(
                            Expression.SwitchCase(
                                WriteParamExpression(
                                    typeToWrite,
                                    Expression.Call(null, mtd, Expression.Convert(writeParam_obj, typeToWrite), writeParam_writer)   //* mtd((T)obj, writer);
                                ),
                            Expression.Constant(writerAttr.targetType))
                        );

                        getWriterCases.Add(
                            Expression.SwitchCase(
                                Expression.Constant(mtd, typeof(MethodInfo)),
                                Expression.Constant(writerAttr.targetType)
                            )
                        );
                    }
                    return;
                }
                else if (AttributeGetter.TryGetAttribute<ByteReaderAttribute>(mtd, out var readerAttr))
                {
                    foreach (var p in getReaderCases)
                    {
                        if (p.TestValues[0].ToString() == readerAttr.targetType)
                        {
                            Debug.LogError($"类型 {p.TestValues[0]} 的 ByteReader 重复, 将忽略 {mtdPath}");
                            return;
                        }
                    }

                    var tempParameters = mtd.GetParameters();

                    if (tempParameters.Length != 1 || tempParameters[0].ParameterType != typeof(ByteWriter))
                    {
                        Debug.LogError($"ByteReader {mtdPath} 必须只含一种参数: ByteWriter");
                        return;
                    }

                    //TODO: 不做限制 (这意味着要用Expression手动装箱, 性能可能会变差, 但是方便转换器互相调用, 还可以直接使用 ByteConverterCenterAttribute)
                    if (mtd.ReturnType != typeof(object))
                    {
                        Debug.LogError($"ByteReader {mtdPath} 必须返回 object");
                        return;
                    }

                    readCases.Add(
                        Expression.SwitchCase(
                            ReadParamExpression(
                                Expression.Call(null, mtd, readParam_parameters)             //* mtd(parameters);
                            ),
                        Expression.Constant(readerAttr.targetType))
                    );
                    getReaderCases.Add(
                        Expression.SwitchCase(
                            Expression.Constant(mtd, typeof(MethodInfo)),
                            Expression.Constant(readerAttr.targetType)
                        )
                    );
                    return;
                }
            });

            //Substep 2: 编译 Converter 方法
            void CompileConverterGetMethod()
            {
                //定义方法体 (Switch)
                var getWriterBody = Expression.Switch(getWriterParam_type, Expression.Constant(null, typeof(MethodInfo)), getWriterCases.ToArray());
                var getReaderBody = Expression.Switch(getReaderParam_type, Expression.Constant(null, typeof(MethodInfo)), getReaderCases.ToArray());

                //编译方法
                ByteWriter.GetWriter = Expression.Lambda<Func<string, MethodInfo>>(getWriterBody, "GetWriter_Lambda", new ParameterExpression[] { getWriterParam_type }).Compile();
                ByteReader.GetReader = Expression.Lambda<Func<string, MethodInfo>>(getReaderBody, "GetReader_Lambda", new ParameterExpression[] { getReaderParam_type }).Compile();
            }

            //* Tips: 第一次编译方法
            CompileConverterGetMethod();





            /* -------------------------------------------------------------------------- */
            /*                        //Step 4: 自动字节转换器: 生成
            /* -------------------------------------------------------------------------- */

            List<Type> typesToSolve = new();
            List<Type> typePaths = new();



            #region 方法定义

            string GenerateTypePath()
            {
                string resultPath = "";

                // 遍历目录
                for (int i = 0; i < typePaths.Count; i++)
                {
                    // 获取当前元素的FullName
                    string item = typePaths[i]?.FullName;

                    // 如果是第一个元素，直接赋值给typePath，后面的元素需要加上箭头符号
                    if (i == 0)
                        resultPath = item;
                    else
                        resultPath += $"->{item}";
                }

                return resultPath;
            }

            bool AddTypeToSolveList(Type parentType, Type type)
            {
                typePaths.Add(parentType);
                typePaths.Add(type);

                if (!type.IsValueType)
                {
                    if (type.IsGenericType || type.IsArray)
                    {
                        if (GenericTypeSupport(type, null, null, null, null, null).isSupported)
                        {

                        }
                        else
                        {
                            Debug.LogError($"类型 {GenerateTypePath()} 无法被自动生成, 因为其不存在泛型读写器");
                            return false;
                        }
                    }
                    else
                    {
                        if (type.NoneConstructor())
                        {
                            Debug.LogError($"类型 {GenerateTypePath()} 无法被自动生成, 因为其不包含任何构造函数");
                            return false;
                        }

                        if (type.NoneDefaultConstructor())
                        {
                            Debug.LogError($"类型 {GenerateTypePath()} 无法被自动生成, 因为其不包含默认构造函数");
                            return false;
                        }
                    }
                }


                typesToSolve.Add(type);
                return true;
            }

            void RemoveTypeFromSolveList(Type type)
            {
                typesToSolve.Remove(type);
                CompileConverterGetMethod();
            }

            #endregion



            //Substep 1: 找到所有需要被自动实现的 ByteConverter
            ModFactory.EachUserType((ass, type) =>
            {
                //找到包含 AutoByteConverterAttribute 的类
                if (!AttributeGetter.TryGetAttribute<AutoByteConverterAttribute>(type, out var att))
                    return;

                foreach (var p in getWriterCases)
                {
                    if (p.TestValues[0].ToString() == type.FullName)
                    {
                        Debug.LogError($"类型 {p.TestValues[0]} 已存在 ByteConverter, 不需要添加 {nameof(AutoByteConverterAttribute)} 属性");
                        return;
                    }
                }

                AddTypeToSolveList(null, type);
            });



        //Substep 2: 处理每一个需要的类型
        ReLoop:
            typePaths.Clear();

            if (typesToSolve.Count != 0)
            {
                //Substep 1 --------------------------- : 处理类型 (每次都从最后一个开始) -------------------------- */
                var type = typesToSolve[^1];

                //Substep 2 --------------------------- : 遍历每一个成员, 并添加到代办 -------------------------- */
                var allFields = type.GetFields(ReflectionTools.BindingFlags_All);
                List<FieldInfo> usefulFields = new();

                //要倒序遍历, 否则会出现异常
                foreach (var mem in allFields)
                {
                    //忽略字段: 静态(包括常量) 附带 NonSerializedAttribute/NonNetworkAttribute 的变量
                    if (mem.IsStatic || AttributeGetter.TryGetAttribute<NonSerializedAttribute>(mem, out _) || AttributeGetter.TryGetAttribute<NonNetworkAttribute>(mem, out _))
                        continue;

                    var memberName = mem.Name;
                    var memberType = mem.FieldType;
                    var memberTypeName = memberType.FullName;

                    if (memberType.IsGenericType)
                    {
                        foreach (var item in memberType.GenericTypeArguments)
                        {
                            var memberWriter = ByteWriter.GetWriter(item.FullName);
                            var memberReader = ByteReader.GetReader(item.FullName);

                            if (memberWriter == null || memberReader == null)
                            {
                                //要添加存在判断是有原因的, 例如 Item 和 ItemData 都需要自动实现转换器, 如果不判断, ItemData 的转换器会被多次实现
                                if (typesToSolve.Exists(p => p.FullName == item.FullName))
                                    typesToSolve.Remove(item);

                                //先删再加是为了把类型移到最后, 第一个生成转换器
                                if (AddTypeToSolveList(type, item))
                                    goto ReLoop;
                                else
                                    continue;
                            }
                        }
                    }
                    //TODO: Combine them
                    //* <------------------------------------------------------------------------------------------->
                    //? <------------------------------------------------------------------------------------------->
                    //!                                     注意要保持上下一致
                    //? <------------------------------------------------------------------------------------------->
                    //* <------------------------------------------------------------------------------------------->
                    else
                    {
                        var memberWriter = ByteWriter.GetWriter(memberTypeName);
                        var memberReader = ByteReader.GetReader(memberTypeName);

                        if (memberWriter == null || memberReader == null)
                        {
                            //要添加存在判断是有原因的, 例如 Item 和 ItemData 都需要自动实现转换器, 如果不判断, ItemData 的转换器会被多次实现
                            if (typesToSolve.Exists(p => p.FullName == memberTypeName))
                                typesToSolve.Remove(memberType);

                            //先删再加是为了把类型移到最后, 第一个生成转换器
                            if (AddTypeToSolveList(type, memberType))
                                goto ReLoop;
                            else
                                continue;
                        }
                    }

                    usefulFields.Add(mem);
                }

                //Substep 3 --------------------------- : 如果子类都已经有了读写器, 就开始处理读写器 -------------------------- */
                //首先, 无论还有没有子对象, 都一定需要生成读写方法
                Expression writerBody;
                Expression readerBody;
                Expression objParam;


                //如果没有子字段
                if (usefulFields.Count == 0)
                {
                    writerBody = Expression.Empty();
                    readerBody = Expression.MemberInit(Expression.New(type));
                    objParam = writeParam_obj;
                }
                //如果有子字段
                else
                {
                    ParameterExpression firstWriter = Expression.Variable(typeof(ByteWriter), "firstWriter");
                    List<Expression> writers = new() { Expression.Assign(firstWriter, Expression.Call(writeParam_writer, typeof(ByteWriter).GetMethod(nameof(ByteWriter.WriteNull)))) };      //* ByteWriter firstWriter = writer.WriteNull();
                    List<ParameterExpression> argumentExpressions = new() { firstWriter };
                    var memberBindings = new List<MemberAssignment>();

                    for (int i = 0; i < usefulFields.Count; i++)
                    {
                        var fieldInfo = usefulFields[i];
                        var fieldType = fieldInfo.FieldType;
                        var fieldInstance = Expression.Field(Expression.Convert(writeParam_obj, type), fieldInfo);                                                     //* ((T)obj).xx

                        var writerToRead = Expression.Field(readParam_parameters, typeof(ByteWriter).GetField(nameof(ByteWriter.chunks))).ListItem(i);

                        //TODO: Combine Generic and normal
                        if (fieldType.IsGenericType)
                        {
                            ParameterExpression writerToWrite = Expression.Variable(typeof(ByteWriter), "genericWriter");
                            writers.Add(Expression.Assign(writerToWrite, Expression.Call(firstWriter, typeof(ByteWriter).GetMethod(nameof(ByteWriter.WriteNull)))));      //* ByteWriter genericWriter = firstWriter.WriteNull();
                            argumentExpressions.Add(writerToWrite);

                            var genericArguments = fieldType.GetGenericArguments();

                            //TODO: Generic List
                            var (_, writer, memberBinding) = GenericTypeSupport(fieldType, fieldInfo, fieldInstance, writerToRead, genericArguments, writerToWrite);
                            writers.Add(writer);
                            memberBindings.Add(memberBinding);
                        }
                        else
                        {
                            var writerMethod = ByteWriter.GetWriter(fieldType.FullName);
                            var readerMethod = ByteReader.GetReader(fieldType.FullName);

                            writers.Add(ByteWriter.GetExpressionOfWriting(firstWriter, fieldInstance, fieldType));

                            memberBindings.Add(
                                Expression.Bind(
                                    fieldInfo,
                                    ByteReader.GetExpressionOfReading(writerToRead, fieldType)
                                )
                            );
                        }
                    }

                    writerBody = Expression.Block(argumentExpressions, writers.ToArray());
                    readerBody = Expression.Convert(Expression.MemberInit(Expression.New(type), memberBindings), typeof(object));   //* T obj = new() { xx = * , yy = * };;
                    objParam = Expression.Convert(writeParam_obj, type);
                }

                //Substep 4 --------------------------- : 编译方法 -------------------------- */
                /* --------------------------- case type.FullName: -------------------------- */

                var writerLambda = Expression.Lambda(writerBody, "WriterDelegate_Lambda", new ParameterExpression[] { writeParam_obj, writeParam_writer });
                var readerLambda = Expression.Lambda(readerBody, "ReaderDelegate_Lambda", new ParameterExpression[] { readParam_parameters });

                var writerDelegate = writerLambda.Compile();
                var readerDelegate = readerLambda.Compile();

                var writerDelegateTarget = Expression.Constant(writerDelegate.Target, typeof(System.Runtime.CompilerServices.Closure));
                var readerDelegateTarget = Expression.Constant(readerDelegate.Target, typeof(System.Runtime.CompilerServices.Closure));

                ByteWriter.autoWriterExpressions.Add(type.FullName, writerLambda);
                ByteReader.autoReaderExpressions.Add(type.FullName, readerLambda);

                writeCases.Add(
                    Expression.SwitchCase(
                        WriteParamExpression(
                            type,
                            writerBody   //* writerDelegate(obj, writer); 或 writerDelegate((T)obj, writer);
                        ),
                    Expression.Constant(type.FullName))
                );

                readCases.Add(
                    Expression.SwitchCase(
                        ReadParamExpression(
                            readerBody                   //* readerDelegate(bytes);
                        ),
                    Expression.Constant(type.FullName))
                );

                //     return writer.Method;
                getWriterCases.Add(Expression.SwitchCase(Expression.Constant(writerDelegate.Method, typeof(MethodInfo)), Expression.Constant(type.FullName)));
                //     return reader.Method;
                getReaderCases.Add(Expression.SwitchCase(Expression.Constant(readerDelegate.Method, typeof(MethodInfo)), Expression.Constant(type.FullName)));

                /* -------------------------------- case end -------------------------------- */

                RemoveTypeFromSolveList(type);
                goto ReLoop;
            }

            //Substep 3: 为无法生成的类型报错
            foreach (var set in typesToSolve)
            {
                Debug.LogError($"无法为类型 {set.FullName} 自动生成字节转换器");
            }









            /* -------------------------------------------------------------------------- */
            /*                              //Step 5: 编译读写方法
            /* -------------------------------------------------------------------------- */

            //定义方法体 (Switch)
            var writeBody = Expression.Switch(
                writeParam_type,
                Expression.Block(
                    //* void OnWriterError(string type, ByteWriter writer)
                    typeof(void),

                    //* Debug.LogError($"获取 {type} 的 Writer 失败");
                    Expression.Call(
                        typeof(Debug).GetMethod("LogError", new Type[] { typeof(object) }),
                        Expression.Call(
                            typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object) }),
                            Expression.Constant("获取 {0} 的 Writer 失败"),
                            writeParam_type
                        )
                    ),
                    //* writer.WriteNull();
                    Expression.Call(
                        writeParam_writer,
                        typeof(ByteWriter).GetMethod(nameof(ByteWriter.WriteNull)))
                ),
                writeCases.ToArray()
            );
            var readBody = Expression.Switch(readParam_type,
                Expression.Block(
                    //* void OnReaderError(string type)
                    typeof(object),

                    //* Debug.LogError($"获取 {type} 的 Reader 失败");
                    Expression.Call(
                        typeof(Debug).GetMethod("LogError", new Type[] { typeof(object) }),
                        Expression.Call(
                            typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object) }),
                            Expression.Constant("获取 {0} 的 Reader 失败"),
                            readParam_type
                        )
                    ),
                    //* return null;
                    Expression.Constant(null)
                ),
                readCases.ToArray()
            );

            //编译方法
            ByteWriter.TypeWrite = Expression.Lambda<Action<string, object, ByteWriter>>(writeBody, "Writer_Lambda", new ParameterExpression[] { writeParam_type, writeParam_obj, writeParam_writer }).Compile();
            ByteReader.TypeRead = Expression.Lambda<Func<string, ByteWriter, object>>(readBody, "Reader_Lambda", new ParameterExpression[] { readParam_type, readParam_parameters }).Compile();






            /* -------------------------------------------------------------------------- */
            /*                              //Step 6: 更改带有 RpcBinder 的方法的内容
            /* -------------------------------------------------------------------------- */

            //获取所有可用方法
            ModFactory.EachUserMethod((ass, type, mtd) =>
            {
                string mtdPath = $"{type.FullName}.{mtd.Name}";

                //获取呼叫类型
                if (GetNetCallType(mtd, mtdPath, voidType, connType, out RpcType callType))
                {
                    /* ---------------------------------- 生成常量 ---------------------------------- */
                    var mtdPathConst = Expression.Constant(mtdPath);
                    var mtdInfo = Expression.Constant(new NMRpc(mtdPath, callType));

                    /* ---------------------------------- 获取参数 ---------------------------------- */
                    var totalParameters = mtd.GetParameters();   //分为真正的参数和 NetworkConnection
                    List<Type> trueParameters = new();
                    for (int i = 0; i < totalParameters.Length - 1; i++)
                    {
                        var pa = totalParameters[i];
                        trueParameters.Add(pa.ParameterType);
                    }
                    var parameterListConst = Expression.Constant(trueParameters.ToArray());

                    /* ---------------------------------- 检查参数 ---------------------------------- */
                    foreach (var param in trueParameters)
                    {
                        var writer = ByteWriter.GetWriter(param.FullName);
                        var reader = ByteReader.GetReader(param.FullName);

                        //TODO: 如果没有一个类型的字段包含一个泛型, 这个泛型将不会被生成字节转换器
                        if (writer == null && reader == null)
                        {
                            Debug.LogError($"无法找到类型 {param.FullName} ({mtdPath}) 的字节写入器和字节读取器");
                            return;
                        }
                        if (writer == null)
                        {
                            Debug.LogError($"无法找到类型 {param.FullName} ({mtdPath}) 的字节写入器");
                            return;
                        }
                        if (reader == null)
                        {
                            Debug.LogError($"无法找到类型 {param.FullName} ({mtdPath}) 的字节读取器");
                            return;
                        }
                    }

                    /* ---------------------------------- 编辑方法 ---------------------------------- */
                    MethodInfo mtdCopy = CopyMethod(mtd);
                    Harmony harmony = new($"Rpc.harmony.{mtdPath}");

                    void PatchMethod(MethodInfo patch)
                    {
                        harmony.Patch(mtd, new HarmonyMethod(patch));
                    }

                    if (mtd.IsStatic)
                    {
                        PatchMethod(trueParameters.Count switch
                        {
                            0 => _StaticWrite0,
                            1 => _StaticWrite1,
                            2 => _StaticWrite2,
                            3 => _StaticWrite3,
                            4 => _StaticWrite4,
                            5 => _StaticWrite5,
                            _ => null
                        });

                        /* --------------------------------- 绑定 Case -------------------------------- */
                        //? 以下为重难点
                        //? == 表示等效于
                        //? 认真看其实很好理解


                        //*== case mtdPath:
                        //*==   _RemoteCall(mtdInfo, caller, writer);
                        //*==   break;
                        remoteCases.Add(Expression.SwitchCase(Expression.Call(null, _RemoteCall, mtdInfo, remoteParam_caller, remoteParam_writer, Expression.Constant(null, typeof(Entity))), mtdPathConst));

                        UnaryExpression LocalCaseGeneration(int index)
                        {
                            //*== return ByteReader.TypeRead(parameterTypes[index].FullName, parameters.chunks[index]);
                            return Expression.Convert(
                                        Expression.Call(
                                            ByteReader.TypeRead.Method,
                                            Expression.Constant(ByteReader.TypeRead.Target, typeof(System.Runtime.CompilerServices.Closure)),
                                            Expression.Constant(trueParameters[index].FullName),
                                            Expression.Field(
                                                localParam_parameters,
                                                typeof(ByteWriter).GetField(nameof(ByteWriter.chunks))
                                            ).ListItem(index)
                                        ),
                                        trueParameters[index]
                                    );
                        }

                        switch (trueParameters.Count)
                        {
                            case 0:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            case 1:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        LocalCaseGeneration(0),
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            case 2:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        LocalCaseGeneration(0),
                                        LocalCaseGeneration(1),
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            case 3:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        LocalCaseGeneration(0),
                                        LocalCaseGeneration(1),
                                        LocalCaseGeneration(2),
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            case 4:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        LocalCaseGeneration(0),
                                        LocalCaseGeneration(1),
                                        LocalCaseGeneration(2),
                                        LocalCaseGeneration(3),
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            case 5:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        LocalCaseGeneration(0),
                                        LocalCaseGeneration(1),
                                        LocalCaseGeneration(2),
                                        LocalCaseGeneration(3),
                                        LocalCaseGeneration(4),
                                        localParam_caller
                                    ), mtdPathConst));
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
                            0 => _InstanceWrite0,
                            1 => _InstanceWrite1,
                            2 => _InstanceWrite2,
                            3 => _InstanceWrite3,
                            4 => _InstanceWrite4,
                            5 => _InstanceWrite5,
                            _ => null
                        });

                        /* --------------------------------- 绑定 Case -------------------------------- */
                        //? 以下为重难点
                        //? == 表示等效于
                        //? 认真看其实很好理解


                        //*== case mtdPath:
                        //*==   _RemoteCall(mtdInfo, caller, writer);
                        //*==   break;
                        remoteCases.Add(Expression.SwitchCase(Expression.Call(null, _RemoteCall, mtdInfo, remoteParam_caller, remoteParam_writer, remoteParam_instance), mtdPathConst));

                        UnaryExpression LocalCaseGeneration(int index)
                        {
                            //* return ByteReader.TypeRead(parameterTypes[index].FullName, parameters.chunks[index]);
                            return Expression.Convert(
                                        Expression.Call(
                                            ByteReader.TypeRead.Method,
                                            Expression.Constant(ByteReader.TypeRead.Target, typeof(System.Runtime.CompilerServices.Closure)),
                                            Expression.Constant(trueParameters[index].FullName),
                                            Expression.Field(
                                                localParam_parameters,
                                                typeof(ByteWriter).GetField(nameof(ByteWriter.chunks))
                                            ).ListItem(index)
                                        ),
                                        trueParameters[index]
                                    );
                        }

                        switch (trueParameters.Count)
                        {
                            case 0:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        Expression.Convert(localParam_instance, mtdCopy.GetParameters()[0].ParameterType),
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            case 1:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        Expression.Convert(localParam_instance, mtdCopy.GetParameters()[0].ParameterType),
                                        LocalCaseGeneration(0),
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            case 2:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        Expression.Convert(localParam_instance, mtdCopy.GetParameters()[0].ParameterType),
                                        LocalCaseGeneration(0),
                                        LocalCaseGeneration(1),
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            case 3:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        Expression.Convert(localParam_instance, mtdCopy.GetParameters()[0].ParameterType),
                                        LocalCaseGeneration(0),
                                        LocalCaseGeneration(1),
                                        LocalCaseGeneration(2),
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            case 4:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        Expression.Convert(localParam_instance, mtdCopy.GetParameters()[0].ParameterType),
                                        LocalCaseGeneration(0),
                                        LocalCaseGeneration(1),
                                        LocalCaseGeneration(2),
                                        LocalCaseGeneration(3),
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            case 5:
                                localCases.Add(Expression.SwitchCase(
                                    Expression.Call(
                                        mtdCopy,
                                        Expression.Convert(localParam_instance, mtdCopy.GetParameters()[0].ParameterType),
                                        LocalCaseGeneration(0),
                                        LocalCaseGeneration(1),
                                        LocalCaseGeneration(2),
                                        LocalCaseGeneration(3),
                                        LocalCaseGeneration(4),
                                        localParam_caller
                                    ), mtdPathConst));
                                break;

                            default:
                                Debug.LogError($"方法 {mtdPath} 包含不支持的参数数量");
                                break;
                        }
                    }
                }
            });






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
            Remote = Expression.Lambda<Func<string, NetworkConnection, ByteWriter, Entity, bool>>(remoteBody, "Remote_Lambda", new ParameterExpression[] { remoteParam_id, remoteParam_caller, remoteParam_writer, remoteParam_instance }).Compile();
            LocalMethod = Expression.Lambda<Action<string, NetworkConnection, ByteWriter, Entity>>(localBody, "LocalMethod_Lambda", new ParameterExpression[] { localParam_id, localParam_caller, localParam_parameters, localParam_instance }).Compile();




            SyncPacker.Init();
        }

        public static void LocalCall(string mtdPath, NetworkConnection caller, ByteWriter parameters, uint instance)
        {
            LocalMethod(mtdPath, caller, parameters, Entity.GetEntityByNetId(instance));
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    public class NonNetworkAttribute : Attribute
    {

    }
}