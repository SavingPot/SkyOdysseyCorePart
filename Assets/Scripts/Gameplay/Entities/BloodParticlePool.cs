using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class BloodParticlePool
    {
        public Stack<ParticleSystem> stack = new();

        public ParticleSystem Get(Entity entity)
        {
            var particle = stack.Count == 0 ? GameObject.Instantiate(GInit.instance.BloodParticleSystemPrefab) : stack.Pop();

            particle.transform.position = entity.transform.position;
            particle.gameObject.SetActive(true);
            particle.Play();
            Tools.InvokeAfter(particle.main.duration + particle.main.startLifetime.constant, () =>
            {
                stack.Push(particle);
            });

            //生成一个新的
            return particle;
        }

        public void Recover(ParticleSystem particle)
        {
            stack.Push(particle);

            particle.gameObject.SetActive(false);
        }
    }
}