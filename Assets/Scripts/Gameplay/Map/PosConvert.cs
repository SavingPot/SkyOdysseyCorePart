using System;
using GameCore.High;
using UnityEngine;

namespace GameCore
{
    public static class PosConvert
    {
        public static Vector2Int WorldToMapPos(Vector2 vec)
        {
            return new((int)Math.Round(vec.x), (int)Math.Round(vec.y));
        }

        public static Vector2Int MapPosToChunkIndex(Vector2Int pos)
        {
            //0, x, 3x, 5x, 7x, 9x
            //0, 0.5x, 1.5x, 2.5x, 3.5x, 4.5x
            //区块中心点：-15, -13, -11, -9, -7, -5, -3, -1, 0, 1, 3, 5, 7, 9, 11, 13, 15
            //排除初始区块的影响 (初始区块只占了一半)
            float xDelta = pos.x > 0 ? Chunk.halfBlockCountPerAxis : Chunk.negativeHalfBlockCountPerAxis;
            float yDelta = pos.y > 0 ? Chunk.halfBlockCountPerAxis : Chunk.negativeHalfBlockCountPerAxis;
            return new((int)((pos.x + xDelta) * Chunk.blockCountPerAxisReciprocal), (int)((pos.y + yDelta) * Chunk.blockCountPerAxisReciprocal));
        }

        public static Vector2Int ChunkToRegionIndex(Vector2Int index)
        {
            //排除初始区域的影响 (初始区域只占了一半)
            float xDelta = index.x > 0 ? Region.halfChunkCount : Region.negativeHalfChunkCount;
            float yDelta = index.y > 0 ? Region.halfChunkCount : Region.negativeHalfChunkCount;

            return new((int)((index.x + xDelta) * Region.chunkCountReciprocal), (int)((index.y + yDelta) * Region.chunkCountReciprocal));
        }

        public static Vector2Int WorldPosToChunkIndex(Vector2 pos)
        {
            return MapPosToChunkIndex(new((int)pos.x, (int)pos.y));
        }

        public static Vector2Int WorldPosToRegionIndex(Vector2 pos)
        {
            return ChunkToRegionIndex(MapPosToChunkIndex(new((int)pos.x, (int)pos.y)));
        }

        public static Vector2Int MapToRegionPos(this Chunk chunk, Vector2Int mapPos)
        {
            return new(mapPos.x - chunk.regionMiddleX, mapPos.y - chunk.regionMiddleY);
        }

        public static Vector2Int MapToRegionPos(Vector2Int mapPos, Vector2Int regionIndex)
        {
            return new(mapPos.x - Region.GetMiddleX(regionIndex), mapPos.y - Region.GetMiddleY(regionIndex));
        }
    }
}