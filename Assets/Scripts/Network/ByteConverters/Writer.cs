// using System;
// using System.Collections.Generic;
// using System.Reflection;

// namespace GameCore
// {
//     public class ByteWriter
//     {
//         public readonly List<byte> bytes;
//         public readonly List<ByteWriterPoint> points;



//         public ByteWriterPoint NewPoint()
//         {

//         }



//         internal ByteWriter(List<byte> bytes, List<ByteWriterPoint> points)
//         {
//             this.bytes = bytes;
//             this.points = points;
//         }










//         public static Action<string, object, ByteWriter> TypeWrite;
//         public static Func<string, MethodInfo> GetWriter;
//         private static Stack<ByteWriter> stack = new();

//         public static ByteWriter Get()
//         {
//             return stack.Count == 0 ? new(new(), new()) : stack.Pop();
//         }

//         public static void Recover(ByteWriter writer)
//         {
//             writer.bytes.Clear();
//             writer.points.Clear();
//             stack.Push(writer);
//         }
//     }

//     public class ByteWriterPoint
//     {
//         public int position;
//         public List<int> internalPositions;

//         public ByteWriterPoint Write(byte value)
//         {
//             ByteWriterPoint point = new(bytes.Count + 1);
//             points.Add(point);
//             bytes.Add(value);
//             return point;
//         }

//         public ByteWriterPoint Write(byte[] value)
//         {
//             ByteWriterPoint point = new(bytes.Count + value.Length);
//             points.Add(point);
//             bytes.AddRange(value);
//             return point;
//         }

//         public ByteWriterPoint WriteNull()
//         {
//             ByteWriterPoint point = new(bytes.Count);
//             points.Add(point);
//             return point;
//         }

//         public ByteWriterPoint()
//         {

//         }

//         public ByteWriterPoint(int position)
//         {
//             this.position = position;
//         }
//     }
// }




using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Mirror;
using SP.Tools;

namespace GameCore
{
    public struct ByteWriter : NetworkMessage
    {
        public byte[] bytes;
        public List<ByteWriter> chunks;





        public readonly ByteWriter Write(byte value)
        {
            ByteWriter chunk = Create();

            chunk.bytes = new byte[1] { value };

            chunks.Add(chunk);
            return chunk;
        }

        public readonly ByteWriter Write(byte[] value)
        {
            ByteWriter chunk = Create();

            chunk.bytes = value;

            chunks.Add(chunk);
            return chunk;
        }

        public readonly ByteWriter WriteNull()
        {
            ByteWriter chunk = Create();


            chunks.Add(chunk);
            return chunk;
        }















        public static Action<string, object, ByteWriter> TypeWrite;
        public static Func<string, MethodInfo> GetWriter;
        public static Dictionary<string, Expression> autoWriterExpressions = new();
        public static Expression GetExpressionOfWriting(Expression writerToWrite, Expression itemToWrite, Type type)
        {
            var writerMethod = GetWriter(type.FullName);

            //调用字节写入器
            return writerMethod.GetParameters().Length == 3 ?          //*== writerMethod((object)((T)obj).xx, writer);
                Expression.Invoke(autoWriterExpressions[type.FullName], itemToWrite.Box(), writerToWrite)
                    :
                Expression.Call(writerMethod, Expression.Convert(itemToWrite, type), writerToWrite);
        }

        //TODO: use it
        private static Stack<ByteWriter> stack = new();

        public static ByteWriter Create()
        {
            var result = new ByteWriter
            {
                chunks = new()
            };
            return result;
        }

        public static ByteWriter CreateNull()
        {
            var result = new ByteWriter();
            return result;
        }
    }
}