using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace GameCore
{
    public class WeatherParticleSystem : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _system;
        public ParticleSystem system => _system;


        private void OnParticleTrigger()
        {
            if (GWeather.weatherId != WeatherID.AcidRain)
                return;

            //获取所有进入了触发器的粒子
            List<Particle> enter = new();
            int numEnter = system.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enter);

            //检测是否碰到实体
            for (int i = 0; i < numEnter; i++)
            {
                Particle p = enter[i];
                foreach (Entity entity in Entity.GetEntitiesInRadius(p.position, 0.1f))
                {
                    bool shouldTakeDamage = true;

                    //帽子可以防止扣血
                    if (entity is IInventoryOwner owner)
                    {
                        var inventory = owner.GetInventory();
                        if (inventory != null && !Item.Null(inventory.helmet))
                            shouldTakeDamage = false;
                    }

                    if (shouldTakeDamage)
                        entity.TakeDamage(1);
                }
            }
        }
    }
}