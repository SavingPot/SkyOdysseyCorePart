using UnityEngine;
using System;
using SP.Tools;

namespace GameCore.Converters
{
    public static class ByteConverter
    {
        public static byte[] ToBytes(bool value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(int value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(uint value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(float value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(double value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(short value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(ushort value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(long value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(ulong value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(string value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(char value) => SystemByteConvert.ToBytes(value);

        public static byte[] ToBytes(Vector2 value)
        {
            byte[] buff = new byte[sizeof(float) * 2];
            Buffer.BlockCopy(ToBytes(value.x), 0, buff, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(ToBytes(value.y), 0, buff, 1 * sizeof(float), sizeof(float));
            return buff;
        }

        public static byte[] ToBytes(Vector2Int value)
        {
            byte[] buff = new byte[sizeof(int) * 2];
            Buffer.BlockCopy(ToBytes(value.x), 0, buff, 0 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(ToBytes(value.y), 0, buff, 1 * sizeof(int), sizeof(int));
            return buff;
        }

        public static byte[] ToBytes(Vector3 value)
        {
            byte[] buff = new byte[sizeof(float) * 3];
            Buffer.BlockCopy(ToBytes(value.x), 0, buff, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(ToBytes(value.y), 0, buff, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(ToBytes(value.z), 0, buff, 2 * sizeof(float), sizeof(float));
            return buff;
        }

        public static byte[] ToBytes(Vector3Int value)
        {
            byte[] buff = new byte[sizeof(int) * 3];
            Buffer.BlockCopy(ToBytes(value.x), 0, buff, 0 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(ToBytes(value.y), 0, buff, 1 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(ToBytes(value.z), 0, buff, 2 * sizeof(int), sizeof(int));
            return buff;
        }

        public static byte[] ToBytes(Vector4 value)
        {
            byte[] buff = new byte[sizeof(float) * 4];
            Buffer.BlockCopy(ToBytes(value.x), 0, buff, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(ToBytes(value.y), 0, buff, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(ToBytes(value.z), 0, buff, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(ToBytes(value.w), 0, buff, 3 * sizeof(float), sizeof(float));
            return buff;
        }

        public static byte[] ToBytes(AudioClip audioClip)
        {
            float[] samples = new float[audioClip.samples];

            audioClip.GetData(samples, 0);

            short[] intData = new short[samples.Length];

            byte[] bytesData = new byte[samples.Length * 2];

            int rescaleFactor = 32767;

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = ToBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            return bytesData;
        }

        //TODO: Make it full sprite, not just texture!
        public static byte[] ToBytes(Sprite sprite) => ToBytes(sprite.texture);

        public static byte[] ToBytes(Texture2D texture) => texture.EncodeToPNG();



        public static bool ToBool(byte[] bytes) => SystemByteConvert.ToBool(bytes);

        public static int ToInt(byte[] bytes) => SystemByteConvert.ToInt(bytes);

        public static uint ToUInt(byte[] bytes) => SystemByteConvert.ToUInt(bytes);

        public static float ToFloat(byte[] bytes) => SystemByteConvert.ToFloat(bytes);

        public static double ToDouble(byte[] bytes) => SystemByteConvert.ToDouble(bytes);

        public static short ToShort(byte[] bytes) => SystemByteConvert.ToShort(bytes);

        public static ushort ToUShort(byte[] bytes) => SystemByteConvert.ToUShort(bytes);

        public static long ToLong(byte[] bytes) => SystemByteConvert.ToLong(bytes);

        public static ulong ToULong(byte[] bytes) => SystemByteConvert.ToULong(bytes);

        public static string ToString(byte[] bytes) => SystemByteConvert.ToString(bytes);

        public static char ToChar(byte[] bytes) => SystemByteConvert.ToChar(bytes);

        public static Vector2 ToVector2(byte[] bytes)
        {
            Vector2 vec = Vector2.zero;
            vec.x = BitConverter.ToSingle(bytes, 0 * sizeof(float));
            vec.y = BitConverter.ToSingle(bytes, 1 * sizeof(float));
            return vec;
        }

        public static Vector2Int ToVector2Int(byte[] bytes)
        {
            Vector2Int vec = Vector2Int.zero;
            vec.x = BitConverter.ToInt32(bytes, 0 * sizeof(int));
            vec.y = BitConverter.ToInt32(bytes, 1 * sizeof(int));
            return vec;
        }

        public static Vector3 ToVector3(byte[] bytes)
        {
            Vector3 vec = Vector3.zero;
            vec.x = BitConverter.ToSingle(bytes, 0 * sizeof(float));
            vec.y = BitConverter.ToSingle(bytes, 1 * sizeof(float));
            vec.z = BitConverter.ToSingle(bytes, 2 * sizeof(float));
            return vec;
        }

        public static Vector3Int ToVector3Int(byte[] bytes)
        {
            Vector3Int vec = Vector3Int.zero;
            vec.x = BitConverter.ToInt32(bytes, 0 * sizeof(int));
            vec.y = BitConverter.ToInt32(bytes, 1 * sizeof(int));
            vec.z = BitConverter.ToInt32(bytes, 2 * sizeof(int));
            return vec;
        }

        public static Vector4 ToVector4(byte[] bytes)
        {
            Vector4 vec = Vector4.zero;
            vec.x = BitConverter.ToSingle(bytes, 0 * sizeof(float));
            vec.y = BitConverter.ToSingle(bytes, 1 * sizeof(float));
            vec.z = BitConverter.ToSingle(bytes, 2 * sizeof(float));
            vec.w = BitConverter.ToSingle(bytes, 3 * sizeof(float));
            return vec;
        }

        public static AudioClip ToAudioClip(byte[] bytes, string clipName = "new_clip")
        {
            lock (bytes)
            {
                float[] samples = new float[bytes.Length / 2];
                float rescaleFactor = 32767;

                for (int i = 0; i < bytes.Length; i += 2)
                {
                    short st = BitConverter.ToInt16(bytes, i);
                    float ft = st / rescaleFactor;
                    samples[i / 2] = ft;
                }

                AudioClip audioClip = AudioClip.Create(clipName, samples.Length, 1, 44100, false);
                audioClip.SetData(samples, 0);

                return audioClip;
            }
        }

        public static Texture2D ToTexture2D(byte[] bytes, FilterMode filterMode = FilterMode.Point, TextureFormat format = TextureFormat.RGBA32)
        {
            Texture2D texture2D = new(1, 1, format, false);
            texture2D.LoadImage(bytes);
            texture2D.filterMode = filterMode;

            return texture2D;
        }

        /// <summary>
        /// 使用 IO 流加载图片，并将图片转换成 Sprite 类型返回
        /// </summary>
        /// <returns></returns>
        public static Sprite ToSprite(byte[] bytes, FilterMode filterMode = FilterMode.Point, float pixelsPerUnit = 0, TextureFormat format = TextureFormat.RGBA32)
        {
            Texture2D texture2D = ToTexture2D(bytes, filterMode, format);

            Sprite sprite = Sprite.Create(texture2D, new(0, 0, texture2D.width, texture2D.height), new(0.5f, 0.5f),
                            pixelsPerUnit <= 0 ? ((texture2D.width + texture2D.height) / 2) : pixelsPerUnit);

            return sprite;
        }
    }
}
