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
using System.Reflection;
using Mirror;

namespace GameCore
{
    public struct ByteWriter : NetworkMessage
    {
        public byte[] bytes;
        public List<ByteWriter> chunks;




        // public ByteWriter NewChunk()
        // {
        //     ByteWriter point = Get();
        //     chunks.Add(point);
        //     return point;
        // }

        public ByteWriter Write(byte value)
        {
            ByteWriter chunk = Create();

            chunk.bytes = new byte[1] { value };

            chunks.Add(chunk);
            return chunk;
        }

        public ByteWriter Write(byte[] value)
        {
            ByteWriter chunk = Create();

            chunk.bytes = value;

            chunks.Add(chunk);
            return chunk;
        }

        public ByteWriter WriteNull()
        {
            ByteWriter chunk = Create();


            chunks.Add(chunk);
            return chunk;
        }
















        public static Action<string, object, ByteWriter> TypeWrite;
        public static Func<string, MethodInfo> GetWriter;
        private static Stack<ByteWriter> stack = new();

        public static ByteWriter Create()
        {
            var result = new ByteWriter();
            result.chunks = new();
            return result;
        }

        public static ByteWriter CreateNull()
        {
            var result = new ByteWriter();
            return result;
        }
    }
}