using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class Lord
    {
        //! 记得更新构造函数
        public string id;
        public Vector2Int manorIndex;
        public List<Vector2Int> ownedTerritories;
        public virtual int coins { get; set; }
        public LaborData laborData;

        /// <summary>
        /// TODO: 比如说如果庄主是玩家，就需要在更改了 Lord.coins 之后调用这个方法，在服务器同步硬币（PlayerAddCoins）
        /// </summary>
        public virtual void ApplyLordDataBack()
        {

        }

        public void WriteData(Lord lord)
        {
            id = lord.id;
            manorIndex = lord.manorIndex;
            ownedTerritories = lord.ownedTerritories;
            coins = lord.coins;
            laborData = lord.laborData;
        }

        public void WriteData(string id, Vector2Int manorIndex, List<Vector2Int> ownedTerritories, int coins, LaborData laborData)
        {
            this.id = id;
            this.manorIndex = manorIndex;
            this.ownedTerritories = ownedTerritories;
            this.coins = coins;
            this.laborData = laborData;
        }
    }

    public sealed class PlayerLord : Lord
    {
        public PlayerSave playerSave;
        public override int coins { get => playerSave.coin; set => playerSave.coin = value; }
        //TODO：未给玩家执行ServerAddCoins或是Refresh之类的，这样必须得重进游戏才会刷新

        public PlayerLord(PlayerSave playerSave)
        {
            this.playerSave = playerSave;
        }
    }
}