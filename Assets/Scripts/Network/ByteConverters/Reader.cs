using System;
using System.Collections.Generic;
using System.Reflection;

namespace GameCore
{
    public class ByteReader
    {
        //public byte[] bytes;

        public static Func<string, ByteWriter, object> TypeRead;
        public static Func<string, MethodInfo> GetReader;

        // public byte[] Read(byte value)
        // {
        //     ByteWriterPoint point = new(bytes.Count + 1);
        //     points.Add(point);
        //     bytes.Add(value);
        //     return point;
        // }

        // public ByteReader(byte[] bytes, ByteWriterPoint[] points, int index)
        // {
        //     this.bytes = index switch
        //     {
        //         0 => new ArraySegment<byte>(bytes, 0, points[0].position).ToArray(),
        //         1 => new ArraySegment<byte>(bytes, points[0].position, points[1].position - points[0].position).ToArray(),
        //         2=>new ArraySegment<byte>(bytes, points[1].position, points[2].position - points[1].position).ToArray()
        //     };
        // }
    }
}