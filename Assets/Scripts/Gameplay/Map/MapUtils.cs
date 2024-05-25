using Org.BouncyCastle.Math.EC.Rfc7748;
using UnityEngine;

namespace GameCore
{
    public static class MapUtils
    {
        public static bool IsConstructionWall(Vector2Int pos, bool isBackground)
            => Map.instance.TryGetBlock(pos, isBackground, out var block) && block.data.IsConstructionWall();





        /// <summary>
        /// 该类不被可复用
        /// </summary>
        public class RoomCheck
        {
            public enum EnclosingState
            {
                Passed,
                TooSmall,
                TooLarge,
                DetectedCollidibleBlock,
                WallHoleTooLarge
            }

            const int minRoomBlockCount = 22;
            const int maxRoomBlockCount = 200;

            (int x, int y, Block wallBlock)[] blocksInConstruction;
            int totalBlockCount;
            Vector2Int pos;

            int minX;
            int maxX;
            int minY;
            int maxY;



            public RoomCheck(Vector2Int pos)
            {
                //初始化
                totalBlockCount = 0;
                this.pos = pos;

                minX = pos.x;
                maxX = pos.x;
                minY = pos.y;
                maxY = pos.y;
            }


            public bool IsValidConstruction()
            {
                return IsEnclosedConstruction() == EnclosingState.Passed && IsSizeSuitable();
            }

            /// <summary>
            /// 这个方法必须被最先调用，因为它会初始化一些变量
            /// </summary>
            /// <returns></returns>
            public EnclosingState IsEnclosedConstruction()
            {
                blocksInConstruction = new (int, int, Block)[maxRoomBlockCount];



                //如果检测的是一个固体方块，则直接返回false（应该是检测房间内空气）
                var blockAtPoint = Map.instance[pos, false];
                if (blockAtPoint != null && blockAtPoint.data.collidible)
                {
                    Debug.LogError("检测的位置是固体方块");
                    return EnclosingState.DetectedCollidibleBlock;
                }




                EnclosingState enclosingState = EnclosingState.Passed;
                CheckIfIsEnclosedConstruction(pos.x, pos.y, ref enclosingState);

                Debug.Log(enclosingState);
                return enclosingState;
            }

            public bool IsSizeSuitable()
            {
                if (IsSizeTooLarge())
                {
                    Debug.LogError("房间过大");
                    return false;
                }

                //检测房间是否过小（空气也会计入方块总数）
                if (isSizeTooSmall())
                {
                    Debug.LogError($"房间过小，只有 {totalBlockCount}/{minRoomBlockCount} 个方块");
                    return false;
                }

                return true;
            }

            //TODO
            public int ScoreRoom()
            {
                if (blocksInConstruction == null)
                    throw new System.InvalidOperationException("请先调用 IsEnclosedConstruction 方法");

                int score = 0;

                //TODO: 检查家具
                foreach (var (x, y, wallBlock) in blocksInConstruction)
                {
                    if ((wallBlock != null && wallBlock.data.IsFurniture()) ||
                        (Map.instance.TryGetBlock(new(x, y), true, out var backgroundBlock) && backgroundBlock.data.IsFurniture()))
                    {
                        score++;
                    }
                }

                return score;
            }

            public bool IsSizeTooLarge() => totalBlockCount >= maxRoomBlockCount;

            public bool isSizeTooSmall() => totalBlockCount < minRoomBlockCount;

            /// <summary>
            /// 这个方法会更改 numRoomTiles
            /// 这是一个递归函数，每一个方块都会被检测
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            //TODO: 更改 CheckRoom 的逻辑
            public void CheckIfIsEnclosedConstruction(int x, int y, ref EnclosingState enclosingState)
            {
                //如果已经检测到无效了就返回
                if (enclosingState != EnclosingState.Passed)
                    return;

                //如果已经检测过了这个方块就返回，不重复检测
                foreach (var pos in blocksInConstruction)
                    if (pos.x == x && pos.y == y)
                        return;

                //刷新当前
                var wallBlockAtThis = Map.instance[x, y, false];
                blocksInConstruction[totalBlockCount] = (x, y, wallBlockAtThis);

                //如果此地已经有可碰撞方块了，则该坐标成功了，达到封闭的条件
                if (wallBlockAtThis != null && wallBlockAtThis.data.collidible)
                {
                    return;
                }
                else
                {
                    //累计房间方块数（仅累计不会被碰撞的方块，包括空气和无法碰撞的墙体）
                    totalBlockCount++;

                    //检测房间是否过大
                    if (IsSizeTooLarge())
                    {
                        enclosingState = EnclosingState.TooLarge;
                        return;
                    }
                }

                //找到房间的边界
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                // 十字向遍历以当前方块为中心 5 个方块
                // x x o x x
                // x x o x x
                // o o O o 0
                // x x o x x
                // x x o x x
                // 遍历 5 格直径而不是 3 格是因为如果是三格的话，只要墙体有一个洞就不能作为房子了，这太严苛了
                bool hasBlockOnX = false;
                bool hasBlockOnY = false;
                for (int offset = -2; offset <= 2; offset++)
                {
                    Vector2Int xBlock = new(x + offset, y);
                    Vector2Int yBlock = new(x, y + offset);

                    //如果有背景墙，或者是房子在这个方向上结束了，就算成功
                    if (IsConstructionWall(xBlock, true) || IsConstructionWall(xBlock, false))
                        hasBlockOnX = true;
                    if (IsConstructionWall(yBlock, true) || IsConstructionWall(yBlock, false))
                        hasBlockOnY = true;
                }
                //检测房屋背景墙缺口是否过大（比如2x2）
                if (!hasBlockOnX || !hasBlockOnY)
                {
                    enclosingState = EnclosingState.WallHoleTooLarge;
                    Debug.LogError($"房屋缺口过大 ({x}, {y})", Map.instance.GetBlock(new(x, y), false)?.gameObject);
                    return;
                }

                //继续递归检查周围的 3x3 个方块（不包括自己，即使这里不跳过，在 CheckRoom 的开头也会被跳过）
                for (int newX = x - 1; newX <= x + 1; newX++)
                {
                    for (int newY = y - 1; newY <= y + 1; newY++)
                    {
                        if ((newX != x || newY != y) && enclosingState == EnclosingState.Passed)
                            CheckIfIsEnclosedConstruction(newX, newY, ref enclosingState);
                    }
                }
            }
        }
    }
}