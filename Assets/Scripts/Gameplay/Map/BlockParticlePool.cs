using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class BlockParticlePool
    {
        public Stack<ParticleSystem> stack = new();

        public ParticleSystem Get(Block block) => Get(new(block.transform.position.x, block.transform.position.y + 0.6f), block.sr.sprite);
        public ParticleSystem Get(Vector2 pos, Sprite sprite)
        {
            var particle = stack.Count == 0 ? GameObject.Instantiate(GInit.instance.BlockParticleSystemPrefab) : stack.Pop();

            particle.transform.position = pos;
            var shape = particle.shape; shape.texture = sprite.texture;
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