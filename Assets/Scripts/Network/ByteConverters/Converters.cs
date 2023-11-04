using System.Collections.Generic;
using GameCore.Converters;
using Mirror;
using UnityEngine;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;
using SP.Tools;
using Newtonsoft.Json;

namespace GameCore
{
    public static class ByteConverters
    {
        // [ByteWriter("System.Void")]
        // public static void Void_Write(object obj, ByteWriter writer)
        // {

        // }

        // [ByteReader("System.Void")]
        // public static object Void_Read(ByteWriter writer)
        // {
        //     return null;
        // }



        //TODO: Whatever happens, writer.Writer(Null) is always called, so we can call WriteNull in Rpc, then offer the methods the writer that has been created just now
        [ByteWriter("System.Byte")]
        public static void Byte_Write(byte obj, ByteWriter writer)
        {
            writer.Write(obj);
        }

        [ByteReader("System.Byte")]
        public static object Byte_Read(ByteWriter writer)
        {
            return writer.bytes[0];
        }



        [ByteWriter("Mirror.NetworkIdentity")]
        public static void NetworkIdentity_Write(NetworkIdentity obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj.netId));
        }

        [ByteReader("Mirror.NetworkIdentity")]
        public static object NetworkIdentity_Read(ByteWriter writer)
        {
            if (!NetworkServer.spawned.TryGetValue(ByteConverter.ToUInt(writer.bytes), out NetworkIdentity identity))
                Debug.LogError($"网络调用失败, netId 未找到");

            return identity;
        }






        [ByteWriter("GameCore.Entity")]
        public static void Entity_Write(Entity obj, ByteWriter writer)
        {
            writer.Write(obj == null ? ByteConverter.ToBytes(uint.MaxValue) : ByteConverter.ToBytes(obj.netId));
        }

        [ByteReader("GameCore.Entity")]
        public static object Entity_Read(ByteWriter writer)
        {
            return writer.bytes != null ? Entity.GetEntityByNetId(ByteConverter.ToUInt(writer.bytes)) : null;
        }



        [ByteWriter("GameCore.Creature")]
        public static void Creature_Write(Creature obj, ByteWriter writer)
        {
            writer.Write(obj == null ? ByteConverter.ToBytes(uint.MaxValue) : ByteConverter.ToBytes(obj.netId));
        }

        [ByteReader("GameCore.Creature")]
        public static object Creature_Read(ByteWriter writer)
        {
            return writer.bytes != null ? Entity.GetEntityByNetId<Creature>(ByteConverter.ToUInt(writer.bytes)) : null;
        }



        [ByteWriter("GameCore.Player")]
        public static void Player_Write(Player obj, ByteWriter writer)
        {
            writer.Write(obj == null ? ByteConverter.ToBytes(uint.MaxValue) : ByteConverter.ToBytes(obj.netId));
        }

        [ByteReader("GameCore.Player")]
        public static object Player_Read(ByteWriter writer)
        {
            return writer.bytes != null ? Entity.GetEntityByNetId<Player>(ByteConverter.ToUInt(writer.bytes)) : null;
        }



        [ByteWriter("GameCore.Enemy")]
        public static void Enemy_Write(Enemy obj, ByteWriter writer)
        {
            writer.Write(obj == null ? ByteConverter.ToBytes(uint.MaxValue) : ByteConverter.ToBytes(obj.netId));
        }

        [ByteReader("GameCore.Enemy")]
        public static object Enemy_Read(ByteWriter writer)
        {
            return writer.bytes != null ? Entity.GetEntityByNetId<Enemy>(ByteConverter.ToUInt(writer.bytes)) : null;
        }



        [ByteWriter("GameCore.ItemEntity")]
        public static void ItemEntity_Write(Drop obj, ByteWriter writer)
        {
            writer.Write(obj == null ? ByteConverter.ToBytes(uint.MaxValue) : ByteConverter.ToBytes(obj.netId));
        }

        [ByteReader("GameCore.ItemEntity")]
        public static object ItemEntity_Read(ByteWriter writer)
        {
            return writer.bytes != null ? Entity.GetEntityByNetId<Drop>(ByteConverter.ToUInt(writer.bytes)) : null;
        }



        [ByteWriter("GameCore.NPC")]
        public static void NPC_Write(NPC obj, ByteWriter writer)
        {
            writer.Write(obj == null ? ByteConverter.ToBytes(uint.MaxValue) : ByteConverter.ToBytes(obj.netId));
        }

        [ByteReader("GameCore.NPC")]
        public static object NPC_Read(ByteWriter writer)
        {
            return writer.bytes != null ? Entity.GetEntityByNetId<NPC>(ByteConverter.ToUInt(writer.bytes)) : null;
        }



        [ByteWriter("System.Boolean")]
        public static void Boolean_Write(bool obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.Boolean")]
        public static object Boolean_Read(ByteWriter writer)
        {
            return ByteConverter.ToBool(writer.bytes);
        }



        [ByteWriter("System.Int32")]
        public static void Int32_Write(int obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.Int32")]
        public static object Int32_Read(ByteWriter writer)
        {
            return ByteConverter.ToInt(writer.bytes);
        }



        [ByteWriter("System.UInt32")]
        public static void UInt32_Write(uint obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.UInt32")]
        public static object UInt32_Read(ByteWriter writer)
        {
            return ByteConverter.ToUInt(writer.bytes);
        }



        [ByteWriter("System.Single")]
        public static void Single_Write(float obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.Single")]
        public static object Single_Read(ByteWriter writer)
        {
            return ByteConverter.ToFloat(writer.bytes);
        }



        [ByteWriter("System.Double")]
        public static void Double_Write(double obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.Double")]
        public static object Double_Read(ByteWriter writer)
        {
            return ByteConverter.ToDouble(writer.bytes);
        }



        [ByteWriter("System.Int16")]
        public static void Int16_Write(short obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.Int16")]
        public static object Int16_Read(ByteWriter writer)
        {
            return ByteConverter.ToShort(writer.bytes);
        }



        [ByteWriter("System.UInt16")]
        public static void UInt16_Write(ushort obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.UInt16")]
        public static object UInt16_Read(ByteWriter writer)
        {
            return ByteConverter.ToUShort(writer.bytes);
        }



        [ByteWriter("System.Int64")]
        public static void Int64_Write(long obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.Int64")]
        public static object Int64_Read(ByteWriter writer)
        {
            return ByteConverter.ToLong(writer.bytes);
        }



        [ByteWriter("System.UInt64")]
        public static void UInt64_Write(ulong obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.UInt64")]
        public static object UInt64_Read(ByteWriter writer)
        {
            return ByteConverter.ToULong(writer.bytes);
        }



        [ByteWriter("System.String")]
        public static void String_Write(string obj, ByteWriter writer)
        {
            if (string.IsNullOrEmpty(obj))
                writer.WriteNull();
            else
                writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.String")]
        public static object String_Read(ByteWriter writer)
        {
            if (writer.bytes == null || writer.bytes.Length == 0)
                return string.Empty;

            return ByteConverter.ToString(writer.bytes);
        }



        [ByteWriter("System.Char")]
        public static void Char_Write(char obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("System.Char")]
        public static object Char_Read(ByteWriter writer)
        {
            return ByteConverter.ToChar(writer.bytes);
        }



        [ByteWriter("UnityEngine.Vector2")]
        public static void Vector2_Write(Vector2 obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj == null ? Vector2.zero : obj));
        }

        [ByteReader("UnityEngine.Vector2")]
        public static object Vector2_Read(ByteWriter writer)
        {
            return ByteConverter.ToVector2(writer.bytes);
        }



        [ByteWriter("UnityEngine.Vector2Int")]
        public static void Vector2Int_Write(Vector2Int obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj == null ? Vector2Int.zero : obj));
        }

        [ByteReader("UnityEngine.Vector2Int")]
        public static object Vector2Int_Read(ByteWriter writer)
        {
            return ByteConverter.ToVector2Int(writer.bytes);
        }



        [ByteWriter("UnityEngine.Vector3")]
        public static void Vector3_Write(Vector3 obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj == null ? Vector3.zero : obj));
        }

        [ByteReader("UnityEngine.Vector3")]
        public static object Vector3_Read(ByteWriter writer)
        {
            return ByteConverter.ToVector3(writer.bytes);
        }



        [ByteWriter("UnityEngine.Vector3Int")]
        public static void Vector3Int_Write(Vector3Int obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj == null ? Vector3Int.zero : obj));
        }

        [ByteReader("UnityEngine.Vector3Int")]
        public static object Vector3Int_Read(ByteWriter writer)
        {
            return ByteConverter.ToVector3Int(writer.bytes);
        }



        [ByteWriter("UnityEngine.AudioClip")]
        public static void AudioClip_Write(AudioClip obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("UnityEngine.AudioClip")]
        public static object AudioClip_Read(ByteWriter writer)
        {
            return ByteConverter.ToAudioClip(writer.bytes);
        }



        [ByteWriter("UnityEngine.Sprite")]
        public static void Sprite_Write(Sprite sprite, ByteWriter writer)
        {
            List<byte> ts = new();

            ts.AddRange(ByteConverter.ToBytes(sprite.pixelsPerUnit));
            ts.AddRange(ByteConverter.ToBytes(sprite));

            writer.Write(ts.ToArray());
        }

        [ByteReader("UnityEngine.Sprite")]
        public static object Sprite_Read(ByteWriter writer)
        {
            return ByteConverter.ToSprite(new ArraySegment<byte>(writer.bytes, 4, writer.bytes.Length - 4).ToArray(), FilterMode.Point, ByteConverter.ToFloat(new ArraySegment<byte>(writer.bytes, 0, 4).ToArray()), TextureFormat.RGBA32);
        }



        [ByteWriter("UnityEngine.Texture2D")]
        public static void Texture2D_Write(Texture2D obj, ByteWriter writer)
        {
            writer.Write(ByteConverter.ToBytes(obj));
        }

        [ByteReader("UnityEngine.Texture2D")]
        public static object Texture2D_Read(ByteWriter writer)
        {
            return ByteConverter.ToTexture2D(writer.bytes);
        }



        [ByteWriter("Newtonsoft.Json.Linq.JObject")]
        public static void JObject_Write(JObject jo, ByteWriter writer)
        {
            String_Write(Compressor.CompressString(jo?.ToString(Formatting.None)), writer);
        }

        [ByteReader("Newtonsoft.Json.Linq.JObject")]
        public static object JObject_Read(ByteWriter writer)
        {
            string str = Compressor.DecompressString((string)String_Read(writer));

            if (string.IsNullOrEmpty(str))
                return null;
            else
                return JsonTools.LoadJObjectByString(str);
        }



        [ByteWriter("GameCore.ItemData")]
        public static void ItemData_Write(ItemData item, ByteWriter writer)
        {
            if (ItemData.Null(item))
                writer.WriteNull();
            else
                String_Write(item.id, writer);
        }

        [ByteReader("GameCore.ItemData")]
        public static object ItemData_Read(ByteWriter writer)
        {
            string str = (string)String_Read(writer);

            if (string.IsNullOrEmpty(str))
                return null;
            else
                return ModFactory.CompareItem(str);
        }



        [ByteWriter("GameCore.Item")]
        public static void Item_Write(Item item, ByteWriter writer)
        {
            if (Item.Null(item))
            {
                Debug.Log("It is null! I Write");
                writer.WriteNull();
            }
            else
            {
                var kid = writer.Write(ByteConverter.ToBytes(item.data.id));

                UInt16_Write(item.count, kid);
                JObject_Write(item.customData, kid);
            }
        }

        [ByteReader("GameCore.Item")]
        public static object Item_Read(ByteWriter writer)
        {
            if (writer.bytes == null)
            {
                Debug.Log("It is null! I Read");
                return null;
            }
            else
            {
                Item item = ModFactory.CompareItem(ByteConverter.ToString(writer.bytes)).ToExtended();

                //TODO: Fix Errors
                item.count = (ushort)UInt16_Read(writer.chunks[0]);
                item.customData = (JObject)JObject_Read(writer.chunks[1]);

                return item;
            }
        }



        [ByteWriter("GameCore.BlockLayer")]
        public static void BlockLayer_Write(BlockLayer obj, ByteWriter writer)
        {
            writer.Write((byte)obj);
        }

        [ByteReader("GameCore.BlockLayer")]
        public static object BlockLayer_Read(ByteWriter writer)
        {
            return (BlockLayer)writer.bytes[0];
        }
    }
}