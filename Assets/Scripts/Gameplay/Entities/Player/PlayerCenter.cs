using System;
using System.Collections.Generic;
using GameCore.High;
using GameCore.Network;

namespace GameCore
{
    public static class PlayerCenter
    {
        public static List<Player> all = new();
        public static Action<Player> OnAddPlayer = _ => { };
        public static Action<Player> OnRemovePlayer = _ => { };
        public static float playerHealthUpTimer;
        public static float playerHungerHurtTimer;

        public static void AddPlayer(Player player)
        {
            all.Add(player);
            OnAddPlayer(player);
        }

        public static void RemovePlayer(Player player)
        {
            all.Remove(player);
            OnRemovePlayer(player);
        }

        public static void Update()
        {
            if (Server.isServer)
            {
                var frameTime = Performance.frameTime;

                foreach (var player in all)
                {
                    if (player.isDead)
                        continue;

                    bool isMoving = player.isMoving;
                    float hungerValue = player.hungerValue;
                    float happinessValue = player.happinessValue;
                    int health = player.health;

                    float hungerValueDelta = frameTime * 0.01f;
                    if (isMoving) hungerValueDelta += frameTime * 0.025f;
                    player.hungerValue = hungerValue - hungerValueDelta;

                    float happinessValueDelta = frameTime * 0.02f;
                    if (isMoving) happinessValueDelta += frameTime * 0.001f;
                    if (hungerValue <= 30) happinessValueDelta += frameTime * 0.03f;
                    player.happinessValue = happinessValue - happinessValueDelta;

                    //一秒回一次血
                    if (Tools.time >= playerHealthUpTimer)
                    {
                        //受伤的八秒内不回血
                        if (health < 100 && Tools.time > player.previousHurtTime + 8)
                        {
                            playerHealthUpTimer = Tools.time + 1f;
                            player.health = health + 1;
                        }
                    }

                    //每三秒扣一次血
                    if (Tools.time >= playerHungerHurtTimer)
                    {
                        playerHungerHurtTimer = Tools.time + 5;

                        if (hungerValue <= 0)
                            player.TakeDamage(5);
                    }
                }
            }
        }
    }
}