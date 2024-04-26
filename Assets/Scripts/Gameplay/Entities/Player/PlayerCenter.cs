using System;
using System.Collections.Generic;
using GameCore.High;

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

                    float hungerValueDelta = frameTime / 100;
                    if (isMoving) hungerValueDelta += frameTime / 40;
                    player.hungerValue = hungerValue - hungerValueDelta;

                    float happinessValueDelta = frameTime / 25;
                    if (isMoving) happinessValueDelta += frameTime / 10;
                    if (hungerValue <= 30) happinessValueDelta += frameTime / 20;
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