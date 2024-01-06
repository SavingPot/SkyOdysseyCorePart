using System.Collections.Generic;
using GameCore.Converters;
using Mirror;
using UnityEngine;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;
using SP.Tools;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.IO;

namespace GameCore
{
    public class SerializationSurrogatesClassAttribute : Attribute
    {

    }

    public interface ISerializationSurrogate<T> : ISerializationSurrogate
    {

    }

    [SerializationSurrogatesClass]
    public static class SerializationSurrogates
    {
        public class NetworkIdentitySurrogate : ISerializationSurrogate<NetworkIdentity>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                var identity = (NetworkIdentity)obj;

                info.AddValue("netId", !identity ? uint.MaxValue : identity.netId);
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var netId = info.GetUInt32("netId");

                if (!NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity))
                    Debug.LogError($"将 netId 读取为 NetworkIdentity 时失败, 未找到 {netId}");

                return identity;
            }
        }





        public class EntitySurrogate : ISerializationSurrogate<Entity>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                var entity = (Entity)obj;
                info.AddValue("netId", entity.netId);
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var netId = info.GetUInt32("netId");

                return Entity.GetEntityByNetId(netId);
            }
        }





        public class CreatureSurrogate : ISerializationSurrogate<Creature>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                var entity = (Creature)obj;
                info.AddValue("netId", entity.netId);
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var netId = info.GetUInt32("netId");

                return Entity.GetEntityByNetId<Creature>(netId);
            }
        }





        public class PlayerSurrogate : ISerializationSurrogate<Player>
        {
            //TODO: 貌似所有的实体都无法被正常的网络传输
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                var entity = (Player)obj;
                Debug.Log($"Write {entity?.netId}");
                info.AddValue("netId", entity.netId);
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var netId = info.GetUInt32("netId");

                Debug.Log($"Read {netId}");
                return Entity.GetEntityByNetId<Player>(netId);
            }
        }





        public class EnemySurrogate : ISerializationSurrogate<Enemy>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                var entity = (Enemy)obj;
                info.AddValue("netId", entity.netId);
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var netId = info.GetUInt32("netId");

                return Entity.GetEntityByNetId<Enemy>(netId);
            }
        }





        public class DropSurrogate : ISerializationSurrogate<Drop>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                var entity = (Drop)obj;
                info.AddValue("netId", entity.netId);
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var netId = info.GetUInt32("netId");

                return Entity.GetEntityByNetId<Drop>(netId);
            }
        }





        public class NPCSurrogate : ISerializationSurrogate<NPC>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                var entity = (NPC)obj;
                info.AddValue("netId", entity.netId);
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var netId = info.GetUInt32("netId");

                return Entity.GetEntityByNetId<NPC>(netId);
            }
        }





        public class Vector2Surrogate : ISerializationSurrogate<Vector2>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                Vector2 vec = (Vector2)obj;
                info.AddValue("vec", ByteConverter.ToBytes(vec == null ? Vector2.zero : vec));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var vec = (byte[])info.GetValue("vec", typeof(byte[]));

                return ByteConverter.ToVector2(vec);
            }
        }





        public class Vector2IntSurrogate : ISerializationSurrogate<Vector2Int>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                Vector2Int vec = (Vector2Int)obj;
                info.AddValue("vec", ByteConverter.ToBytes(vec == null ? Vector2Int.zero : vec));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var vec = (byte[])info.GetValue("vec", typeof(byte[]));

                return ByteConverter.ToVector2Int(vec);
            }
        }





        public class Vector3Surrogate : ISerializationSurrogate<Vector3>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                Vector3 vec = (Vector3)obj;
                info.AddValue("vec", ByteConverter.ToBytes(vec == null ? Vector3.zero : vec));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var vec = (byte[])info.GetValue("vec", typeof(byte[]));

                return ByteConverter.ToVector3(vec);
            }
        }





        public class Vector3IntSurrogate : ISerializationSurrogate<Vector3Int>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                Vector3Int vec = (Vector3Int)obj;
                info.AddValue("vec", ByteConverter.ToBytes(vec == null ? Vector3Int.zero : vec));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var vec = (byte[])info.GetValue("vec", typeof(byte[]));

                return ByteConverter.ToVector3Int(vec);
            }
        }





        public class Vector4Surrogate : ISerializationSurrogate<Vector4>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                Vector4 vec = (Vector4)obj;
                info.AddValue("vec", ByteConverter.ToBytes(vec == null ? Vector4.zero : vec));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var vec = (byte[])info.GetValue("vec", typeof(byte[]));

                return ByteConverter.ToVector4(vec);
            }
        }





        public class BlockSave_LocationSurrogate : ISerializationSurrogate<BlockSave_Location>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                BlockSave_Location location = (BlockSave_Location)obj;
                info.AddValue("x", location.pos.x);
                info.AddValue("y", location.pos.y);
                info.AddValue("bg", location.isBackground);
                info.AddValue("cd", location.customData == null ? null : ByteConverter.ToBytes(location.customData.ToString()));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var x = (int)info.GetValue("x", typeof(int));
                var y = (int)info.GetValue("y", typeof(int));
                var bg = (bool)info.GetValue("bg", typeof(bool));
                var cd = (byte[])info.GetValue("cd", typeof(byte[]));

                return new BlockSave_Location(new(x, y), bg, cd == null ? null : ByteConverter.ToString(cd));
            }
        }





        public class AudioClipSurrogate : ISerializationSurrogate<AudioClip>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                info.AddValue("clip", ByteConverter.ToBytes((AudioClip)obj));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var clip = (byte[])info.GetValue("clip", typeof(byte[]));

                return ByteConverter.ToAudioClip(clip);
            }
        }





        public class SpriteSurrogate : ISerializationSurrogate<Sprite>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                var sprite = (Sprite)obj;
                info.AddValue("pixelsPerUnit", sprite.pixelsPerUnit);
                info.AddValue("texture", ByteConverter.ToBytes(sprite.texture));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var pixelsPerUnit = info.GetSingle("pixelsPerUnit");
                var texture = (byte[])info.GetValue("texture", typeof(byte[]));

                return ByteConverter.ToSprite(
                    texture,
                    FilterMode.Point,
                    pixelsPerUnit,
                    TextureFormat.RGBA32
                );
            }
        }





        public class Texture2DSurrogate : ISerializationSurrogate<Texture2D>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                info.AddValue("texture", ByteConverter.ToBytes((Texture2D)obj));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var textureData = (byte[])info.GetValue("texture", typeof(byte[]));

                return ByteConverter.ToTexture2D(textureData);
            }
        }





        public class JObjectSurrogate : ISerializationSurrogate<JObject>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                info.AddValue("str", Compressor.CompressString(((JObject)obj)?.ToString(Formatting.None)));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var str = (string)info.GetValue("str", typeof(string));

                if (string.IsNullOrEmpty(str))
                {
                    return null;
                }
                else
                {
                    return JsonTools.LoadJObjectByString(Compressor.DecompressString(str));
                }
            }
        }





        public class JTokenSurrogate : ISerializationSurrogate<JToken>
        {
            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                info.AddValue("str", Compressor.CompressString(((JToken)obj)?.ToString(Formatting.None)));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var str = (string)info.GetValue("str", typeof(string));

                if (string.IsNullOrEmpty(str))
                {
                    return null;
                }
                else
                {
                    return JsonTools.LoadJTokenByString(Compressor.DecompressString(str));
                }
            }
        }
    }
}